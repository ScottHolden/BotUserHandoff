using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace SupportBot
{
	public class RootDialog : ComponentDialog
	{
		private readonly UserState _userState;

		public RootDialog(UserState userState)
			: base(nameof(RootDialog))
		{
			_userState = userState;
		}


	}
}
