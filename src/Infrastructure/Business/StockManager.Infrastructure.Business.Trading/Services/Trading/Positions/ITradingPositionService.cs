using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions
{
	public interface ITradingPositionService
	{
		Task<IList<TradingPosition>> GetOpenPositions();
		Task SyncExistingPositionsWithStock(Action<PositionChangedEventArgs> onPositionChangedCallback);
		Task<TradingPosition> OpenPosition(NewOrderPositionInfo positionInfo);
		Task<TradingPosition> UpdatePosition(TradingPosition currentPosition, TradingPosition nextPosition, bool syncWithStock, Action<PositionChangedEventArgs> onPositionChangedCallback);
	}
}
