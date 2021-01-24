using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.TradingAdvisor.Models
{
	public class InstrumentInfo
    {
        public CurrencyPair CurrencyPair { get; set; }
        public bool IsSuggestedToBuy { get; set; }
        public bool IsSuggestedToSell { get; set; }
    }
}
