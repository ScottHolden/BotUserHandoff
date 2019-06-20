using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

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
		public static async Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient client, string url, object o)
		{
			using (StringContent content = new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json"))
			{
				return await client.PostAsync(url, content);
			}
		}
		public static async Task<T> ReadJsonAsAsync<T>(this HttpContent content) =>
			JsonConvert.DeserializeObject<T>(await content.ReadAsStringAsync());
	}
}
