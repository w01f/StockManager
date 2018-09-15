namespace StockManager.Infrastructure.Common.Models.Trading
{
	public class StopLossOrderInfo : OrderAnalysisInfo
	{
		public decimal LastMaxValue { get; set; }
		public decimal TrailingStopAccelerationFactor { get; set; }
	}
}
