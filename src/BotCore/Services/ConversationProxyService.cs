using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace BotCore
{
	public class ConversationProxyService
	{
		private const string StorageKeyPrefix = "conversationproxy-reference-";
		private readonly IBotFrameworkHttpAdapter _adapter;
		private readonly string _botId;
		private readonly IStorage _storage;

		public ConversationProxyService(IBotFrameworkHttpAdapter adapter, 
										ICredentialProvider credentials,
										IStorage storage)
		{
			_adapter = adapter;
			_storage = storage;
			_botId = ((SimpleCredentialProvider)credentials).AppId;

			if (string.IsNullOrEmpty(_botId))
				_botId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid (Emulator, etc)
		}

		public async Task<string> RegisterConversationAsync(ConversationReference reference, CancellationToken cancellationToken)
		{
			string proxyId = Guid.NewGuid().ToString();
			string storageKey = BuildStorageKey(proxyId);

			// Store proxy info
			await _storage.WriteAsync(new Dictionary<string, object>
			{
				{storageKey, reference}
			}, cancellationToken);

			return proxyId;
		}
		private async Task<ConversationReference> GetConversationReferenceAsync(string proxyId, CancellationToken cancellationToken)
		{
			string storageKey = BuildStorageKey(proxyId);

			IDictionary<string, ConversationReference> refs = await _storage.ReadAsync<ConversationReference>(new[] { storageKey }, cancellationToken);

			return refs.ContainsKey(storageKey) ? refs[storageKey] : null;
		}

		public async Task RemoveConversationAsync(string proxyId, CancellationToken cancellationToken)
		{
			string storageKey = BuildStorageKey(proxyId);

			await _storage.DeleteAsync(new[] { storageKey }, cancellationToken);
		}
		
		public async Task SendMessageAsync(string proxyId, string text, CancellationToken cancellationToken)
		{
			// Read back the conversation reference
			ConversationReference conversation = await GetConversationReferenceAsync(proxyId, cancellationToken);

			await ((BotAdapter)_adapter).ContinueConversationAsync(_botId, conversation, 
						(turnContext, innerCancellationToken) => turnContext.SendActivityAsync(text),
						cancellationToken);
		}

		private string BuildStorageKey(string proxyId) => StorageKeyPrefix + proxyId;
	}
}
