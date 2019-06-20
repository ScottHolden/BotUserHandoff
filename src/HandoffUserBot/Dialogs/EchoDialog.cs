using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace HandoffUserBot
{
	public class EchoDialog : ComponentDialog
	{
		private readonly ConversationState _conversationState;

		public EchoDialog(ConversationState conversationState,
							HandoffDialog handoffDialog)
			: base(nameof(EchoDialog))
		{
			_conversationState = conversationState;

			AddDialog(handoffDialog);

			AddDialog(new TextPrompt(nameof(TextPrompt)));
			AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
				{
					MessagePromptStepAsync,
					EchoLoopStepAsync
				}));

			InitialDialogId = nameof(WaterfallDialog);
		}

		private async Task<DialogTurnResult> MessagePromptStepAsync(
			WaterfallStepContext stepContext,
			CancellationToken cancellationToken)
		{
			PromptOptions messagePrompt = new PromptOptions();

			if (!(stepContext.Options is bool looping && looping))
			{
				messagePrompt.Prompt = MessageFactory.Text("As an example of normal interactions with a bot, your messages will be echo'ed back, reservsed");
			}

			return await stepContext.PromptAsync(nameof(TextPrompt), messagePrompt);
		}

		private async Task<DialogTurnResult> EchoLoopStepAsync(
			WaterfallStepContext stepContext,
			CancellationToken cancellationToken)
		{
			string text = stepContext.Context.Activity.Text;

			// If the user says to handoff, we do.
			if(text.Trim().Equals("/handoff"))
			{
				return await stepContext.ReplaceDialogAsync(nameof(HandoffDialog), true, cancellationToken);
			}

			string response = Reverse(text);

			IStatePropertyAccessor<List<string>> conversationStateAccessors = _conversationState.CreateProperty<List<string>>(HandoffDialog.MessageHistoryStateKey);
			List<string> conversationData = await conversationStateAccessors.GetAsync(stepContext.Context, () => new List<string>());

			// Explict logging for messages, but could also be done via middleware
			conversationData.Add("User: " + text);
			conversationData.Add("Bot: " + response);

			await stepContext.Context.SendActivityAsync(response, cancellationToken: cancellationToken);

			return await stepContext.ReplaceDialogAsync(nameof(EchoDialog), true, cancellationToken);
		}

		private string Reverse(string text)
		{
			if (text == null) return null;

			char[] array = text.ToCharArray();

			Array.Reverse(array);

			return new string(array);
		}
	}
}
