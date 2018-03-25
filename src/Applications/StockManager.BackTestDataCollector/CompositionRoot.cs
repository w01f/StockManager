using System.Collections.Generic;
using Ninject;
using Ninject.Modules;
using Ninject.Parameters;

namespace StockManager.BackTestDataCollector
{
	class CompositionRoot
	{
		private static IKernel _ninjectKernel;

		public static void Initialize(INinjectModule module)
		{
			_ninjectKernel = new StandardKernel(module);
		}

		public static TObject Resolve<TObject>(params IParameter[] parameters)
		{
			return _ninjectKernel.Get<TObject>(parameters);
		}

		public static IEnumerable<TObject> ResolveAll<TObject>(params IParameter[] parameters)
		{
			return _ninjectKernel.GetAll<TObject>(parameters);
		}
	}
}
