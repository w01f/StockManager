using System;
using Newtonsoft.Json;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Common.Models.Trading
{
	public static class OrderMap
	{
		public static Order ToModel(this Domain.Core.Entities.Trading.Order source, CurrencyPair currencyPair)
		{
			var target = new Order();

			target.ExtId = source.ExtId;
			target.ClientId = source.ClientId;
			target.ParentClientId = source.ParentClientId;
			target.CurrencyPair = currencyPair.Id == source.CurrencyPair ? currencyPair :
				throw new BusinessException("Undefined currency")
				{
					Details = String.Format("Expected currency: {0}; Received currency {1}", currencyPair.Id, source.CurrencyPair)
				};
			target.Role = source.Role;
			target.OrderSide = source.OrderSide;
			target.OrderType = source.OrderType;
			target.OrderStateType = source.OrderStateType;
			target.TimeInForce = source.TimeInForce;
			target.Quantity = source.Quantity;
			target.Price = source.Price;
			target.StopPrice = source.StopPrice;

			if (!String.IsNullOrEmpty(source.AnalysisInfoEncoded))
				switch (source.Role)
				{
					case OrderRoleType.StopLoss:
						target.AnalysisInfo = JsonConvert.DeserializeObject<StopLossOrderInfo>(source.AnalysisInfoEncoded);
						break;
					default:
						target.AnalysisInfo = null;
						break;
				}

			target.Created = source.Created;
			target.Updated = source.Updated;

			return target;
		}

		public static Domain.Core.Entities.Trading.Order ToEntity(this Order source, Domain.Core.Entities.Trading.Order target = null)
		{
			if (target == null)
				target = new Domain.Core.Entities.Trading.Order();

			target.ExtId = source.ExtId;
			target.ClientId = source.ClientId;
			target.ParentClientId = source.ParentClientId;
			target.CurrencyPair = source.CurrencyPair.Id;
			target.Role = source.Role;
			target.OrderSide = source.OrderSide;
			target.OrderType = source.OrderType;
			target.OrderStateType = source.OrderStateType;
			target.TimeInForce = source.TimeInForce;
			target.Quantity = source.Quantity;
			target.Price = source.Price;
			target.StopPrice = source.StopPrice;

			if (source.AnalysisInfo != null)
				target.AnalysisInfoEncoded = JsonConvert.SerializeObject(source.AnalysisInfo);

			target.Created = source.Created;
			target.Updated = source.Updated;

			return target;
		}

		public static Domain.Core.Entities.Trading.OrderHistory ToHistory(this Order source)
		{
			var target = new Domain.Core.Entities.Trading.OrderHistory();

			target.ExtId = source.ExtId;
			target.ClientId = source.ClientId;
			target.ParentClientId = source.ParentClientId;
			target.CurrencyPair = source.CurrencyPair.Id;
			target.Role = source.Role;
			target.OrderSide = source.OrderSide;
			target.OrderType = source.OrderType;
			target.OrderStateType = source.OrderStateType;
			target.TimeInForce = source.TimeInForce;
			target.Quantity = source.Quantity;
			target.Price = source.Price;
			target.StopPrice = source.StopPrice;

			if (source.AnalysisInfo != null)
				target.AnalysisInfoEncoded = JsonConvert.SerializeObject(source.AnalysisInfo);

			target.Created = source.Created;
			target.Updated = source.Updated;

			return target;
		}
	}
}
