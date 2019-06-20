using System.IO;
using Microsoft.AspNetCore.Http;

namespace BotCore
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
	}
}
