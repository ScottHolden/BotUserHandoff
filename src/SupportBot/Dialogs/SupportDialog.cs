using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace SupportBot
{
	public class SupportDialog : ComponentDialog
	{
		private readonly UserState _userState;

		public SupportDialog(UserState userState)
			: base(nameof(SupportDialog))
		{
			_userState = userState;
		}
	}
}
