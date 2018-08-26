using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Infrastructure.Utilities.Logging.Models.Errors
{
	public class ErrorAction:BaseLogAction
	{
		public override LogActionType LogActionType => LogActionType.ErrorAction;

		public string ExceptionType { get; set; }
		public string Message { get; set; }
		public string StackTrace { get; set; }
	}
}
