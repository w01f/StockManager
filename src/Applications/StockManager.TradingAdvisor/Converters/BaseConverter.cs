using System;
using System.Globalization;
using System.Windows.Data;

namespace StockManager.TradingAdvisor.Converters
{
	public abstract class BaseConverter<TSource, TResult> : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is TSource))
				return ConvertDefault(value);

			var result = Convert((TSource) value);
			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is TResult))
				return ConvertBackDefault(value);

			var result = ConvertBack((TResult)value);
			return result;
		}

		protected abstract TResult Convert(TSource source);

		protected virtual TSource ConvertBack(TResult source)
		{
			return default(TSource);
		}

		protected virtual TResult ConvertDefault(object value)
		{
			return default(TResult);
		}

		protected virtual TSource ConvertBackDefault(object value)
		{
			return default(TSource);
		}
	}
}
