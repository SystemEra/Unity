/*--------------------------+
ICrossContextMediationBinder.cs
2014
Jacob Liechty

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using strange.extensions.mediation.api;
using strange.framework.api;

namespace strange.extensions.nestedcontext.api
{
	public interface INestedContextMediationBinder : IMediationBinder
	{
		//Cross-context Mediation Binder is shared across all child contexts
		IMediationBinder CrossContextBinder { get; set; }

		/// Recast binding as ICrossContextMediationBinding.
		new INestedContextMediationBinding Bind<T>();
	}
}
