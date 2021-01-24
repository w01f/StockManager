namespace StockManager.TradingAdvisor.Converters
{
	public abstract class BaseBooleanConverter<TSource, TResult> : BaseConverter<TSource, TResult>
	{
		public bool IsInvert { get; set; }

		protected abstract TResult Positive { get; set; }
		protected abstract TResult Negative { get; set; }
		protected abstract bool Resolve(TSource source);

		protected override TResult Convert(TSource source)
		{
			var value = Resolve(source);
			if(IsInvert)
			{
				value = !value;
			}

			var result = value ? Positive : Negative;
			return result;
		}

	}
}
