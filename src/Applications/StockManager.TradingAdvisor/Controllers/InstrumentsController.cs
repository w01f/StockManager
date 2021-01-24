using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ninject;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers;
using StockManager.TradingAdvisor.Models;

namespace StockManager.TradingAdvisor.Controllers
{
	public class InstrumentsController
	{
		private readonly TinkoffTradingAdvisorController _tinkoffTradingAdvisorController;

		public event EventHandler<CurrencyPairsEventArgs> SuggestedToBuyCurrencyPairsChanged;
		public event EventHandler<UnhandledExceptionEventArgs> Exception;

		[Inject]
		public InstrumentsController(TinkoffTradingAdvisorController tinkoffTradingAdvisorController)
		{
			_tinkoffTradingAdvisorController = tinkoffTradingAdvisorController ?? throw new ArgumentNullException(nameof(tinkoffTradingAdvisorController));
		}

		public async Task<IList<InstrumentInfo>> GetInstruments()
		{
			var activeCurrencyPairs = await _tinkoffTradingAdvisorController.GetActiveCurrencyPairs();
			return activeCurrencyPairs.Select(currencyPair => new InstrumentInfo { CurrencyPair = currencyPair}).ToList();
		}

		public void StartMonitoring()
		{
			_tinkoffTradingAdvisorController.SuggestedCurrencyPairsUpdated += OnSuggestedCurrencyPairsUpdated;
			_tinkoffTradingAdvisorController.Exception += OnException;
			_tinkoffTradingAdvisorController.StartMonitoring();
		}

		private void OnSuggestedCurrencyPairsUpdated(object sender, CurrencyPairsEventArgs e)
		{
			SuggestedToBuyCurrencyPairsChanged?.Invoke(this, e);
		}

		protected virtual void OnException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception?.Invoke(this, e);
		}
	}
}