using System.Collections.Generic;

namespace BotCore
{
	public static class MiscExtensions
	{
		public static bool TryGetValue<T>(this IDictionary<string, object> dict, string key, out T result)
		{
			result = default;
			if (dict != null &&
				dict.ContainsKey(key) &&
				dict.TryGetValue(key, out object o) &&
				o is T value)
			{
				result = value;
				return true;
			}
			return false;
		}
	}
}