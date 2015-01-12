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

using UnityEngine;
using System;
using System.Linq;
using strange.extensions.mediation.api;
using strange.extensions.mediation.impl;
using strange.extensions.nestedcontext.api;
using strange.framework.api;

namespace strange.extensions.nestedcontext.impl
{
	public class NestedContextMediationBinder : MediationBinder, INestedContextMediationBinder
	{
		public IMediationBinder CrossContextBinder { get; set; }
		
		public NestedContextMediationBinder() : base()
		{
		}

		/// Initialize all IViews within this view
		override protected void injectViewAndChildren(strange.extensions.mediation.api.IView view)
		{
			MonoBehaviour mono = view as MonoBehaviour;
			injectionBinder.injector.Inject(mono, false);
		}

		public override IBinding GetRawBinding()
		{
			return new NestedContextMediationBinding(resolver) as IBinding;
		}

		public override IBinding GetBinding(object key, object name)
		{
			IBinding binding = base.GetBinding(key, name);
			if (binding == null) //Attempt to get this from the cross context. Cross context is always SECOND PRIORITY. Local injections always override
			{
				if (CrossContextBinder != null)
				{
					binding = CrossContextBinder.GetBinding(key, name);
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
			if (binding is INestedContextMediationBinding)
			{
				NestedContextMediationBinding injectionBinding = (NestedContextMediationBinding)binding;
				if (injectionBinding.isCrossContext && !forceLocal)
				{
					if (CrossContextBinder == null) //We are a crosscontextbinder
					{
						base.ResolveBinding(binding, key);
					}
					else
					{
						Unbind(key, binding.name); //remove this cross context binding from the local binder

						if (CrossContextBinder is NestedContextMediationBinder)
						{
							(CrossContextBinder as NestedContextMediationBinder).ResolveBinding(binding, key, true);
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
		
		/// Creates and registers one or more Mediators for a specific View instance.
		/// Takes a specific View instance and a binding and, if a binding is found for that type, creates and registers a Mediator.
		override protected void mapView(IView view, IMediationBinding binding)
		{
			Type viewType = view.GetType();

			object[] values = binding.value as object[];
			int aa = values.Length;
			for (int a = 0; a < aa; a++)
			{
				MonoBehaviour mono = view as MonoBehaviour;
				Type mediatorType = values[a] as Type;
				if (mediatorType == viewType)
				{
					throw new MediationException(viewType + "mapped to itself. The result would be a stack overflow.", MediationExceptionType.MEDIATOR_VIEW_STACK_OVERFLOW);
				}
				MonoBehaviour mediator = mono.gameObject.AddComponent(mediatorType) as MonoBehaviour;
				if (mediator == null)
					throw new MediationException("The view: " + viewType.ToString() + " is mapped to mediator: " + mediatorType.ToString() + ". AddComponent resulted in null, which probably means " + mediatorType.ToString().Substring(mediatorType.ToString().LastIndexOf(".") + 1) + " is not a MonoBehaviour.", MediationExceptionType.NULL_MEDIATOR);
				if (mediator is IMediator)
					((IMediator)mediator).PreRegister();

				injectionBinder.Bind<IView>().ToValue(view).ToInject(false);

				Type typeToInject = (binding.abstraction == null || binding.abstraction.Equals(BindingConst.NULLOID)) ? viewType : binding.abstraction as Type;
				if (typeof(IView) != typeToInject)
					injectionBinder.Bind(typeToInject).ToValue(view).ToInject(false);

				injectionBinder.injector.Inject(mediator);
				if (typeof(IView) != typeToInject)
					injectionBinder.Unbind(typeToInject);

				injectionBinder.Unbind<IView>();

				if (mediator is IMediator)
					((IMediator)mediator).OnRegister();
			}
		}

		/// Removes a mediator when its view is destroyed
		override protected void unmapView(IView view, IMediationBinding binding)
		{
			object[] values = binding.value as object[];
			int aa = values.Length;
			for (int a = 0; a < aa; a++)
			{
				Type mediatorType = values[a] as Type;
				MonoBehaviour mono = view as MonoBehaviour;
				IMediator mediator = mono.GetComponent(mediatorType) as IMediator;
				if (mediator != null)
				{
					mediator.OnRemove();
					UnityEngine.Object.Destroy(mediator as MonoBehaviour);
				}
			}
		}

		new public INestedContextMediationBinding Bind<T>()
		{
			return base.Bind<T>() as INestedContextMediationBinding;
		}
	}
}
