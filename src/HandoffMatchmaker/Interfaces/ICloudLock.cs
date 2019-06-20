using System.Threading.Tasks;

namespace HandoffMatchmaker
{
	public interface ICloudLock
	{
		Task<(bool, string)> GetLockAsync(string key);
		Task<bool> ReleaseLockAsync(string key, string lockString);
	}
}
