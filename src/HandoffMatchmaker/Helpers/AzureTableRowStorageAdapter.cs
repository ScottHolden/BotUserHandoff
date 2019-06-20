using System;
using System.Collections.Generic;
using HandoffMatchmaker.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace HandoffMatchmaker
{
	internal class AzureTableRowStorageAdapter<T> : ITableEntity where T : new()
	{
		private T _internalObject;
		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset Timestamp { get; set; }
		public string ETag
		{
			get => (_internalObject is IETagged tagged) ? tagged.TransientETag : internalETag;
			set
			{
				if (_internalObject is IETagged tagged)
					tagged.TransientETag = value;
				else
					internalETag = value;
			}
		}
		private string internalETag = "*";

		public AzureTableRowStorageAdapter()
			: this(new T())
		{
		}

		public AzureTableRowStorageAdapter(T innerObject)
		{
			_internalObject = innerObject;
		}

		public T GetValue() => _internalObject;

		public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			_internalObject = EntityPropertyConverter.ConvertBack<T>(properties, operationContext);
		}

		public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
		{
			return EntityPropertyConverter.Flatten(_internalObject, operationContext);
		}
	}
}
