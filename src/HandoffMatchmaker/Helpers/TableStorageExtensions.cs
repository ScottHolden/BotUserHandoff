using System.Threading.Tasks;
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
}
