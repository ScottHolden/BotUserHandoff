using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace HandoffMatchmaker
{
	public class MatchmakerService : IDisposable
	{
		private const string ProxyIdHeaderName = "x-handoff-proxyid";
		private readonly IRowStorage _rowStorage;
		private readonly IPartyEndpointProvider _partyEndpointProvider;
		private readonly IMatchmakeEventCallback _matchmakeEventCallback;
		private readonly ICloudLock _cloudLock;
		private readonly HttpClient _httpClient;

		public MatchmakerService(IRowStorage rowStorage, 
									IPartyEndpointProvider partyEndpointProvider,
									IMatchmakeEventCallback matchmakeEventCallback,
									ICloudLock cloudLock,
									IHttpClientFactory httpClientFactory)
		{
			_rowStorage = rowStorage;
			_partyEndpointProvider = partyEndpointProvider;
			_matchmakeEventCallback = matchmakeEventCallback;
			_cloudLock = cloudLock;
			_httpClient = httpClientFactory.CreateClient();
		}
		private const string MatchmakerPrefix = "mmsid-";
		private const string CloudLockKey = "inprogress";

		private string NewMatchmakerSessionId() => MatchmakerPrefix + Guid.NewGuid().ToString("D");
		private static readonly Regex MatchmakerSessionIdRegex = new Regex($@"^{MatchmakerPrefix}[0-9a-f]{{8}}-[0-9a-f]{{4}}-[0-9a-f]{{4}}-[0-9a-f]{{4}}-[0-9a-f]{{12}}$");
		private bool ValidateMatchmakerSessionIdFormat(string matchmakerSessionId) => MatchmakerSessionIdRegex.IsMatch(matchmakerSessionId);

		public async Task<string> NewSessionAsync(string proxyId, PartyType partyType)
		{
			SessionState session = new SessionState
			{
				SessionId = NewMatchmakerSessionId(),
				Valid = true,
				Connected = false,
				TransientETag = "*",
				Local = new ProxyEndpoint
				{
					PartyType = partyType,
					ProxyId = proxyId
				}
			};

			await _rowStorage.UpdateRowAsync(session.SessionId, session);

			await _matchmakeEventCallback.QueueMatchmakeEvent();

			return session.SessionId;
		}
		public async Task<SessionStateResponse> GetSessionAsync(string matchmakerSessionId)
		{
			if (!ValidateMatchmakerSessionIdFormat(matchmakerSessionId))
				return SessionStateResponse.ErrorResponse();

			SessionState sessionState = await _rowStorage.GetRowAsync<SessionState>(matchmakerSessionId);

			if(sessionState == null)
				return SessionStateResponse.ErrorResponse();

			return new SessionStateResponse
			{
				Connected = sessionState.Connected,
				Valid = sessionState.Valid
			};
		}
		public async Task SendMessageToRemoteAsync(string matchmakerSessionId, string text)
		{
			// TODO: Log/exception
			if (!ValidateMatchmakerSessionIdFormat(matchmakerSessionId))
				return;

			SessionState sessionState = await _rowStorage.GetRowAsync<SessionState>(matchmakerSessionId);

			await SendProxyMessageAsync(sessionState.Remote, text);
		}

		public async Task SendProxyMessageAsync(ProxyEndpoint endpoint, string text)
		{
			string url = _partyEndpointProvider.GetPartyUrl(endpoint.PartyType);
			
			using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
			{
				request.Headers.Add(ProxyIdHeaderName, endpoint.ProxyId);
				request.Content = new StringContent(text);
				using(HttpResponseMessage response = await _httpClient.SendAsync(request))
				{
					response.EnsureSuccessStatusCode();
				}
			}
		}

		public async Task AddSessionBackgroundAsync(string matchmakerSessionId, IEnumerable<string> info)
		{
			StringBuilder sb = new StringBuilder();
			int msOffset = 0;
			foreach(string s in info)
			{
				sb.AppendLine(s);
				if (sb.Length > 1000)
				{
					await SaveSessionBackgroundAsync(new BackgroundMessage
					{
						TimeStamp = DateTime.Now.AddMilliseconds(++msOffset),
						MessageId = Guid.NewGuid().ToString(),
						SessionId = matchmakerSessionId,
						Text = sb.ToString()
					});
					sb.Clear();
				}
			}
			if (sb.Length > 0)
			{
				await SaveSessionBackgroundAsync(new BackgroundMessage
				{
					TimeStamp = DateTime.Now.AddMilliseconds(++msOffset),
					MessageId = Guid.NewGuid().ToString(),
					SessionId = matchmakerSessionId,
					Text = sb.ToString()
				});
			}
		}

		private Task SaveSessionBackgroundAsync(BackgroundMessage message) =>
			_rowStorage.UpdateRowAsync(message.MessageId, message);

		private async Task<IEnumerable<string>> GetSessionBackgroundAsync(string matchmakerSessionId) =>
			(await _rowStorage.QueryRowsAsync<BackgroundMessage>(nameof(BackgroundMessage.SessionId), matchmakerSessionId))
									.OrderBy(x=>x.TimeStamp)
									.Select(x => x.Text);

		public Task JoinSessionsAsync()
			=> LockedTaskAsync(InternalJoinSessionsAsync);

		private async Task InternalJoinSessionsAsync()
		{
			IEnumerable<SessionState> session = await GetReadySessionsAsync();

			await Task.WhenAll(session.Where(x => x.Local.PartyType == PartyType.User)
										.Zip(session.Where(x => x.Local.PartyType == PartyType.Support), CreateConnection));
		}

		public async Task CreateConnection(SessionState userSession, SessionState supportSession)
		{
			userSession.Connected = true;
			userSession.Remote = supportSession.Local;
			supportSession.Connected = true;
			supportSession.Remote = userSession.Local;
			await Task.WhenAll(new[] {
				_rowStorage.UpdateRowAsync(userSession.SessionId, userSession),
				_rowStorage.UpdateRowAsync(supportSession.SessionId, supportSession)
			});
			await Task.WhenAll(new[] {
				SendUserWelcome(userSession.Local),
				SendSupportConnected(supportSession.Local, userSession)
			});
		}

		private Task SendUserWelcome(ProxyEndpoint endpoint) => SendProxyMessageAsync(endpoint, "You have been connected with a support agent");
		private async Task SendSupportConnected(ProxyEndpoint endpoint, SessionState userSession)
		{
			await SendProxyMessageAsync(endpoint, "Connected to a user, grabbing background info...");
			await SendProxyMessageAsync(endpoint, "---");
			

			IEnumerable<string> info = await GetSessionBackgroundAsync(userSession.SessionId);

			foreach(string s in info)
				await SendProxyMessageAsync(endpoint, s);

			await SendProxyMessageAsync(endpoint, "---");
			await SendProxyMessageAsync(endpoint, "Ready to talk:");
		}

		private static readonly SemaphoreSlim _localSemaphone = new SemaphoreSlim(1);
		private async Task LockedTaskAsync(Func<Task> func)
		{
			if(await _localSemaphone.WaitAsync(60 * 1000))
			{
				try
				{
					bool success = false;
					string leaseId = null;
					try
					{
						(success, leaseId) = await _cloudLock.GetLockAsync(CloudLockKey);

						if(success)
							await func();
					}
					finally
					{
						if (success)
							await _cloudLock.ReleaseLockAsync(CloudLockKey, leaseId);
					}
				}
				finally
				{
					_localSemaphone.Release();
				}
			}
		}

		private Task<IEnumerable<SessionState>> GetReadySessionsAsync() =>
			_rowStorage.QueryRowsAsync<SessionState>(nameof(SessionState.Connected), false);

		public void Dispose()
		{
			_httpClient.Dispose();
		}
	}
}
