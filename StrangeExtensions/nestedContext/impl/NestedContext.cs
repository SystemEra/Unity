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
using strange.extensions.injector.api;
using strange.extensions.injector.impl;
using strange.extensions.context.api;
using strange.extensions.nestedcontext.api;
using strange.extensions.context.impl;
using strange.framework.api;
using strange.framework.impl;
using strange.extensions.mediation.api;
using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.dispatcher.api;
using strange.extensions.dispatcher.eventdispatcher.impl;

namespace strange.extensions.nestedcontext.impl
{
	public class NestedContext : Context, INestedContext
	{		
		private IBinder _crossContextBridge;

		/// A Binder that handles dependency injection binding and instantiation
		public ICrossContextInjectionBinder injectionBinder { get; set; }
		
		/// A specific instance of EventDispatcher that communicates 
		/// across multiple contexts. An event sent across this 
		/// dispatcher will be re-dispatched by the various context-wide 
		/// dispatchers. So a dispatch to other contexts is simply 
		/// 
		/// `crossContextDispatcher.Dispatch(MY_EVENT, payload)`;
		/// 
		/// Other contexts don't need to listen to the cross-context dispatcher
		/// as such, just map the necessary event to your local context
		/// dispatcher and you'll receive it.
		protected IEventDispatcher _crossContextDispatcher;

		// The parent context in the nested hierarchy
		public IContext parentContext { get; set; }

		public NestedContext()
			: this(null)
		{
		}

		public NestedContext(MonoBehaviour view, bool autoMapping)
			: this(view, (autoMapping) ? ContextStartupFlags.MANUAL_MAPPING : ContextStartupFlags.MANUAL_LAUNCH | ContextStartupFlags.MANUAL_MAPPING)
		{
		}

		public NestedContext(object view)
			: this(view, ContextStartupFlags.AUTOMATIC)
		{
		}

		public NestedContext(object view, ContextStartupFlags flags)
		{
			if (view is strange.extensions.nestedcontext.api.INestedCapableView)
				parentContext = (view as strange.extensions.nestedcontext.api.INestedCapableView).overrideContext;

			if (view is INestedContextView)
				(view as INestedContextView).context = this;

			SetContextView(view);
			if (parentContext == null)
				SetParentContext();

			//If firstContext was unloaded, the contextView will be null. Assign the new context as firstContext.
			if (firstContext == null || firstContext.GetContextView() == null)
			{
				firstContext = this;
			}
			else
			{
				parentContext.AddContext(this);
			}

			addCoreComponents();
			this.autoStartup = (flags & ContextStartupFlags.MANUAL_LAUNCH) != ContextStartupFlags.MANUAL_LAUNCH;
			if ((flags & ContextStartupFlags.MANUAL_MAPPING) != ContextStartupFlags.MANUAL_MAPPING)
			{
				Start();
			}
		}

		virtual public void SetParentContext()
		{
			parentContext = firstContext;
		}

		protected override void addCoreComponents()
		{
			base.addCoreComponents();

			injectionBinder = new NestedContextInjectionBinder();
			if (parentContext != null && parentContext is NestedContext)
			{
				var newInjectionBinder = new NestedContextInjectionBinder();
				newInjectionBinder.CrossContextBinder = (parentContext as NestedContext).injectionBinder.CrossContextBinder;
				injectionBinder.CrossContextBinder = newInjectionBinder;
			}
			else
			{
				injectionBinder.CrossContextBinder = new NestedContextInjectionBinder();
			}
			
			if (firstContext == this)
			{
				injectionBinder.Bind<IEventDispatcher>().To<EventDispatcher>().ToSingleton().ToName(ContextKeys.CROSS_CONTEXT_DISPATCHER).CrossContext();
				injectionBinder.Bind<CrossContextBridge>().ToSingleton().CrossContext();
			}

			injectionBinder.Bind<INestedContext>().ToValue(this).CrossContext();
		}

		protected override void instantiateCoreComponents()
		{
			base.instantiateCoreComponents();

			IInjectionBinding dispatcherBinding = injectionBinder.GetBinding<IEventDispatcher>(ContextKeys.CONTEXT_DISPATCHER);

			if (dispatcherBinding != null)
			{
				IEventDispatcher dispatcher = injectionBinder.GetInstance<IEventDispatcher>(ContextKeys.CONTEXT_DISPATCHER) as IEventDispatcher;

				if (dispatcher != null)
				{
					crossContextDispatcher = injectionBinder.GetInstance<IEventDispatcher>(ContextKeys.CROSS_CONTEXT_DISPATCHER) as IEventDispatcher;
					(crossContextDispatcher as ITriggerProvider).AddTriggerable(dispatcher as ITriggerable);
					(dispatcher as ITriggerProvider).AddTriggerable(crossContextBridge as ITriggerable);
				}
			}
		}

		override public IContext AddContext(IContext context)
		{
			base.AddContext(context);
			if (context is ICrossContextCapable)
			{
				AssignCrossContext((ICrossContextCapable)context);
			}
			return this;
		}

		virtual public void AssignCrossContext(ICrossContextCapable childContext)
		{
			childContext.crossContextDispatcher = crossContextDispatcher;
		}

		virtual public void RemoveCrossContext(ICrossContextCapable childContext)
		{
			if (childContext.crossContextDispatcher != null)
			{
				((childContext.crossContextDispatcher) as ITriggerProvider).RemoveTriggerable(childContext.GetComponent<IEventDispatcher>(ContextKeys.CONTEXT_DISPATCHER) as ITriggerable);
				childContext.crossContextDispatcher = null;
			}
		}

		override public IContext RemoveContext(IContext context)
		{
			if (context is ICrossContextCapable)
			{
				RemoveCrossContext((ICrossContextCapable)context);
			}
			return base.RemoveContext(context);
		}

		virtual public IDispatcher crossContextDispatcher
		{
			get
			{
				return _crossContextDispatcher;
			}
			set
			{
				_crossContextDispatcher = value as IEventDispatcher;
			}
		}

		virtual public IBinder crossContextBridge
		{
			get
			{
				if (_crossContextBridge == null)
				{
					_crossContextBridge = injectionBinder.GetInstance<CrossContextBridge>() as IBinder;
				}
				return _crossContextBridge;
			}
			set
			{
				_crossContextDispatcher = value as IEventDispatcher;
			}
		}
	}
}

