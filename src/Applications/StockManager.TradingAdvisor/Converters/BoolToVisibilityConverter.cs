using System.Windows;

namespace StockManager.TradingAdvisor.Converters
{
	public class BoolToVisibilityConverter : BaseBooleanConverter<bool, Visibility>
	{
		protected override Visibility Positive { get; set; } = Visibility.Visible;
		protected override Visibility Negative { get; set; } = Visibility.Collapsed;
		protected override bool Resolve(bool source)
		{
			return source;
		}
	}
}
