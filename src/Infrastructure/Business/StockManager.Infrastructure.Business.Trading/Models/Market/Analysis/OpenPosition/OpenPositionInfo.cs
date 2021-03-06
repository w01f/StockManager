﻿using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition
{
	public abstract class OpenPositionInfo
	{
		public OpenMarketPositionType PositionType { get; set; }

		protected OpenPositionInfo(OpenMarketPositionType positionType)
		{
			PositionType = positionType;
		}
	}
}
