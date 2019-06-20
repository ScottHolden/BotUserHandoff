using System;
using Microsoft.Extensions.Configuration;

namespace HandoffMatchmaker.Services
{
	class ConfigPartyEndpointProvider : IPartyEndpointProvider
	{
		private const string UserPartyKeyName = "UserPartyCallbackUrl";
		private const string SupportPartyKeyName = "SupportPartyCallbackUrl";
		private readonly IConfiguration _configuration;

		public ConfigPartyEndpointProvider(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public string GetPartyUrl(PartyType party)
		{
			switch(party)
			{
				case PartyType.User:
					return _configuration.GetValue<string>(UserPartyKeyName);
				case PartyType.Support:
					return _configuration.GetValue<string>(SupportPartyKeyName);
				// TODO: Better exception
				default:
					throw new Exception("Unknown party type " + party);
			}
		}
	}
}
