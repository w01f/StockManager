using System;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow
{
	interface ITradingPositionStateProcessor
	{
		bool IsAllowToProcess(TradingPosition currentState, TradingPosition nextState);
		Task<TradingPosition> ProcessTradingPositionChanging(TradingPosition currentState, 
			TradingPosition nextState, 
			bool syncWithStock, 
			Action<PositionChangedEventArgs> onPositionChangedCallback);
	}
}
