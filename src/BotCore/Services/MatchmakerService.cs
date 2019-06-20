using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BotCore
{
	public class MatchmakerService
	{
		private const string PartyAuthHeader = "x-handoff-partykey";
		private const string MatchmakerUrlKey = "MatchmakerUrl";
		private const string MatchmakerKeyKey = "MatchmakerKey";
		
		private readonly HttpClient _httpClient;

		public MatchmakerService(IConfiguration configuration)
		{
			string matchmakerUrl = configuration.GetValue<string>(MatchmakerUrlKey);
			string matchmakerKey = configuration.GetValue<string>(MatchmakerKeyKey);

			_httpClient = new HttpClient
			{
				BaseAddress = new Uri(matchmakerUrl)
			};
			_httpClient.DefaultRequestHeaders.Add(PartyAuthHeader, matchmakerKey);
		}

		public async Task<string> NewSessionAsync(string proxyId)
		{
			NewSessionRequest newSessionRequest = new NewSessionRequest
			{
				ProxyId = proxyId
			};
			using(HttpResponseMessage response = await _httpClient.PostAsJsonAsync("session", newSessionRequest))
			{
				response.EnsureSuccessStatusCode();

				NewSessionResponse newSessionResponse = await response.Content.ReadJsonAsAsync<NewSessionResponse>();

				return newSessionResponse.SessionId;
			}
		}
		public async Task<SessionState> GetSessionAsync(string matchmakerSessionId)
		{
			using (HttpResponseMessage response = await _httpClient.GetAsync($"session/{matchmakerSessionId}"))
			{
				response.EnsureSuccessStatusCode();

				return await response.Content.ReadJsonAsAsync<SessionState>();
			}
		}
		public async Task SendMessageAsync(string matchmakerSessionId, string text)
		{
			using(StringContent content = new StringContent(text))
			using (HttpResponseMessage response = await _httpClient.PostAsync($"session/{matchmakerSessionId}/message", content))
			{
				response.EnsureSuccessStatusCode();
			}
		}
		public async Task AddSessionBackgroundAsync(string matchmakerSessionId, IEnumerable<string> info)
		{
			using (HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"session/{matchmakerSessionId}/background", info.ToArray()))
			{
				response.EnsureSuccessStatusCode();
			}
		}
	}
}
