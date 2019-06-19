using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;

namespace HandoffUserBot
{
	public class HandoffDialog : ComponentDialog
	{
		private const string HandoffState = "value-userHandoffState";
		private readonly ILogger _log;
		private readonly UserState _userState;

		public HandoffDialog(UserState userState, ILogger<HandoffDialog> logger)
			: base(nameof(HandoffDialog))
		{
			_log = logger;
			_userState = userState;

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

			if (!stepContext.Values.ContainsKey(HandoffState))
			{
				stepContext.Values.Add(HandoffState, true);

				if (!(stepContext.Options is bool) || !(bool)stepContext.Options)
				{
					messagePrompt.Prompt = MessageFactory.Text("You are now connected with a support agent.");
				}
				
			}

			return await stepContext.PromptAsync(nameof(TextPrompt), messagePrompt);
		}

		private async Task<DialogTurnResult> HandoffLoopStepAsync(
			WaterfallStepContext stepContext,
			CancellationToken cancellationToken)
		{
			string message = (string)stepContext.Result;

			_log.LogInformation($"Sending message '{message}' to handoff engine");

			bool handoffFinished = false;


			if (handoffFinished)
				return await stepContext.EndDialogAsync(null, cancellationToken);
			else
				return await stepContext.ReplaceDialogAsync(nameof(HandoffDialog), true, cancellationToken);
		}
	}
}
