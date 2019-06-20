using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace HandoffUserBot
{
	[Route("api/handoff")]
	[ApiController]
	public class ConversationProxyController : ControllerBase
	{
		private const string ProxyIdHeaderName = "x-handoff-proxyid";
		private readonly ConversationProxyService _conversationProxy;

		public ConversationProxyController(ConversationProxyService conversationProxy)
		{
			_conversationProxy = conversationProxy;
		}

		[HttpPost]
		public async Task<IActionResult> PostAsync(CancellationToken cancellationToken)
		{
			if (!Request.Headers.TryGetValue(ProxyIdHeaderName, out StringValues values) ||
				values.Count != 1)
			{
				return Unauthorized();
			}

			string proxyId = values.FirstOrDefault();

			try
			{
				string text = Request.ReadBodyAsString();

				await _conversationProxy.SendMessageAsync(proxyId, text, cancellationToken);
			}
			catch
			{
				return BadRequest();
			}

			return Ok();
		}
	}
}
