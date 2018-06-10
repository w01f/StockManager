﻿using System.Threading.Tasks;
using Ninject;
using StockManager.Infrastructure.Business.Chart.Models;
using StockManager.Infrastructure.Business.Chart.Services;

namespace StockManager.Dashboard.Controllers
{
	public class CurrencyPairController
	{
		private readonly ChartService _chartService;

		[Inject]
		public CurrencyPairController(ChartService chartService)
		{
			_chartService = chartService;
		}

		public async Task<ChartDataset> GetChartData(ChartSettings settings)
		{
			return await _chartService.GetChartData(settings);
		}
	}
}
