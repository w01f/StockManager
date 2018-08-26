using StockManager.Infrastructure.Utilities.Logging.Models;

namespace StockManager.Infrastructure.Utilities.Logging.Services
{
	public interface ILoggingService
	{
		void LogAction<TAction>(TAction action) where TAction : BaseLogAction;
	}
}
