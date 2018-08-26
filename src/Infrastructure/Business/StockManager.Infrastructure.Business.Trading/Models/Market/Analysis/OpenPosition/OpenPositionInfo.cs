using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition
{
	public abstract class OpenPositionInfo
	{
		public OpenMarketPositionType PositionType { get; }

		protected OpenPositionInfo(OpenMarketPositionType positionType)
		{
			PositionType = positionType;
		}
	}
}
