using System;
using System.Threading.Tasks;
using HandoffMatchmaker.Functions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Queue;

namespace HandoffMatchmaker.Services
{
	public class AzureQueueMatchmakeEventCallback : IMatchmakeEventCallback
	{
		private readonly CloudQueue _queue;
		public AzureQueueMatchmakeEventCallback(IConfiguration configuration)
		{
			string connectionString = configuration.GetValue<string>("AzureWebJobsStorage");

			StorageAccount storageAccount = StorageAccount.NewFromConnectionString(connectionString);
			CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
			_queue = queueClient.GetQueueReference(MatchmakerQueueFunctions.MatchmakerQueueName);
		}
		public async Task QueueMatchmakeEvent()
		{
			await _queue.CreateIfNotExistsAsync();
			await _queue.AddMessageAsync(new CloudQueueMessage(Guid.NewGuid().ToString()));
		}
	}
}
