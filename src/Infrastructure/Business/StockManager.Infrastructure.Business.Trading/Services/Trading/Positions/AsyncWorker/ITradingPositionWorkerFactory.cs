using System;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.AsyncWorker
{
	public interface ITradingPositionWorkerFactory
	{
		Task<TradingPositionWorker> CreateWorkerWithNewPosition(NewOrderPositionInfo newPositionInfo, Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback);
		TradingPositionWorker CreateWorkerWithExistingPosition(TradingPosition tradingPosition, Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback);
	}
}
