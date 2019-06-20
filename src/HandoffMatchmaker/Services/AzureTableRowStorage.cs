using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Table;

namespace HandoffMatchmaker
{
	public class AzureTableRowStorage : IRowStorage
	{
		private const string FixedRowKey = "fixedvalue";
		private const string TablePrefix = "rowstore";
		private readonly CloudTableClient _tableClient;
		public AzureTableRowStorage(IConfiguration configuration)
		{
			string connectionString = configuration.GetValue<string>("AzureWebJobsStorage");

			StorageAccount storageAccount = StorageAccount.NewFromConnectionString(connectionString);
			_tableClient = storageAccount.CreateCloudTableClient();
		}

		private async Task<CloudTable> GetReferenceAsync<T>()
		{
			string tableName = TablePrefix + typeof(T).Name.ToLowerInvariant();

			CloudTable table = _tableClient.GetTableReference(tableName);

			// TODO: It's aggressive, look to cache
			await table.CreateIfNotExistsAsync();

			return table;
		}

		public async Task<T> GetRowAsync<T>(string key) where T : new()
		{
			CloudTable table = await GetReferenceAsync<T>();
			(bool success, T value) = await table.GetRowAsync<T>(key, FixedRowKey);

			if (!success) throw new Exception("Unable to retrieve row " + key);

			return value;
		}

		public Task<IEnumerable<T>> QueryRowsAsync<T>(string column, string value) where T : new() =>
			QueryRowsAsync<T>(TableQuery.GenerateFilterCondition(column, QueryComparisons.Equal, value));

		public Task<IEnumerable<T>> QueryRowsAsync<T>(string column, bool value) where T : new() =>
			QueryRowsAsync<T>(TableQuery.GenerateFilterConditionForBool(column, QueryComparisons.Equal, value));

		private async Task<IEnumerable<T>> QueryRowsAsync<T>(string filter) where T : new()
		{
			CloudTable table = await GetReferenceAsync<T>();

			TableQuery<AzureTableRowStorageAdapter<T>> query =
				new TableQuery<AzureTableRowStorageAdapter<T>>()
					.Where(filter);

			List<T> results = new List<T>();

			TableQuerySegment<AzureTableRowStorageAdapter<T>> segment = await table.ExecuteQuerySegmentedAsync(query, null);

			results.AddRange(segment.Results.Select(x => x.GetValue()));

			while (segment.ContinuationToken != null)
			{
				segment = await table.ExecuteQuerySegmentedAsync(query, segment.ContinuationToken);

				results.AddRange(segment.Results.Select(x => x.GetValue()));
			}

			return results;
		}

		public async Task UpdateRowAsync<T>(string key, T o) where T : new()
		{
			CloudTable table = await GetReferenceAsync<T>();

			AzureTableRowStorageAdapter<T> value = new AzureTableRowStorageAdapter<T>(o)
			{
				PartitionKey = key,
				RowKey = FixedRowKey,
				Timestamp = DateTimeOffset.UtcNow
			};

			await table.ExecuteAsync(TableOperation.InsertOrReplace(value));
		}
	}
}
