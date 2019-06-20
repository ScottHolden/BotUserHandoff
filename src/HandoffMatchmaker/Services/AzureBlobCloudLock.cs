using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HandoffMatchmaker.Services
{
	public class AzureBlobCloudLock : ICloudLock
	{
		private static readonly Dictionary<string, LeaseTimer> _timerCache = new Dictionary<string, LeaseTimer>();
		private const string LockContainerName = "cloudlocks";
		private readonly CloudBlobContainer _blobContainer;
		public AzureBlobCloudLock(IConfiguration configuration)
		{
			string connectionString = configuration.GetValue<string>("AzureWebJobsStorage");

			StorageAccount storageAccount = StorageAccount.NewFromConnectionString(connectionString);
			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
			_blobContainer = blobClient.GetContainerReference(LockContainerName);
			_blobContainer.CreateIfNotExistsAsync().Wait();
		}

		private class LeaseTimer
		{
			private readonly CloudBlockBlob _blob;
			private readonly string _leaseId;
			private readonly Timer _timer;
			public LeaseTimer(CloudBlockBlob blob, string leaseId)
			{
				_blob = blob;
				_leaseId = leaseId;
				_timer = new Timer(async (o) => await RenewLeaseAsync(), null, 15 * 1000, 15 * 1000);
			}
			private async Task RenewLeaseAsync()
			{
				try
				{
					await _blob.RenewLeaseAsync(new AccessCondition
					{
						LeaseId = _leaseId
					});
				}
				catch
				{
					Stop();
				}
			}
			public void Stop()
			{
				_timer.Change(-1, Timeout.Infinite);
				_timer.Dispose();
			}
		}

		public async Task<(bool, string)> GetLockAsync(string key)
		{
			try
			{
				CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(key);
				if (!await blob.ExistsAsync())
					await blob.UploadTextAsync("lock");
				string leaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(30));
				lock (_timerCache)
				{
					if (_timerCache.ContainsKey(key)) _timerCache.Remove(key);
					_timerCache.Add(key, new LeaseTimer(blob, leaseId));
				}
				return (true, leaseId);
			}
			// TODO: Don't catch 'em all!
			catch
			{
				return (false, null);
			}
		}

		public async Task<bool> ReleaseLockAsync(string key, string leaseId)
		{
			CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(key);
			try
			{
				await blob.ReleaseLeaseAsync(new AccessCondition
				{
					LeaseId = leaseId
				});
				lock (_timerCache)
				{
					if (_timerCache.ContainsKey(key))
					{
						_timerCache[key].Stop();
						_timerCache.Remove(key);
					}
				}
				return true;
			}
			// TODO: Don't catch 'em all!
			catch
			{
				return false;
			}
		}
	}
}
