using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace HandoffUserBot
{
	public class RootDialog : ComponentDialog
	{
		public RootDialog(HandoffDialog handoffDialog,
							EchoDialog echoDialog)
			: base(nameof(RootDialog))
		{
			AddDialog(handoffDialog);
			AddDialog(echoDialog);

			AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
			{
				InitialStepAsync,
				FinalStepAsync,
			}));

			InitialDialogId = nameof(WaterfallDialog);
		}

		private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			return await stepContext.BeginDialogAsync(nameof(EchoDialog), null, cancellationToken);
		}

		private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			await stepContext.Context.SendActivityAsync(CreateThankyouMessage(stepContext.Context.Activity));

			return await stepContext.EndDialogAsync(null, cancellationToken);
		}

		private Activity CreateThankyouMessage(IActivity activity) =>
			((Activity)activity).CreateReply("Your handoff session has finished, the bot should return back to normal state.");
	}
}
