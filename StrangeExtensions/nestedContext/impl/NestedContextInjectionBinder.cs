/*
  Copyright 2015 System Era Softworks
 
 	Licensed under the Apache License, Version 2.0 (the "License");
 	you may not use this file except in compliance with the License.
 	You may obtain a copy of the License at
 
 		http://www.apache.org/licenses/LICENSE-2.0
 
 		Unless required by applicable law or agreed to in writing, software
 		distributed under the License is distributed on an "AS IS" BASIS,
 		WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 		See the License for the specific language governing permissions and
 		limitations under the License.

 * The Strange IOC Framework is hosted at https://github.com/strangeioc/strangeioc. 
 * Code contained here may contain modified code which is subject to the license 
 * at this link.
 */

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

