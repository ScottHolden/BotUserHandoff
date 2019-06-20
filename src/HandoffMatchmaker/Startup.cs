using HandoffMatchmaker.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HandoffMatchmaker.Startup))]

namespace HandoffMatchmaker
{
	public class Startup : FunctionsStartup
	{
		public override void Configure(IFunctionsHostBuilder builder)
		{
			builder.Services.AddHttpClient();
			builder.Services.AddSingleton<MatchmakerService>();
			builder.Services.AddSingleton<ICloudLock, AzureBlobCloudLock>();
			builder.Services.AddSingleton<IMatchmakeEventCallback, AzureQueueMatchmakeEventCallback>();
			builder.Services.AddSingleton<IPartyEndpointProvider, ConfigPartyEndpointProvider>();
			builder.Services.AddSingleton<IRowStorage, AzureTableRowStorage>();
			builder.Services.AddSingleton<IPartyKeyProvider, ConfigPartyKeyProvider>();
		} 
	}
}
