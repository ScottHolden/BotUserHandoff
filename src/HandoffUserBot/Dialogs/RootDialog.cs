using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace HandoffUserBot
{
	public class RootDialog : ComponentDialog
	{
		public RootDialog(HandoffDialog handoffDialog)
			: base(nameof(RootDialog))
		{
			AddDialog(handoffDialog);

			AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
			{
				InitialStepAsync,
				FinalStepAsync,
			}));

			InitialDialogId = nameof(WaterfallDialog);
		}

		private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			return await stepContext.BeginDialogAsync(nameof(HandoffDialog), null, cancellationToken);
		}

		protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
		{
			DialogTurnResult result = await InterruptAsync(innerDc, cancellationToken);
			if (result != null)
			{
				return result;
			}

			return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
		}

		private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
		{
			if (innerDc.Context.Activity.Type == ActivityTypes.Message)
			{
				string text = innerDc.Context.Activity.Text.ToLowerInvariant();

				switch (text)
				{
					case "cancel":
					case "quit":
						await innerDc.Context.SendActivityAsync($"Cancelling", cancellationToken: cancellationToken);
						return await innerDc.CancelAllDialogsAsync();
				}
			}
			return null;
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
