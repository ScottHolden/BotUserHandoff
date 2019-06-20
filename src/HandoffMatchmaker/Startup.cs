using System;
using System.Collections.Generic;
using System.Text;

[assembly: FunctionsStartup(typeof(HandoffMatchmaker.Startup))]

namespace HandoffMatchmaker
{
	public class Startup : FunctionsStartup
	{
		public override void Configure(IFunctionsHostBuilder builder)
		{
			builder.Services.AddHttpClient();
			builder.Services.AddSingleton((s) => {
				return new CosmosClient(Environment.GetEnvironmentVariable("COSMOSDB_CONNECTIONSTRING"));
			});
		}
	}
}
