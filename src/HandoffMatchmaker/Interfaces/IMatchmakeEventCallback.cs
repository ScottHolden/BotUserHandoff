using System.Threading.Tasks;


namespace HandoffMatchmaker
{
	public interface IMatchmakeEventCallback
	{
		Task QueueMatchmakeEvent();
	}
}
