using System;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace StockManager.Infrastructure.Connectors.Socket.Models.Tinkoff.Subscriptions
{
	class Subscription
	{
		public StreamingRequest SubscribeRequest { get; set; }
		public StreamingRequest UnsubscribeRequest { get; set; }
		public Action<object, StreamingEventReceivedEventArgs> EventHandler { get; set; }
	}
}
