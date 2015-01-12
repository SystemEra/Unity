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

 
 */

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
