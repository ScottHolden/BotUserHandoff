using BotCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace HandoffUserBot
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

			services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
			services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

			// Change to external state storage
			services.AddSingleton<IStorage, MemoryStorage>();
			services.AddSingleton<UserState>();
			services.AddSingleton<ConversationState>();

			services.AddSingleton<RootDialog>();
			services.AddSingleton<HandoffDialog>();
			services.AddTransient<IBot, DailogHandoffUserBot<RootDialog>>();
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseDefaultFiles();
				app.UseStaticFiles();
			}

			app.UseMvc();
		}
	}
}
