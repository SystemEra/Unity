using UnityEngine;
using System;
using System.Collections;
using SE;

// Nodes that can be bound to for signal activation.
public abstract class ActivationSignalBase : SelfActivated, IEnableBinding<IActivationSignals>, IBinding<Signal>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	protected Activation m_activation = new Activation();
	protected Activation SignalingActivation { get { return m_activation; } }

	protected override bool DefaultActivatedByContainer { get { return true; } }
	IActivationSignals IBinding<IActivationSignals>.Get() { return m_activation; }
	Signal IBinding<Signal>.Get() { return m_activation.ActivatedSignal; }
	public override bool IsActivation { get { return true; } }
	public override int DisplayColor { get { return 2; } }

	protected virtual void SignalActivation()
	{
		m_activation.Activate();
	}

	protected virtual void SignalDeactivation()
	{
		m_activation.Deactivate();
	}
}

// Nodes that can be bound to for signal activation, but should never receive activation from anywhere else.
// On parent state activate/deactivate, we check to see if we've come in already activated, or are leaving while still activated.
public abstract class ActivationSignals : ActivationSignalBase
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	protected abstract bool Active { get; }

	public override void ContainerPostEnable()
	{
		base.ContainerPostEnable();

		// If we have spun up activated, so signal
		if (Active && IsActivation)
			SignalActivation();
	}

	public override void ContainerPreDisable()
	{
		base.ContainerPreDisable();

		// If we are still activated, signal deactivation
		if (Active && IsActivation)
			SignalDeactivation();
	}
}

public class NamedActivationSignals : ActivationSignals, IBinding<IActivationActions>, IBinding<Action>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public string Name;
	public override string Title { get { return Name; } }
	public override int DisplayColor { get { return 2; } }
	protected override bool Active { get { return SignalingActivation.GetActive(); } }
	public NamedActivationSignals() { }

	IActivationActions IBinding<IActivationActions>.Get() { return m_activation; }
	Action IBinding<Action>.Get() { return m_activation.Action; }
}

public class ReceiveSignalCommand<SignalType> : CommandBase, INamed<SignalType> where SignalType : Signal
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public override int DisplayColor { get { return 2; } }
	[Inject] public GameObject viewGameObject { get; set; }
	[Inject] public SignalType Signal { get; set; }

	public override void Enable()
	{
		base.Enable();
		Signal.AddListener(Dispatch);
		Signal.AddListener(viewGameObject, Dispatch);
	}

	public override void Disable()
	{
		base.Disable();
		Signal.RemoveListener(Dispatch);
		Signal.RemoveListener(viewGameObject, Dispatch);
	}
}

public class ReceiveSignalCommand<SignalType, A> : CommandBase<A>, INamed<SignalType> where SignalType : Signal<A>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public override int DisplayColor { get { return 2; } }
	[Inject] public GameObject viewGameObject { get; set; }
	[Inject] public SignalType Signal { get; set; }

	public override void Enable()
	{
		base.Enable();
		Signal.AddListener(Dispatch);
		Signal.AddListener(viewGameObject, Dispatch);
	}

	public override void Disable()
	{
		base.Disable();
		Signal.RemoveListener(Dispatch);
		Signal.RemoveListener(viewGameObject, Dispatch);
	}
}

public class ReceiveSignalCommand<SignalType, A, B> : CommandBase<A, B>, INamed<SignalType> where SignalType : Signal<A, B>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public override int DisplayColor { get { return 2; } }
	[Inject] public GameObject viewGameObject { get; set; }
	[Inject] public SignalType Signal { get; set; }

	public override void Enable()
	{
		base.Enable();
		Signal.AddListener(Dispatch);
		Signal.AddListener(viewGameObject, Dispatch);
	}

	public override void Disable()
	{
		base.Disable();
		Signal.RemoveListener(Dispatch);
		Signal.RemoveListener(viewGameObject, Dispatch);
	}
}

public class ReceiveSignalCommand<SignalType, A, B, C> : CommandBase<A, B, C>, INamed<SignalType> where SignalType : Signal<A, B, C>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public override int DisplayColor { get { return 2; } }
	[Inject] public GameObject viewGameObject { get; set; }
	[Inject] public SignalType Signal { get; set; }

	public override void Enable()
	{
		base.Enable();
		Signal.AddListener(Dispatch);
		Signal.AddListener(viewGameObject, Dispatch);
	}

	public override void Disable()
	{
		base.Disable();
		Signal.RemoveListener(Dispatch);
		Signal.RemoveListener(viewGameObject, Dispatch);
	}
}

public class ReceiveActivation<ActivationType> : ActivationSignals, INamed<ActivationType> where ActivationType : ActivationBase
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[Inject] public GameObject viewGameObject { get; set; }
	[Inject] public ActivationType Activation { get; set; }
	protected override bool Active
	{
		get { return Activation.GetActive() || Activation.GetActive(viewGameObject); }
	}

	public override void Enable()
	{
		base.Enable();
		Activation.AddListener(base.SignalActivation, base.SignalDeactivation);
		Activation.AddListener(viewGameObject, base.SignalActivation, base.SignalDeactivation);
	}

	public override void Disable()
	{
		base.Disable();
		Activation.RemoveListener(base.SignalActivation, base.SignalDeactivation);
		Activation.RemoveListener(viewGameObject, base.SignalActivation, base.SignalDeactivation);
	}
}

