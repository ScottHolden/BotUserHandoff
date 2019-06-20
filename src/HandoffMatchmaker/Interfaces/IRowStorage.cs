using System.Collections.Generic;
using System.Threading.Tasks;


namespace HandoffMatchmaker
{
	public interface IRowStorage
	{
		Task<T> GetRowAsync<T>(string key) where T : new();
		Task UpdateRowAsync<T>(string key, T o) where T : new();
		Task<IEnumerable<T>> QueryRowsAsync<T>(string column, string value) where T : new();
		Task<IEnumerable<T>> QueryRowsAsync<T>(string column, bool value) where T : new();
	}
}
