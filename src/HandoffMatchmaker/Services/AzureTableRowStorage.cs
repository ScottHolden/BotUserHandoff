using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HandoffMatchmaker.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace HandoffMatchmaker
{
	public static class TableStorageExtensions
	{
		public static async Task<(bool success, T value)> GetRowAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : new()
		{
			TableResult tableResult = await table.ExecuteAsync(TableOperation.Retrieve<AzureTableRowStorageAdapter<T>>(partitionKey, rowKey));
			if (tableResult.Result != null)
			{
				T value = ((AzureTableRowStorageAdapter<T>)tableResult.Result).GetValue();
				return (true, value);
			}
			return (false, default);
		}
	}
	internal class AzureTableRowStorageAdapter<T> : ITableEntity where T : new()
	{
		private readonly T _internalObject;
		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset Timestamp { get; set; }
		public string ETag
		{
			get => (_internalObject is IETagged tagged) ? tagged.TransientETag : internalETag;
			set
			{
				if (_internalObject is IETagged tagged)
					tagged.TransientETag = value;
				else
					internalETag = value;
			}
		}
		private string internalETag;

		public AzureTableRowStorageAdapter()
			: this(new T())
		{
		}

		public AzureTableRowStorageAdapter(T innerObject)
		{
			_internalObject = innerObject;
		}

		public T GetValue() => _internalObject;

		public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			TableEntity.ReadUserObject(_internalObject, properties, operationContext);
		}

		public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
		{
			return TableEntity.WriteUserObject(_internalObject, operationContext);
		}
	}
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

		public async Task<IEnumerable<T>> QueryRowsAsync<T>(string column, string value) where T : new()
		{
			CloudTable table = await GetReferenceAsync<T>();

			TableQuery<AzureTableRowStorageAdapter<T>> query =
				new TableQuery<AzureTableRowStorageAdapter<T>>()
					.Where(TableQuery.GenerateFilterCondition(column, QueryComparisons.Equal, value));

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

			AzureTableRowStorageAdapter<T> value = new AzureTableRowStorageAdapter<T>(o);

			await table.ExecuteAsync(TableOperation.InsertOrReplace(value));
		}
	}
}
