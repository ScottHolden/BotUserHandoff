﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace SupportBot
{
	public class DialogSupportBot<T> : ActivityHandler where T : Dialog
	{
		protected readonly Dialog Dialog;
		protected readonly BotState ConversationState;
		protected readonly BotState UserState;
		protected readonly ILogger Logger;

		public DialogSupportBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogSupportBot<T>> logger)
		{
			ConversationState = conversationState;
			UserState = userState;
			Dialog = dialog;
			Logger = logger;
		}

		public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
		{
			await base.OnTurnAsync(turnContext, cancellationToken);

			await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
			await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
		}

		protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
		{
			foreach (ChannelAccount member in membersAdded)
				if (member.Id != turnContext.Activity.Recipient.Id)
					await turnContext.SendActivityAsync(CreateWelcomeMessage(turnContext.Activity), cancellationToken);
		}

		protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
		{
			Logger.LogInformation("Running dialog with Message Activity.");

			await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
		}

		private Activity CreateWelcomeMessage(IActivity activity) =>
			((Activity)activity).CreateReply("Welcome, you are now connected to the support bot. To begin, send any message, and you will be put into the queue to recieve support requests");
	}
}
