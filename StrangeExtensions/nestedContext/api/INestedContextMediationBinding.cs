/*--------------------------+
ICrossContextMediationBinding.cs
2014
Jacob Liechty

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using strange.extensions.mediation.api;
using strange.framework.api;

namespace strange.extensions.nestedcontext.api
{
	public interface INestedContextMediationBinding : IMediationBinding
	{
		/// Map the binding and give access to all contexts in hierarchy
		INestedContextMediationBinding CrossContext();

		new INestedContextMediationBinding Bind<T>();
		new INestedContextMediationBinding Bind(object key);
		new INestedContextMediationBinding To<T>();
		new INestedContextMediationBinding To(object o);
	}
}
