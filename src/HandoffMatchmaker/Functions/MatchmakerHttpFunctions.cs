using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Linq;

namespace HandoffMatchmaker
{
	public class MatchmakerHttpFunctions
    {
		private const string PartyAuthHeader = "x-handoff-partykey";
		private readonly MatchmakerService _matchmakerService;
		private readonly IPartyKeyProvider _partyKeyProvider;

		public MatchmakerHttpFunctions(MatchmakerService matchmakerService,
										IPartyKeyProvider partyKeyProvider)
		{
			_matchmakerService = matchmakerService;
			_partyKeyProvider = partyKeyProvider;
		}

		private bool TryValidateRequest(HttpRequest req, out PartyType party)
		{
			party = PartyType.Unknown;
			if (!req.Headers.TryGetValue(PartyAuthHeader, out StringValues keys) ||
				keys.Count != 1)
			{
				return false;
			}

			string key = keys.FirstOrDefault();

			if(_partyKeyProvider.VerifyUserPartyKey(key))
			{
				party = PartyType.User;
				return true;
			}

			if (_partyKeyProvider.VerifySupportPartyKey(key))
			{
				party = PartyType.Support;
				return true;
			}

			return false;
		}

		[FunctionName(nameof(NewSessionAsync))]
        public async Task<IActionResult> NewSessionAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session")] 
				HttpRequest req,
            ILogger log)
        {
			// Validate keys if trying to create a new session
			if (!TryValidateRequest(req, out PartyType partyType))
				return new UnauthorizedResult();

			log.LogInformation("Setting up new session");

			NewSessionRequest sessionRequest = req.ReadyBodyJsonAs<NewSessionRequest>();

			string sessionId = await _matchmakerService.NewSessionAsync(sessionRequest.ProxyId, partyType);

			return new JsonResult(new NewSessionResponse
			{
				SessionId = sessionId
			});
        }

		[FunctionName(nameof(GetSessionAsync))]
		public async Task<IActionResult> GetSessionAsync(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}")]
				HttpRequest req,
			string sessionId,
			ILogger log)
		{
			// Validate keys
			if (!TryValidateRequest(req, out PartyType _))
				return new UnauthorizedResult();

			log.LogInformation("Getting session status");

			SessionStateResponse session = await _matchmakerService.GetSessionAsync(sessionId);

			if (session == null)
				return new BadRequestResult();

			return new JsonResult(session);
		}

		[FunctionName(nameof(SendMessageAsync))]
		public async Task<IActionResult> SendMessageAsync(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/message")]
				HttpRequest req,
			string sessionId,
			ILogger log)
		{
			// Validate keys
			if (!TryValidateRequest(req, out PartyType _))
				return new UnauthorizedResult();

			log.LogInformation("Sending session message");

			string text = await req.ReadAsStringAsync();

			await _matchmakerService.SendMessageToRemoteAsync(sessionId, text);

			return new OkResult();
		}

		[FunctionName(nameof(AddSessionBackgroundAsync))]
		public async Task<IActionResult> AddSessionBackgroundAsync(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/background")]
				HttpRequest req,
			string sessionId,
			ILogger log)
		{
			// Validate keys
			if (!TryValidateRequest(req, out PartyType _))
				return new UnauthorizedResult();

			log.LogInformation("Updating session background");

			string[] info = req.ReadyBodyJsonAs<string[]>();

			await _matchmakerService.AddSessionBackgroundAsync(sessionId, info);

			return new OkResult();
		}
	}
}
