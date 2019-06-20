namespace HandoffMatchmaker
{
	public interface IPartyKeyProvider
	{
		bool VerifyUserPartyKey(string key);
		bool VerifySupportPartyKey(string key);
	}
}
