using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Interfaces for objects that are usable in node graphs
public interface IScoped { Type ScopedContextView { get; set; } }
public interface IScope<T> {}
public interface INamed<T> { }
public interface IProxy { object ValueObject { get; set; } }
public interface IProxy<T> : INamed<T>, IProxy { }
public interface ITitled { string Title { get; set; } }
public interface IBindingBase { }
public interface IEnableBindingBase { }

// Binding with return value
public interface IBinding<T> : IBindingBase { T Get(); }
public interface IEnableBinding<T> : IBinding<T>, IEnableBindingBase, IEnabled { }

// Won't be exposed in the node graph
public interface IEnableBindingExplicit<T> : IEnableBinding<T> { }
public interface IBindingExplicit<T> : IBinding<T> { }

// Type provider for binding menus
public class ConstructorSelection
{
	public string Title;
	public Func<object[], object> Constructor;
	public object[] Params;
	public ConstructorSelection(string title, Func<object[], object> constructor, object[] _params)
	{
		Title = title; Constructor = constructor; Params = _params;
	}
}

// To specify binding scopes
public class ScopeAttribute : System.Attribute
{
	public Type View;
	public ScopeAttribute(Type view) { View = view; }
}

public class ParentScopeAttribute : System.Attribute
{
	public Type[] Parents;
	public ParentScopeAttribute(Type parent) { Parents = new Type[] { parent }; }
	public ParentScopeAttribute(Type[] parents) { Parents = parents; }
}


// A component that is added to a prefab instantiation to tag where it came from
public class PrefabReferenceTag : MonoBehaviour
{
	public PrefabReference Tag;
}

// Tag a field to have its contents auto-enabled by BaseView
public class EnableAttribute : System.Attribute { }
public class ExposeAttribute : System.Attribute { }

// A globally unique prefab that will be referenced in save game with a GUID
public class PrefabGuid : PrefabReference 
{ 
	public System.Guid Guid = System.Guid.NewGuid();
	public override bool Equals(object o)
	{
		var rhs = o as PrefabGuid;
		return o != null && rhs.Guid == Guid;
	}

	public override int GetHashCode()
	{
		return Guid.GetHashCode();
	}
}

// A prefab referenced by index, presumably from a list
public class PrefabInstantiationReference : PrefabReference
{
	public int Index;
	public override bool Equals(object o)
	{
		var rhs = o as PrefabInstantiationReference;
		return o != null && rhs.Index == Index;
	}

	public override int GetHashCode()
	{
		return Index.GetHashCode();
	}
}

// A prefab that is guaranteed to be the only object with its name in that place in the hierachy
public class PrefabSingleton : PrefabReference { }

// Make our own signal types that can also be targets of bindingspublic class Signal<T> : strange.extensions.policysignal.impl.PolicySignal<T>, IBinding<Signal<T>> { Signal<T> IBinding<Signal<T>>.Get() { return this; } }
public class Signal<T> : strange.extensions.policysignal.impl.PolicySignal<T>, IBinding<Signal<T>> { Signal<T> IBinding<Signal<T>>.Get() { return this; } }
public class Signal<T, U> : strange.extensions.policysignal.impl.PolicySignal<T, U>, IBinding<Signal<T, U>> { Signal<T, U> IBinding<Signal<T, U>>.Get() { return this; } }
public class Signal<T, U, V> : strange.extensions.policysignal.impl.PolicySignal<T, U, V>, IBinding<Signal<T, U, V>> { Signal<T, U, V> IBinding<Signal<T, U, V>>.Get() { return this; } }
public class Signal<T, U, V, W> : strange.extensions.policysignal.impl.PolicySignal<T, U, V, W>, IBinding<Signal<T, U, V, W>> { Signal<T, U, V, W> IBinding<Signal<T, U, V, W>>.Get() { return this; } }

// Allow vanilla signal to also be the target of IActivationSignals.  This will basically be an activation that never deactivates.
public class Signal : strange.extensions.policysignal.impl.PolicySignal, IBinding<IActivationSignals>, IBinding<Signal>, IBinding<Action>, IActivationSignals
{
	IActivationSignals IBinding<IActivationSignals>.Get() { return this; }
	Signal IBinding<Signal>.Get() { return this; }
	Action IBinding<Action>.Get() { return Dispatch; }

	public void AddListener(Action activated, Action deactivated) { AddListener(activated); }
	public void AddListener(object key, Action activated, Action deactivated) { AddListener(key, activated); }
	public void RemoveListener(Action activated, Action deactivated) { RemoveListener(activated); }
	public void RemoveListener(object key, Action activated, Action deactivated) { RemoveListener(key, activated); }
	public void AddListener(IActivationActions actions) { AddListener(actions.Action); }
	public void AddListener(object key, IActivationActions actions) { AddListener(key, actions.Action); }
	public void RemoveListener(IActivationActions actions) { RemoveListener(actions.Action); }
	public void RemoveListener(object key, IActivationActions actions) { RemoveListener(key, actions.Action); }
	public bool GetActive() { return false; }
	public bool GetActive(object key) { return false; }
	public Signal ActivatedSignal { get { return this; } }
	public Signal DeactivatedSignal { get { return null; } }
}

// Reference utility interface
public interface IRef<T>
{
	T Get();
	void Set(T value);
}

// Reference utility implementation - Ref<float> is an example of a use case
public class Ref<T> : IRef<T>
{
	public T Value;
	public T Get() { return Value; }
	public void Set(T value) { Value = value; }
	public Ref() { Value = TypeUtil.Default<T>(); }
	public Ref(T value) { Value = value; }

	public override string ToString()
	{
		return Value.ToString();
	}

	public override bool Equals(object obj)
	{
		return obj is Ref<T> && (obj as Ref<T>).Value.Equals(Value);
	}

	public override int GetHashCode()
	{
		return Value != null ? Value.GetHashCode() : 0;
	}
}

// Custom signal type for performance and to allow edits during dispatch
public class UpdateSignal
{
    public HashSet<Action> Listeners = new HashSet<Action>();
    public void AddListener(Action action) { Listeners.Add(action); }
    public void RemoveListener(Action action) { Listeners.Remove(action); }

    public void Dispatch()
    {
        var listenersCopy = Listeners.ToList();
        foreach (Action action in listenersCopy)
        {
            if (Listeners.Contains(action))
                action();
        }
    }
}
public class FixedUpdateSignal : UpdateSignal { }

// A randomly generated value in a range
[ProtoBuf.ProtoContract(ImplicitFields = ProtoBuf.ImplicitFields.AllPublic)]
public class ParameterRangeReal
{
    public float Min = 1.0f;
    public float Max = 1.0f;
    public ParameterRangeReal() { }
    public ParameterRangeReal(float min, float max) { Min = min; Max = max; }
    public ParameterRangeReal(float value) { Min = value; Max = value; }

    public float GetRandom()
    {
        return UnityEngine.Random.Range(Min, Max);
    }
}

// A helper to allow the stack to be inspected so that we can point to line numbers
public class ScriptInfo
{
	public string FilePath;
	public int LineNumber;

	// Fetch cs file and line number from call stack.
	public static ScriptInfo Get()
	{
		var stackTraceFrame = new System.Diagnostics.StackTrace(true).GetFrame(1);
		return new ScriptInfo() { FilePath = stackTraceFrame.GetFileName(), LineNumber = stackTraceFrame.GetFileLineNumber() };
	}
}