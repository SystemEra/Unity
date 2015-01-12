/*--------------------------+
CrossContextMediationBinding.cs
2014
Jacob Liechty

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using strange.extensions.mediation.impl;
using strange.extensions.nestedcontext.api;
using strange.framework.api;
using strange.framework.impl;

namespace strange.extensions.nestedcontext.impl
{
	public class NestedContextMediationBinding : MediationBinding, INestedContextMediationBinding
	{
		private bool _isCrossContext = false;

		public NestedContextMediationBinding(Binder.BindingResolver resolver)
			: base(resolver)
		{
		}

		public bool isCrossContext
		{
			get
			{
				return _isCrossContext;
			}
		}

		public INestedContextMediationBinding CrossContext()
		{
			_isCrossContext = true;
			if (resolver != null)
			{
				resolver(this);
			}
			return this;
		}

		new public INestedContextMediationBinding Bind<T>()
		{
			return base.Bind<T>() as INestedContextMediationBinding;
		}

		new public INestedContextMediationBinding Bind(object key)
		{
			return base.Bind(key) as INestedContextMediationBinding;
		}

		new public INestedContextMediationBinding To<T>()
		{
			return base.To<T>() as INestedContextMediationBinding;
		}

		new public INestedContextMediationBinding To(object o)
		{
			return base.To(o) as INestedContextMediationBinding;
		}
	}
}
