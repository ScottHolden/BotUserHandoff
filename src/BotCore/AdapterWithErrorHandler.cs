using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;

namespace BotCore
{
	public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
	{
		public AdapterWithErrorHandler(ICredentialProvider credentialProvider, ILogger<BotFrameworkHttpAdapter> logger, ConversationState conversationState = null)
			: base(credentialProvider)
		{
			OnTurnError = async (turnContext, exception) =>
			{
				logger.LogError($"Exception caught : {exception.Message}");
				await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");

				if (conversationState != null)
				{
					try
					{
						await conversationState.DeleteAsync(turnContext);
					}
					catch (Exception e)
					{
						logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
					}
				}
			};
		}
	}
}
