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
 * 
 * * The Strange IOC Framework is hosted at https://github.com/strangeioc/strangeioc. 
 * Code contained here may contain modified code which is subject to the license 
 * at this link.
 */

using strange.extensions.implicitBind.api;
using strange.extensions.implicitBind.impl;
using UnityEngine;
using strange.extensions.command.api;
using strange.extensions.command.impl;
using strange.extensions.context.api;
using strange.extensions.context.impl;
using strange.extensions.nestedcontext.api;
using strange.extensions.nestedcontext.impl;
using strange.extensions.dispatcher.api;
using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.injector.api;
using strange.extensions.mediation.api;
using strange.extensions.mediation.impl;
using strange.extensions.sequencer.api;
using strange.extensions.sequencer.impl;
using strange.framework.api;
using strange.framework.impl;

namespace strange.extensions.nestedcontext.impl
{
	public class MVCSNestedContext : NestedContext
	{
		/// A Binder that maps Events to Commands
		public ICommandBinder commandBinder{get;set;}

		/// A Binder that serves as the Event bus for the Context
		public IEventDispatcher dispatcher{get;set;}

		//Interprets implicit bindings
		public IImplicitBinder implicitBinder { get; set; }

		/// A Binder that maps Events to Sequences
		public ISequencer sequencer{get;set;}

		/// A Binder that maps Views to Mediators
		public INestedContextMediationBinder mediationBinder{get;set;}

		/// A list of Views Awake before the Context is fully set up.
		protected static ISemiBinding viewCache = new SemiBinding();
		
		public MVCSNestedContext() : base()
		{}

		/// The recommended Constructor
		/// Just pass in the instance of your ContextView. Everything will begin automatically.
		/// Other constructors offer the option of interrupting startup at useful moments.
		public MVCSNestedContext(MonoBehaviour view) : base(view)
		{
		}

		public MVCSNestedContext(MonoBehaviour view, ContextStartupFlags flags) : base(view, flags)
		{
		}

		public MVCSNestedContext(MonoBehaviour view, bool autoMapping) : base(view, autoMapping)
		{
		}
		
		override public IContext SetContextView(object view)
		{
			contextView = view;
			return this;
		}

		/// Map the relationships between the Binders.
		/// Although you can override this method, it is recommended
		/// that you provide all your application bindings in `mapBindings()`.
		protected override void addCoreComponents()
		{
			base.addCoreComponents();
			
			injectionBinder.Bind<IInstanceProvider>().Bind<IInjectionBinder>().ToValue(injectionBinder);
			injectionBinder.Bind<IContext>().ToValue(this).ToName(ContextKeys.CONTEXT);
			injectionBinder.Bind<ICommandBinder>().To<EventCommandBinder>().ToSingleton();
			//This binding is for local dispatchers
			injectionBinder.Bind<IEventDispatcher>().To<EventDispatcher>();
			//This binding is for the common system bus
			injectionBinder.Bind<IEventDispatcher>().To<EventDispatcher>().ToSingleton().ToName(ContextKeys.CONTEXT_DISPATCHER);
			injectionBinder.Bind<IMediationBinder>().To<NestedContextMediationBinder>().ToSingleton();
			injectionBinder.Bind<ISequencer>().To<EventSequencer>().ToSingleton();
			injectionBinder.Bind<IImplicitBinder>().To<ImplicitBinder>().ToSingleton();
		}
		
		protected override void instantiateCoreComponents()
		{
			base.instantiateCoreComponents();
			if (contextView == null)
			{
				throw new ContextException("MVCSContext requires a ContextView of type MonoBehaviour", ContextExceptionType.NO_CONTEXT_VIEW);
			}
			injectionBinder.Bind<MonoBehaviour>().ToValue(contextView).ToName(ContextKeys.CONTEXT_VIEW);
			injectionBinder.Bind<GameObject>().ToValue((contextView as MonoBehaviour).gameObject).ToName(ContextKeys.CONTEXT_VIEW);
			commandBinder = injectionBinder.GetInstance<ICommandBinder>() as ICommandBinder;
			dispatcher = injectionBinder.GetInstance<IEventDispatcher>(ContextKeys.CONTEXT_DISPATCHER) as IEventDispatcher;
			
			mediationBinder = injectionBinder.GetInstance<IMediationBinder>() as INestedContextMediationBinder;
			if (parentContext != null && parentContext is MVCSNestedContext)
			{
				var newMediationBinder = new NestedContextMediationBinder();
				newMediationBinder.CrossContextBinder = (parentContext as MVCSNestedContext).mediationBinder.CrossContextBinder;
				mediationBinder.CrossContextBinder = newMediationBinder;
			}
			else
			{
				mediationBinder.CrossContextBinder = new NestedContextMediationBinder();
			}

			sequencer = injectionBinder.GetInstance<ISequencer>() as ISequencer;
			implicitBinder = injectionBinder.GetInstance<IImplicitBinder>() as IImplicitBinder;

			(dispatcher as ITriggerProvider).AddTriggerable(commandBinder as ITriggerable);
			(dispatcher as ITriggerProvider).AddTriggerable(sequencer as ITriggerable);
		}
		
		/// Fires ContextEvent.START
		/// Whatever Command/Sequence you want to happen first should 
		/// be mapped to this event.
		public override void Launch()
		{
			dispatcher.Dispatch(ContextEvent.START);
		}
		
		/// Gets an instance of the provided generic type.
		/// Always bear in mind that doing this risks adding
		/// dependencies that must be cleaned up when Contexts
		/// are removed.
		override public object GetComponent<T>()
		{
			return GetComponent<T>(null);
		}

		/// Gets an instance of the provided generic type and name from the InjectionBinder
		/// Always bear in mind that doing this risks adding
		/// dependencies that must be cleaned up when Contexts
		/// are removed.
		override public object GetComponent<T>(object name)
		{
			IInjectionBinding binding = injectionBinder.GetBinding<T>(name);
			if (binding != null)
			{
				return injectionBinder.GetInstance<T>(name);
			}
			return null;
		}
		
		override public void AddView(object view)
		{
			base.AddView(view);
			if (mediationBinder != null)
			{
				mediationBinder.Trigger(MediationEvent.AWAKE, view as IView);
			}
			else
			{
				cacheView(view as MonoBehaviour);
			}
		}

		public override void SetParentContext()
		{
			parentContext = NestedCapableView.GetParentContext(contextView as INestedCapableView, (contextView as MonoBehaviour));
		}
		
		override public void RemoveView(object view)
		{
			base.RemoveView(view);
			mediationBinder.Trigger(MediationEvent.DESTROYED, view as IView);
		}

		/// Caches early-riser Views.
		/// 
		/// If a View is on stage at startup, it's possible for that
		/// View to be Awake before this Context has finished initing.
		/// `cacheView()` maintains a list of such 'early-risers'
		/// until the Context is ready to mediate them.
		virtual protected void cacheView(MonoBehaviour view)
		{
			if (viewCache.constraint.Equals(BindingConstraintType.ONE))
			{
				viewCache.constraint = BindingConstraintType.MANY;
			}
			viewCache.Add(view);
		}

		/// Provide mediation for early-riser Views
		virtual protected void mediateViewCache()
		{
			if (mediationBinder == null)
				throw new ContextException("MVCSContext cannot mediate views without a mediationBinder", ContextExceptionType.NO_MEDIATION_BINDER);
			
			object[] values = viewCache.value as object[];
			if (values == null)
			{
				return;
			}
			int aa = values.Length;
			for (int a = 0; a < aa; a++)
			{
				mediationBinder.Trigger(MediationEvent.AWAKE, values[a] as IView);
			}
			viewCache = new SemiBinding();
		}

		/// Clean up. Called by a ContextView in its OnDestroy method
		public override void OnRemove()
		{
			base.OnRemove();
			commandBinder.OnRemove();
		}
	}
}

