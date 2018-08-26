using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition
{
	public abstract class NewPositionInfo
	{
		public NewMarketPositionType PositionType { get; }

		protected NewPositionInfo(NewMarketPositionType positionType)
		{
			PositionType = positionType;
		}
	}
}
