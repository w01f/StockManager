using System;
using System.Collections.Generic;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Business.Common.Helpers
{
	static class Extensions
	{
		public static IEnumerable<DateTime> GetMomentsByPeriod(CandlePeriod period, int limit)
		{
			var lastDate = DateTime.UtcNow;
			lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, lastDate.Hour, lastDate.Minute, 0);
			switch (period)
			{
				case CandlePeriod.Minute3:
					while (lastDate.Minute % 3 != 0)
						lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Minute5:
					while (lastDate.Minute % 5 != 0)
						lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Minute15:
					while (lastDate.Minute % 15 != 0)
						lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Minute30:
					while (lastDate.Minute % 30 != 0)
						lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Hour1:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, lastDate.Hour, 0, 0);
					break;
				case CandlePeriod.Hour4:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, lastDate.Hour, 0, 0);
					while (lastDate.Hour % 4 != 0)
						lastDate = lastDate.AddHours(-1);
					break;
				case CandlePeriod.Day1:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, 0, 0, 0);
					break;
				case CandlePeriod.Day7:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, 0, 0, 0);
					while (lastDate.DayOfWeek != DayOfWeek.Monday)
						lastDate = lastDate.AddDays(-1);
					break;
				case CandlePeriod.Month1:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, 1, 0, 0, 0);
					break;
			}

			while (limit > 0)
			{
				yield return lastDate;
				switch (period)
				{
					case CandlePeriod.Minute1:
						lastDate = lastDate.AddMinutes(-1);
						break;
					case CandlePeriod.Minute3:
						lastDate = lastDate.AddMinutes(-3);
						break;
					case CandlePeriod.Minute5:
						lastDate = lastDate.AddMinutes(-5);
						break;
					case CandlePeriod.Minute15:
						lastDate = lastDate.AddMinutes(-15);
						break;
					case CandlePeriod.Minute30:
						lastDate = lastDate.AddMinutes(-30);
						break;
					case CandlePeriod.Hour1:
						lastDate = lastDate.AddHours(-1);
						break;
					case CandlePeriod.Hour4:
						lastDate = lastDate.AddHours(-4);
						break;
					case CandlePeriod.Day1:
						lastDate = lastDate.AddDays(-1);
						break;
					case CandlePeriod.Day7:
						lastDate = lastDate.AddDays(-7);
						break;
					case CandlePeriod.Month1:
						lastDate = lastDate.AddMonths(-1);
						break;
				}
				limit--;
			}
		}
	}
}
