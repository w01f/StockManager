using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition
{
	public abstract class PendingPositionInfo
	{
		public PendingMarketPositionType PositionType { get; }

		protected PendingPositionInfo(PendingMarketPositionType positionType)
		{
			PositionType = positionType;
		}
	}
}