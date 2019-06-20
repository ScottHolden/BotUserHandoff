using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BotCore
{
	public class MatchmakerService
	{
		private HttpClient _hc;
		public async Task<string> NewSessionAsync(string proxyId)
		{
			return Guid.NewGuid().ToString();
		}
		public async Task<SessionState> GetSessionAsync(string matchmakerSessionId)
		{
			using(HttpRequestMessage request = new HttpRequestMessage())
			{
				return null;
			}
		}
		public async Task SendMessageAsync(string matchmakerSessionId, string text)
		{

		}
		public async Task AddSessionBackground(string matchmakerSessionId, IEnumerable<string> info)
		{

		}
	}
}
