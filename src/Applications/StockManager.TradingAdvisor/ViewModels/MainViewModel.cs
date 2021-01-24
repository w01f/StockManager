using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Ninject;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.TradingAdvisor.Controllers;
using StockManager.TradingAdvisor.Models;
using StockManager.TradingAdvisor.Properties;

namespace StockManager.TradingAdvisor.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		private readonly Dispatcher _dispatcher;
		private readonly InstrumentsController _instrumentsController;

		public ICommand LoadCommand { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		private ObservableCollection<InstrumentInfo> _instruments = new ObservableCollection<InstrumentInfo>();
		public ObservableCollection<InstrumentInfo> Instruments
		{
			get => _instruments;
			set
			{
				if (value == _instruments)
					return;
				_instruments = value;
				OnPropertyChanged(nameof(Instruments));
			}
		}

		private bool _exceptionHandled;
		public bool ExceptionHandled
		{
			get => _exceptionHandled;
			set
			{
				if (value == _exceptionHandled)
					return;
				_exceptionHandled = value;
				OnPropertyChanged(nameof(ExceptionHandled));
			}
		}

		private string _exceptionText;
		public string ExceptionText
		{
			get => _exceptionText;
			set
			{
				if (value == _exceptionText)
					return;
				_exceptionText = value;
				OnPropertyChanged(nameof(ExceptionText));
			}
		}

		[Inject]
		public MainViewModel(Dispatcher dispatcher, InstrumentsController instrumentsController)
		{
			_dispatcher = dispatcher;
			_instrumentsController = instrumentsController ?? throw new ArgumentNullException();

			LoadCommand = new CommonCommand(OnLoading);
		}

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private async void OnLoading()
		{
			var instruments = await _instrumentsController.GetInstruments().ConfigureAwait(false);
			Dispatch(() =>
			{
				Instruments = new ObservableCollection<InstrumentInfo>(instruments
					.OrderBy(instrument=>instrument.CurrencyPair.BaseCurrencyId)
					.ThenBy(instrument=>instrument.CurrencyPair.QuoteCurrencyId));
			});

			_instrumentsController.SuggestedToBuyCurrencyPairsChanged += OnSuggestedToBuyCurrencyPairsChanged;
			_instrumentsController.Exception += OnException;

			_instrumentsController.StartMonitoring();
		}

		private void OnSuggestedToBuyCurrencyPairsChanged(object sender, CurrencyPairsEventArgs e)
		{
			Dispatch(() =>
			{
				foreach (var instrumentInfo in Instruments)
				{
					if (e.CurrencyPairs.Any(currency => string.Equals(currency.Id, instrumentInfo.CurrencyPair.Id)))
					{
						instrumentInfo.IsSuggestedToBuy = true;
						instrumentInfo.IsSuggestedToSell = false;
					}
				}
			});
		}

		private void OnException(object sender, UnhandledExceptionEventArgs e)
		{
			Dispatch(() =>
			{
				ExceptionHandled = true;
				ExceptionText = $"{(e.ExceptionObject as Exception)?.Message}{Environment.NewLine}{(e.ExceptionObject as Exception)?.StackTrace}";
			});
		}

		private void Dispatch(Action action)
		{
			if (action == null) return;

			if (_dispatcher is null) action();

			if (_dispatcher.CheckAccess())
				action();
			else
				_dispatcher.Invoke(action);
		}
	}
}
