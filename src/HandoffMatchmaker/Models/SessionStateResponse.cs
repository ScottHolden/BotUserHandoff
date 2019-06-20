namespace HandoffMatchmaker
{
	public class SessionStateResponse
	{
		public bool Valid { get; set; }
		public bool Connected { get; set; }

		public static SessionStateResponse ErrorResponse() =>
			new SessionStateResponse {
				Connected = false,
				Valid = false
			};
	}
}
