using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotCore;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace SupportBot
{
	public class SupportDialog : ComponentDialog
	{
		private const string HandoffState = "value-userHandoffState";
		// TODO: More logging!
		private readonly ILogger _log;
		private readonly MatchmakerService _matchmaker;
		private readonly ConversationProxyService _conversationProxy;

		public SupportDialog(MatchmakerService matchmaker,
								ConversationProxyService conversationProxy,
								ILogger<SupportDialog> logger)
			: base(nameof(SupportDialog))
		{
			_log = logger;
			_matchmaker = matchmaker;
			_conversationProxy = conversationProxy;

			AddDialog(new TextPrompt(nameof(TextPrompt)));
			AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
				{
					MessagePromptStepAsync,
					HandoffLoopStepAsync
				}));

			InitialDialogId = nameof(WaterfallDialog);
		}

		private async Task<DialogTurnResult> MessagePromptStepAsync(
			WaterfallStepContext stepContext,
			CancellationToken cancellationToken)
		{
			PromptOptions messagePrompt = new PromptOptions();

			if (!stepContext.Values.TryGetValue(HandoffState, out HandoffContextState contextState) ||
				contextState == null)
			{
				// Attempt to continue on from a previous context

				if (stepContext.Options is HandoffContextState passthoughContextState)
				{
					stepContext.Values.Add(HandoffState, passthoughContextState);
				}
				else
				{
					// Build our conversation callback

					ConversationReference conversationReference = stepContext.Context.Activity.GetConversationReference();

					string proxyId = await _conversationProxy.RegisterConversationAsync(conversationReference, cancellationToken);

					// Net new connection, connect to the handoff agent

					string sessionId = await _matchmaker.NewSessionAsync(proxyId);

					HandoffContextState newContextState = new HandoffContextState
					{
						MatchmakerSessionId = sessionId,
						ProxyId = proxyId
					};

					stepContext.Values.Add(HandoffState, newContextState);

					_log.LogInformation("New handoff session created");

					// Let the user know
					messagePrompt.Prompt = MessageFactory.Text("You have been put in the queue to be connected with a user, please wait...");
				}
			}

			return await stepContext.PromptAsync(nameof(TextPrompt), messagePrompt);
		}

		private async Task<DialogTurnResult> HandoffLoopStepAsync(
			WaterfallStepContext stepContext,
			CancellationToken cancellationToken)
		{
			string text = stepContext.Context.Activity.Text;

			// Check to make sure we have context

			if (!stepContext.Values.TryGetValue(HandoffState, out HandoffContextState contextState))
			{
				return await EndDialogWithCleanupAsync(stepContext,
														contextState,
														"Lost context state with matchmaking service",
														cancellationToken);
			}

			// Check to make sure that the context is still connected & valid

			SessionState sessionState = await _matchmaker.GetSessionAsync(contextState.MatchmakerSessionId);

			if (sessionState == null || !sessionState.Valid)
			{
				return await EndDialogWithCleanupAsync(stepContext,
														contextState,
														"Matchmaker couldn't find this conversation",
														cancellationToken);
			}

			// Let the user know that we haven't connected yet

			if (!sessionState.Connected)
			{
				await stepContext.Context.SendActivityAsync("Not currently connected, waiting on a user to request support...");
			}
			else
			{
				// Pass through the message to the matchmaker service

				await _matchmaker.SendMessageAsync(contextState.MatchmakerSessionId, text);
			}

			// Loop back to recieve next message

			return await stepContext.ReplaceDialogAsync(nameof(SupportDialog), contextState, cancellationToken);
		}

		private async Task<DialogTurnResult> EndDialogWithCleanupAsync(WaterfallStepContext stepContext,
																	HandoffContextState contextState,
																	string message,
																	CancellationToken cancellationToken)
		{
			if (contextState != null && !string.IsNullOrWhiteSpace(contextState.ProxyId))
				await _conversationProxy.RemoveConversationAsync(contextState.ProxyId, cancellationToken);

			if (!string.IsNullOrWhiteSpace(message))
				await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);

			return await stepContext.EndDialogAsync(null, cancellationToken);
		}
	}
}
