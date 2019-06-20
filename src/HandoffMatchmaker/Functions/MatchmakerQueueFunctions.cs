using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace HandoffMatchmaker.Functions
{
    public class MatchmakerQueueFunctions
    {
		public const string MatchmakerQueueName = "matchmake";
		private readonly MatchmakerService _matchmakerService;
		public MatchmakerQueueFunctions(MatchmakerService matchmakerService)
		{
			_matchmakerService = matchmakerService;
		}

        [FunctionName(nameof(MatchmakeAsync))]
        public void MatchmakeAsync([QueueTrigger(MatchmakerQueueName)]
			string runId, 
			ILogger log)
        {
            log.LogInformation($"Starting Matchmaking Process {runId}");

			_matchmakerService.JoinSessionsAsync();

			log.LogInformation($"Done!");
		}
    }
}
