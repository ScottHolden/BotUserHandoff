using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace HandoffMatchmaker
{
	class ConfigPartyKeyProvider : IPartyKeyProvider
	{
		private const string UserPartyMatchmakerKeyName = "UserPartyMatchmakerKey";
		private const string SupportPartyMatchmakerKeyName = "SupportPartyMatchmakerKey";
		private readonly IConfiguration _configuration;

		public ConfigPartyKeyProvider(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public bool VerifySupportPartyKey(string key) =>
			_configuration.GetValue<string>(SupportPartyMatchmakerKeyName)
							.Equals(key, StringComparison.Ordinal);

		public bool VerifyUserPartyKey(string key) =>
			_configuration.GetValue<string>(UserPartyMatchmakerKeyName)
							.Equals(key, StringComparison.Ordinal);
	}
}
