using System;
using Newtonsoft.Json;
using StockManager.Domain.Core.Entities.Logging;

namespace StockManager.Infrastructure.Utilities.Logging.Models
{
	public static class LogActionMap
	{
		public static TActionModel ToModel<TActionModel>(this LogAction source) where TActionModel:BaseLogAction
		{
			var actionModel = !String.IsNullOrEmpty(source.ExtendedOptionsEncoded)
				? JsonConvert.DeserializeObject<TActionModel>(source.ExtendedOptionsEncoded)
				: Activator.CreateInstance<TActionModel>();

			return actionModel;
		}

		public static LogAction ToEntity(this BaseLogAction source, LogAction target = null)
		{
			if (target == null)
				target = new LogAction();

			target.Moment = source.Moment;
			target.LogActionType = source.LogActionType;
			target.ExtendedOptionsEncoded = JsonConvert.SerializeObject(source);

			return target;
		}
	}
}
