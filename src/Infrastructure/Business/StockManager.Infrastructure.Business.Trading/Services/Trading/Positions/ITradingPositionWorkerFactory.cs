using System;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions
{
	public interface ITradingPositionWorkerFactory
	{
		Task<TradingPositionWorker> CreateWorkerWithNewPosition(NewOrderPositionInfo newPositionInfo, Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback);
		TradingPositionWorker CreateWorkerWithExistingPosition(OrderPair orderPair, Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback);
	}
}
