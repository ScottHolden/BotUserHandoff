using HandoffMatchmaker.Services;

namespace HandoffMatchmaker
{
	public class SessionState : IETagged
	{
		public string SessionId { get; set; }
		public string RemoteSessionId { get; set; }
		public bool Valid { get; set; }
		public bool Connected { get; set; }
		public ProxyEndpoint Local { get; set; }
		public ProxyEndpoint Remote { get; set; }
		public string TransientETag { get; set; }
	}

	public class ProxyEndpoint
	{
		public PartyType PartyType { get; set; }
		public string ProxyId { get; set; }
	}
}
