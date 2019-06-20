using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace HandoffMatchmaker
{
	public static class HttpExtensions
	{
		public static string ReadBodyAsString(this HttpRequest request)
		{
			using (StreamReader bodyReader = new StreamReader(request.Body))
			{
				return bodyReader.ReadToEnd();
			}
		}

		public static T ReadyBodyJsonAs<T>(this HttpRequest request) =>
			JsonConvert.DeserializeObject<T>(request.ReadBodyAsString());
	}
}
