/*--------------------------+
NestedContextInjectionBinder.cs
2014
Jacob Liechty

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using strange.extensions.nestedcontext.api;
using strange.extensions.context.api;
using strange.extensions.context.impl;
using strange.extensions.injector.api;
using strange.extensions.injector.impl;
using strange.framework.api;
using strange.framework.impl;

namespace strange.extensions.nestedcontext.impl
{
	public class NestedContextInjectionBinder : InjectionBinder, ICrossContextInjectionBinder
	{
		public IInjectionBinder CrossContextBinder { get; set; }

		public override IInjectionBinding GetBinding(object key, object name)
		{
			IInjectionBinding binding = base.GetBinding(key, name) as IInjectionBinding;
			if (binding == null) //Attempt to get this from the cross context. Cross context is always SECOND PRIORITY. Local injections always override
			{
				if (CrossContextBinder != null)
				{
					binding = CrossContextBinder.GetBinding(key, name) as IInjectionBinding;
				}
			}
			return binding;
		}

		override public void ResolveBinding(IBinding binding, object key)
		{
			ResolveBinding(binding, key, false);
		}

		private void ResolveBinding(IBinding binding, object key, bool forceLocal)
		{
			//Decide whether to resolve locally or not
			if (binding is IInjectionBinding)
			{
				InjectionBinding injectionBinding = (InjectionBinding)binding;
				if (injectionBinding.isCrossContext && !forceLocal)
				{
					if (CrossContextBinder == null) //We are a crosscontextbinder
					{
						base.ResolveBinding(binding, key);
					}
					else
					{
						Unbind(key, binding.name); //remove this cross context binding from the local binder

						if (CrossContextBinder is NestedContextInjectionBinder)
						{
							(CrossContextBinder as NestedContextInjectionBinder).ResolveBinding(binding, key, true);
						}
						else
						{
							CrossContextBinder.ResolveBinding(binding, key);
						}
					}
				}
				else
				{
					base.ResolveBinding(binding, key);
				}
			}
		}
	}
}

