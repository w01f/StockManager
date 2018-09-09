using System;
using System.Collections.Generic;
using System.Linq;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Models.Trading
{
	static class OrderMap
	{
		public static Order ToInnerModel(this Infrastructure.Common.Models.Trading.Order source, Order target = null)
		{
			if (target == null)
				target = new Order();

			target.Id = source.ExtId;
			target.ClientId = source.ClientId.ToString();
			target.CurrencyPairId = source.CurrencyPair.Id;

			switch (source.OrderSide)
			{
				case OrderSide.Sell:
					target.OrderSide = "sell";
					break;
				case OrderSide.Buy:
					target.OrderSide = "buy";
					break;
				default:
					throw new ConnectorException("Undefined order side", null);
			}

			switch (source.OrderType)
			{
				case OrderType.Limit:
					target.OrderType = "limit";
					break;
				case OrderType.Market:
					target.OrderType = "market";
					break;
				case OrderType.StopLimit:
					target.OrderType = "stopLimit";
					break;
				case OrderType.StopMarket:
					target.OrderType = "stopMarket";
					break;
				default:
					throw new ConnectorException("Undefined order type", null);
			}

			switch (source.OrderStateType)
			{
				case OrderStateType.New:
					target.OrderStateType = "new";
					break;
				case OrderStateType.Suspended:
					target.OrderStateType = "suspended";
					break;
				case OrderStateType.PartiallyFilled:
					target.OrderStateType = "partiallyFilled";
					break;
				case OrderStateType.Filled:
					target.OrderStateType = "filled";
					break;
				case OrderStateType.Cancelled:
					target.OrderStateType = "canceled";
					break;
				case OrderStateType.Expired:
					target.OrderStateType = "expired";
					break;
				default:
					throw new ConnectorException("Undefined order state type", null);
			}

			target.TimeInForce = "IOC";
			target.Quantity = source.Quantity;
			target.Price = source.Price;
			target.StopPrice = source.StopPrice ?? 0;
			target.Created = source.Created;
			target.Updated = source.Updated;

			return target;
		}

		public static Infrastructure.Common.Models.Trading.Order ToOuterModel(this Order source, IList<CurrencyPair> currencyPairs)
		{
			var target = new Infrastructure.Common.Models.Trading.Order();

			target.ExtId = (Int64)source.Id;
			target.ClientId = Guid.Parse(source.ClientId);
			target.CurrencyPair = currencyPairs.FirstOrDefault(item => item.Id == source.CurrencyPairId) ??
								  throw new ConnectorException("Undefined currency");

			switch (source.OrderSide.ToLower())
			{
				case "sell":
					target.OrderSide = OrderSide.Sell;
					break;
				case "buy":
					target.OrderSide = OrderSide.Buy;
					break;
				default:
					throw new ConnectorException("Undefined order side", null);
			}

			switch (source.OrderType.ToLower())
			{
				case "limit":
					target.OrderType = OrderType.Limit;
					break;
				case "market":
					target.OrderType = OrderType.Market;
					break;
				case "stopLimit":
					target.OrderType = OrderType.StopLimit;
					break;
				case "stopMarket":
					target.OrderType = OrderType.StopMarket;
					break;
				default:
					throw new ConnectorException("Undefined order type", null);
			}

			switch (source.OrderStateType.ToLower())
			{
				case "new":
					target.OrderStateType = OrderStateType.New;
					break;
				case "suspended":
					target.OrderStateType = OrderStateType.Suspended;
					break;
				case "partiallyFilled":
					target.OrderStateType = OrderStateType.PartiallyFilled;
					break;
				case "filled":
					target.OrderStateType = OrderStateType.Filled;
					break;
				case "canceled":
					target.OrderStateType = OrderStateType.Cancelled;
					break;
				case "expired":
					target.OrderStateType = OrderStateType.Expired;
					break;
				default:
					throw new ConnectorException("Undefined order state type", null);
			}

			target.Quantity = source.Quantity;
			target.Price = source.Price;
			target.StopPrice = source.StopPrice;
			target.Created = source.Created;
			target.Updated = source.Updated;

			return target;
		}
	}
}
