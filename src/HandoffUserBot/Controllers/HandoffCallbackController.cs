using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;

namespace HandoffUserBot
{
	[Route("api/handoff")]
	[ApiController]
	public class HandoffCallbackController : ControllerBase
	{
		private readonly IBot _bot;

		public HandoffCallbackController(IBot bot)
		{
			_bot = bot;
		}

		[HttpPost]
		public async Task PostAsync()
		{
			
			//
		}
	}
}
