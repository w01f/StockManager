using System;
using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Domain.Core.Entities.Market
{
	public class Candle : BaseEntity
	{
		public virtual string CurrencyPair { get; set; }
		public virtual DateTime Moment { get; set; }
		public virtual CandlePeriod Period { get; set; }
		public virtual decimal OpenPrice { get; set; }
		public virtual decimal ClosePrice { get; set; }
		public virtual decimal MaxPrice { get; set; }
		public virtual decimal MinPrice { get; set; }
		public virtual decimal VolumeInBaseCurrency { get; set; }
		public virtual decimal VolumeInQuoteCurrency { get; set; }
	}
}
