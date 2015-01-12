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

using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using strange.extensions.context.impl;

public class Injection : IDisposable
{
	private BaseContext Context = null;
	private object Name = null;
	private Type Type = null;

	public Injection(BaseContext context, Type type, object value) 
	{
		if (context == null) return;

		Context = context;  
		Type = type;
		Context.injectionBinder.Bind(type).To(value);
	}

	public Injection(BaseContext context, Type type, object value, object name) 
	{
		if (context == null) return;

		Context = context;
		Type = type;
		Name = name;
		context.injectionBinder.Bind(type).To(value).ToName(name);
	}

	public void Dispose()
	{
		if (Context == null) return;
		if (Name == null)
			Context.injectionBinder.Unbind(Type);
		else
			Context.injectionBinder.Unbind(Type, Name);
	}

	public static Injection Create<T>(BaseContext context, T value)
	{
		return new Injection(context, typeof(T), value);
	}

	public static Injection Create<T>(BaseContext context, T value, object name)
	{
		return new Injection(context, typeof(T), value, name);
	}

	public static Injection Create(BaseContext context, Type type, object value)
	{
		return new Injection(context, type, value);
	}

	public static Injection Create<T>(BaseContext context, Type type, object value, object name)
	{
		return new Injection(context, type, value, name);
	}
}

public class BaseContext : strange.extensions.nestedcontext.impl.MVCSNestedContext 
{
	public BaseContext(MonoBehaviour view) : base(view) { }
	public new BaseContextView contextView { get { return base.contextView as BaseContextView; } }
	public BaseContextView ParentContextView { get { return parentContext as BaseContext != null ? (parentContext as BaseContext).contextView : null; } }

	// Unbind the default EventCommandBinder and rebind the SignalCommandBinder
	protected override void addCoreComponents()
	{
		base.addCoreComponents();
		injectionBinder.Unbind<strange.extensions.command.api.ICommandBinder>();
		injectionBinder.Bind<strange.extensions.command.api.ICommandBinder>().To<strange.extensions.command.impl.SignalCommandBinder>().ToSingleton();
	}

	public T GetInstance<T>() { return injectionBinder.GetInstance<T>(); }
	public T GetInstance<T>(object name) { return injectionBinder.GetInstance<T>(name); }

	public bool TryGetInstance<T>(out T value)
	{
		value = default(T);
		try { value = injectionBinder.GetInstance<T>(); }
		catch { return false; }
		return true;
	}

	public bool TryGetInstance<T>(object name, out T value)
	{
		value = default(T);
		try { value = injectionBinder.GetInstance<T>(name); }
		catch { return false; }
		return true;
	}

	public new strange.extensions.injector.api.IInjectionBinding Bind<T>()
	{
		return injectionBinder.Bind<T>();
	}

	public strange.extensions.injector.api.IInjectionBinding Bind<T>(object name)
	{
		return injectionBinder.Bind<T>().ToName(name);
	}

	public strange.extensions.injector.api.IInjectionBinding Rebind<T>()
	{
		injectionBinder.Unbind<T>();
		return injectionBinder.Bind<T>();
	}

	public strange.extensions.injector.api.IInjectionBinding Rebind<T>(object name)
	{
		injectionBinder.Unbind<T>(name);
		return injectionBinder.Bind<T>().ToName(name);
	}

	public strange.extensions.injector.api.IInjectionBinding RebindCrossContext<T>()
	{
		injectionBinder.CrossContextBinder.Unbind<T>();
		return injectionBinder.Bind<T>().CrossContext();
	}

	public strange.extensions.injector.api.IInjectionBinding RebindCrossContext<T>(object name)
	{
		injectionBinder.CrossContextBinder.Unbind<T>(name);
		return injectionBinder.Bind<T>().ToName(name).CrossContext();
	}

	public void Inject(object o)
	{
		injectionBinder.injector.Inject(o);
	}

	public T GetChildComponent<T>(string name) where T : Component
	{
		// Find a UI canvas
		Transform componentTransform = contextView.transform.Find(name);
		if (componentTransform != null)
		{
			T component = componentTransform.gameObject.GetComponent<T>();
			return component;
		}

		return null;
	}

	protected override void mapBindings()
	{
		base.mapBindings();
		
		injectionBinder.Bind<IBaseView>().ToValue(contextView).ToInject(false).CrossContext();
		injectionBinder.Bind<BaseContext>().ToValue(this).ToInject(false).CrossContext();
		injectionBinder.Bind<BaseContextView>().ToValue(contextView).ToInject(false).CrossContext();
		injectionBinder.Bind(contextView.GetType()).ToValue(contextView).ToInject(false).CrossContext();
		injectionBinder.Bind<GameObject>().ToValue(contextView.gameObject).ToInject(false).CrossContext();
	}
}

public class BaseContext<ContextViewType> : BaseContext where ContextViewType : BaseContextView
{
	public BaseContext(MonoBehaviour view) : base(view) { }
	public new ContextViewType contextView { get { return base.contextView as ContextViewType; } }

	protected override void mapBindings()
	{
		base.mapBindings();		
		if (contextView.GetType() != typeof(ContextViewType))
		{
			injectionBinder.Bind(typeof(ContextViewType)).ToValue(contextView).ToInject(false).CrossContext();
		}
	}
}

public interface IMainContext
{
	bool IsMainContext { get; }
}

public class MainContext<ContextViewType> : BaseContext<ContextViewType>, IMainContext where ContextViewType : BaseContextView
{
	public new MainContextView contextView { get { return base.contextView as MainContextView; } }
    public MainContext(MonoBehaviour view) : base(view) { }
	public bool IsMainContext { get { return firstContext == this; } }

    protected override void mapBindings()
    {
        base.mapBindings();

		if (IsMainContext)
        {
            // Globals			
			injectionBinder.Bind<SaveSignal>().ToSingleton().CrossContext();
            injectionBinder.Bind<UpdateSignal>().ToSingleton().CrossContext();
            injectionBinder.Bind<FixedUpdateSignal>().ToSingleton().CrossContext();
            injectionBinder.Bind<GlobalAudio>().ToSingleton().CrossContext();
			injectionBinder.Bind<VirtualControls>().ToValue(contextView.VirtualControls).CrossContext();
			injectionBinder.Bind<CanvasClickSignal>().ToSingleton().CrossContext();

			if (typeof(ContextViewType) != typeof(MainContextView))
				injectionBinder.Bind<MainContextView>().ToValue(contextView).CrossContext();

            var existingCanvas = GetChildComponent<Canvas>("Canvas");
			if (existingCanvas == null)
			{
				var canvasObject = new GameObject("Canvas");
				canvasObject.transform.parent = contextView.transform;

				var mainCanvas = canvasObject.AddComponent<Canvas>();
				injectionBinder.Bind<Canvas>().ToValue(mainCanvas).ToInject(false).CrossContext();
			}
			else
				injectionBinder.Bind<Canvas>().ToValue(existingCanvas).ToInject(false).CrossContext();

			var existingEvent = GetChildComponent<EventSystem>("EventSystem");
            if (existingEvent == null)
            {
                var eventObject = new GameObject("EventSystem");
                eventObject.transform.parent = contextView.transform;

				eventObject.AddComponent<StandaloneInputModule>();
                var mainEvent = eventObject.GetComponent<EventSystem>();
				injectionBinder.Bind<EventSystem>().ToValue(mainEvent).ToInject(false).CrossContext();
            }
			else
				injectionBinder.Bind<EventSystem>().ToValue(existingEvent).ToInject(false).CrossContext();
        }
    }
}