using StockManager.Domain.Core.Entities.Logging;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Utilities.Logging.Models;

namespace StockManager.Infrastructure.Utilities.Logging.Services
{
	public class LoggingService : ILoggingService
	{
		private readonly IRepository<LogAction> _repository;

		public LoggingService(IRepository<LogAction> repository)
		{
			_repository = repository;
		}

		public void LogAction<TAction>(TAction action) where TAction : BaseLogAction
		{
			_repository.Insert(action.ToEntity());
		}
	}
}
