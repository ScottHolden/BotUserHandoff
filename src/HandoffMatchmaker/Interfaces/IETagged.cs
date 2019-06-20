namespace HandoffMatchmaker.Services
{
	public interface IETagged
	{
		string TransientETag { get; set; }
	}
}
