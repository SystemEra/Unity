/*-------------------------------------------------------------------------+
ActivationCommand.cs
2014
Jacob Liechty

Activations are like Signals, except they have an on/off
lifecycle as opposed to being fired once.  
 
Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Functions like Signal, but for activations.  Add listeners to this.
public interface IActivationSignals
{
	void AddListener(Action activated, Action deactivated);
	void AddListener(object key, Action activated, Action deactivated);
	void RemoveListener(Action activated, Action deactivated);
	void RemoveListener(object key, Action activated, Action deactivated);
	void AddListener(IActivationActions actions);
	void AddListener(object key, IActivationActions actions);
	void RemoveListener(IActivationActions actions);
	void RemoveListener(object key, IActivationActions actions);
	bool GetActive();
	bool GetActive(object key);
	Signal ActivatedSignal { get; }
	Signal DeactivatedSignal { get; }
}

// Functions like Action, but for activations.  Invoke directly.
public interface IActivationActions
{
	void Activate();
	void Activate(object key);
	void Deactivate();
	void Deactivate(object key);
	bool GetActive();
	bool GetActive(object key);
	void Update(bool active);
	void Update(object key, bool active);
	Action Action { get; }
}

public interface IActivation : IActivationActions, IActivationSignals { }

[HideInInspector]
public abstract class ActivationBase : IActivation, IBinding<IActivationSignals>, IBinding<IActivationActions>
{
	// Our actual signals that will pass along the dispatches
	protected Signal Activated = new Signal();
	protected Signal Deactivated = new Signal();

	public Signal ActivatedSignal { get { return Activated; } }
	public Signal DeactivatedSignal { get { return Deactivated; } }

	// Does not do any closure checks, use this to spoof as an Action
	public Action Action { get { return Activated.Dispatch; } }

	IActivationSignals IBinding<IActivationSignals>.Get() { return this; }
	IActivationActions IBinding<IActivationActions>.Get() { return this; }

	// Utility class to allow us to add an update function as the listener of this activation
	// so that update function will be called once per frame while this activation is active.
	public class UpdateSignalFunctor
	{
		private UpdateSignal UpdateSignal;
		private Action Update;

		public UpdateSignalFunctor(UpdateSignal updateSignal, Action update) { UpdateSignal = updateSignal; Update = update; }

		public void AddListener() { UpdateSignal.AddListener(Update); }
		public void RemoveListener() { UpdateSignal.RemoveListener(Update); }

		public override int GetHashCode()
		{
			return UpdateSignal.GetHashCode() ^ Update.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var objFunctor = obj as UpdateSignalFunctor;
			return objFunctor != null && UpdateSignal.Equals(objFunctor.UpdateSignal) && Update.Equals(objFunctor.Update);
		}
	}

	// Lots of helpers for adding listeners
	public void AddListener(Action activated, Action deactivated)
	{
		Activated.AddListener(activated);
		Deactivated.AddListener(deactivated);
	}

	public void RemoveListener(Action activated, Action deactivated)
	{
		Activated.RemoveListener(activated);
		Deactivated.RemoveListener(deactivated);
	}
	
	public void AddListener(object key, Action activated, Action deactivated)
	{
		Activated.AddListener(key, activated);
		Deactivated.AddListener(key, deactivated);
	}

	public void RemoveListener(object key, Action activated, Action deactivated)
	{
		Activated.RemoveListener(key, activated);
		Deactivated.RemoveListener(key, deactivated);
	}

	public void AddListener(object key, UpdateSignal updateSignal, Action update)
	{
		var signalFunctor = new UpdateSignalFunctor(updateSignal, update);
		Activated.AddListener(key, signalFunctor.AddListener);
		Deactivated.AddListener(key, signalFunctor.RemoveListener);
	}

	public void RemoveListener(object key, UpdateSignal updateSignal, Action update)
	{
		var signalFunctor = new UpdateSignalFunctor(updateSignal, update);
		signalFunctor.RemoveListener();
		Activated.RemoveListener(key, signalFunctor.AddListener);
		Deactivated.RemoveListener(key, signalFunctor.RemoveListener);
	}

	public void AddListener(IActivationActions actions) { AddListener(actions.Activate, actions.Deactivate); }
	public void AddListener(object key, IActivationActions actions) { AddListener(key, actions.Activate, actions.Deactivate); }

	public void RemoveListener(IActivationActions actions) { RemoveListener(actions.Activate, actions.Deactivate); }
	public void RemoveListener(object key, IActivationActions actions) { RemoveListener(key, actions.Activate, actions.Deactivate); }

	// These listeners are special.  They mark an update function so that a per-frame update signal will call it for the duration of this activation
	public void AddListener(UpdateSignal updateSignal, Action update)
	{
		var signalFunctor = new UpdateSignalFunctor(updateSignal, update);
		Activated.AddListener(signalFunctor.AddListener);
		Deactivated.AddListener(signalFunctor.RemoveListener);
	}

	public void RemoveListener(UpdateSignal updateSignal, Action update)
	{
		var signalFunctor = new UpdateSignalFunctor(updateSignal, update);
		signalFunctor.RemoveListener();
		Activated.RemoveListener(signalFunctor.AddListener);
		Deactivated.RemoveListener(signalFunctor.RemoveListener);
	}

	public void Update(bool active)
	{
		if (active && !GetActive())
			Activate();
		else if (!active && GetActive())
			Deactivate();
	}

	public void Update(object key, bool active)
	{
		if (active && !GetActive(key))
			Activate(key);
		else if (!active && GetActive(key))
			Deactivate(key);
	}

	// To be implemented by various activation types
	public abstract void Activate();
	public abstract void Deactivate();
	public abstract void Activate(object key);
	public abstract void Deactivate(object key);
	public abstract bool GetActive();
	public abstract bool GetActive(object key);
}

// Vanilla activation, just on and off.
public class Activation : ActivationBase
{
	private bool m_active = false;
	private Dictionary<object, bool> m_keyActives = new Dictionary<object, bool>();
	public override bool GetActive() { return m_active; }
	public override bool GetActive(object key) { return m_keyActives.ContainsKey(key) && m_keyActives[key]; }

	// In editor, enforce Activation Closure, the concept that there is never a double activation or deactivation
	// Listener leaks are not cool!
	public override void Activate()
	{
		if (m_active)
		{
			Debug.LogError("Activation activated when already active!");
		}
		else
		{
			Activated.Dispatch();
			m_active = true;
		}
	}

	public override void Deactivate()
	{
		if (!m_active)
		{
			Debug.LogError("Activation deactivated when already inactive!");
		}
		else
		{
			Deactivated.Dispatch();
			m_active = false;
		}
	}

	public override void Activate(object key)
	{
		if (GetActive(key))
		{
			Debug.LogError("Activation activated when already active!");
		}
		else
		{
			Activated.Dispatch(key);
			m_keyActives[key] = true;
		}
	}

	public override void Deactivate(object key)
	{
		if (!GetActive(key))
		{
			Debug.LogError("Activation deactivated when already inactive!");
		}
		else
		{
			Deactivated.Dispatch(key);
			m_keyActives[key] = false;
		}
	}
}

// Activation that you can just pass a bool to and it will update accordingly
// DEPRECATED - Any Activation can do this now.
public class PollingActivation : Activation {}

// A multi-activation can be activated any number of times, and the count is tracked so that the outgoing activation is only signaled once
public class MultiActivation : ActivationBase
{
	private int m_activations = 0;
	private Dictionary<object, int> m_keyActivations = new Dictionary<object, int>();
	public override bool GetActive() { return m_activations > 0; }
	public override bool GetActive(object key) { return m_keyActivations.ContainsKey(key) && m_keyActivations[key] > 0; }

	public override void Activate()
	{
		if (m_activations == 0)
		{
			Activated.Dispatch();
		}
		m_activations++;
	}

	public override void Deactivate()
	{
		if (m_activations == 0)
		{
			Debug.LogError("More deactivations than activations!");
		}
		else if (m_activations == 1)
		{
			Deactivated.Dispatch();
			m_activations--;
		}
		else
			m_activations--;
	}

	public override void Activate(object key)
	{
		Activated.Dispatch(key);
		if (m_keyActivations.ContainsKey(key))
			m_keyActivations[key]++;
		else
			m_keyActivations[key] = 1;
	}

	public override void Deactivate(object key)
	{
		if (m_keyActivations.ContainsKey(key))
		{
			if (m_keyActivations[key] == 1)
			{
				Deactivated.Dispatch(key);
				m_keyActivations[key]--;
				return;
			}
			else
				m_keyActivations[key]--;
		}
		else
			m_keyActivations[key] = 0;
		Debug.LogError("More deactivations than activations!");
	}
}
