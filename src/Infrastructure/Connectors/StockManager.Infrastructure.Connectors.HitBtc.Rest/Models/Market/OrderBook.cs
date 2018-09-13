﻿using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Models.Market
{
	class OrderBook
	{
		[JsonProperty(PropertyName = "ask")]
		public OrderBookItem[] AskItems { get; set; }

		[JsonProperty(PropertyName = "bid")]
		public OrderBookItem[] BidItems { get; set; }
	}
}
