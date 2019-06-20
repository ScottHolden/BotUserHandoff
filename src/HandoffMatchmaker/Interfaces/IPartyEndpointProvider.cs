namespace HandoffMatchmaker
{
	public interface IPartyEndpointProvider
	{
		string GetPartyUrl(PartyType party);
	}
}
