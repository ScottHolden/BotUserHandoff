using System;


namespace HandoffMatchmaker
{
	public class BackgroundMessage
	{
		public string MessageId { get; set; }
		public string SessionId { get; set; }
		public string Text { get; set; }
		public DateTime TimeStamp { get; set; }
	}
}
