using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotCore;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace HandoffUserBot
{
	public class HandoffDialog : ComponentDialog
	{
		public static readonly string MessageHistoryStateKey = "conversation-messagehistory";

		private const string HandoffState = "value-userHandoffState";
		// TODO: More logging!
		private readonly ILogger _log;
		private readonly ConversationState _conversationState;
		private readonly MatchmakerService _matchmaker;
		private readonly ConversationProxyService _conversationProxy;

		public HandoffDialog(ConversationState conversationState,
								MatchmakerService matchmaker,
								ConversationProxyService conversationProxy,
								ILogger<HandoffDialog> logger)
			: base(nameof(HandoffDialog))
		{
			_log = logger;
			_conversationState = conversationState;
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
			
			if (!stepContext.Values.TryGetValue(HandoffState, out HandoffContextState contextState))
			{
				// Attempt to continue on from a previous context

				if(stepContext.Options is HandoffContextState passthoughContextState)
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

					// Add message to the background info
					// TODO: Refactor
					IStatePropertyAccessor<List<string>> conversationStateAccessors = _conversationState.CreateProperty<List<string>>(MessageHistoryStateKey);

					List<string> conversationData = await conversationStateAccessors.GetAsync(stepContext.Context, () => new List<string>());

					if (conversationData != null && conversationData.Count > 0)
					{
						await _matchmaker.AddSessionBackgroundAsync(sessionId, conversationData);

						// Clear the current history once it has been passed to the matchmaker
						await conversationStateAccessors.DeleteAsync(stepContext.Context);
					}

					_log.LogInformation("Set up new handoff session");

					// Let the user know
					messagePrompt.Prompt = MessageFactory.Text("You are being connected to a support agent, please wait...");
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
														sessionState.Connected ? "Handoff session has been closed" : null,
														cancellationToken);
			}

			// Exit out if the conversation is over

			if (text.Trim().Equals("/quit", StringComparison.OrdinalIgnoreCase))
			{
				return await EndDialogWithCleanupAsync(stepContext,
														contextState,
														"You have left the handoff, returing to normal bot",
														cancellationToken);
			}

			if (!sessionState.Connected)
			{
				// Let the user know that we haven't connected yet

				await stepContext.Context.SendActivityAsync("Waiting on a support agent, but i'll pass the message on");

				await _matchmaker.AddSessionBackgroundAsync(contextState.MatchmakerSessionId, new[] { "User: " + text });
			}
			else
			{
				// Pass through the message to the matchmaker service

				await _matchmaker.SendMessageAsync(contextState.MatchmakerSessionId, text);
			}

			// Loop back to recieve next message

			return await stepContext.ReplaceDialogAsync(nameof(HandoffDialog), contextState, cancellationToken);			
		}

		private async Task<DialogTurnResult> EndDialogWithCleanupAsync(WaterfallStepContext stepContext,
																	HandoffContextState contextState,
																	string message,
																	CancellationToken cancellationToken)
		{
			if (contextState != null && !string.IsNullOrWhiteSpace(contextState.MatchmakerSessionId))
				await _matchmaker.EndSessionAsync(contextState.MatchmakerSessionId);

			if (contextState != null && !string.IsNullOrWhiteSpace(contextState.ProxyId))
				await _conversationProxy.RemoveConversationAsync(contextState.ProxyId, cancellationToken);

			if(!string.IsNullOrWhiteSpace(message))
				await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);

			return await stepContext.EndDialogAsync(null, cancellationToken);
		}
	}
}
