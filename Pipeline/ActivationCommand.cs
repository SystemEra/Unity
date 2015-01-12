/*-------------------------------------------------------------------------+
ActivationCommand.cs
2014
Jacob Liechty

Activation commands are like Strange Commands, except they have an on/off
lifecycle as opposed to being fired once.  They may be bound to activation
signals in the same way that Commands can be bound to Signals.
 
Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface IScriptInfo
{
	ScriptInfo ScriptInfo { get; }
}

public interface IBold
{
	bool IsBold { get; }
}

public interface IDisplayColor
{
	int DisplayColor { get; }
}

public abstract class EnabledCommand : IScriptInfo, IDisplayColor, ITitled, IBold, IEnableContainer, IActivationContainer
{
	public virtual ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	// IEnableContainer
	[Inject] public BaseContext context { get; set; }
	[Inject] public GameObject gameObject { get; set; }
	private List<IEnabled> m_enabledObjects = new List<IEnabled>();
	public IEnumerable<IEnabled> EnabledObjects { get { return m_enabledObjects; } set { m_enabledObjects = value as List<IEnabled>; } }

	public virtual void ContainerPostEnable() { }
	public virtual void ContainerPreDisable() { }

	public virtual int DisplayColor { get { return 0; } }
	public bool IsBold { get { return IsActivation; } }
	public virtual bool IsActivation { get { return true; } }
	public virtual string Title { get { return TypeUtil.TypeName(GetType()); } set { } }
	public bool Owned { get; set; }

	private bool m_started = false;
	public virtual void Create(BaseContext context)
	{
		if (m_started)
			Debug.LogError("Command started twice!");
		m_started = true;
		context.Inject(this);
		EnableContainerImpl.Create(this);
	}

	public virtual void Enable()
	{
		EnableContainerImpl.Enable(this);
	}

	public virtual void Disable()
	{
		EnableContainerImpl.Disable(this);
	}

	public virtual void Destroy()
	{
		EnableContainerImpl.Destroy(this);
	}
}


// Base class for nodes that can be activated.  Derived classes call Trigger(De)activated methods to define activation criteria.
public abstract class SelfActivated : EnabledCommand
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[Inject] public UpdateSignal UpdateSignal { get; set; }
	[Inject] public FixedUpdateSignal FixedUpdateSignal { get; set; }

	public override int DisplayColor { get { return 1; } }

	private bool m_hasUpdateFunction = false;
	private bool m_hasFixedUpdateFunction = false;
	protected bool m_isActive = false;
	protected bool IsActive() { return m_isActive; }

	protected virtual bool DefaultActivatedByContainer { get { return false; } }
	private static Dictionary<Type, Tuple<bool, bool>> m_reflectionCache = new Dictionary<Type, Tuple<bool, bool>>();

	public override void Create(BaseContext context)
	{
		base.Create(context);

		// See if update functions are overridden, otherwise don't add them to update loop on activation
		Tuple<bool, bool> updateValues;
		Type thisType = GetType();
		if (!m_reflectionCache.TryGetValue(thisType, out updateValues))
		{
			m_hasUpdateFunction = thisType.GetMethod("OnUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).DeclaringType != typeof(SelfActivated);
			m_hasFixedUpdateFunction = thisType.GetMethod("OnFixedUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).DeclaringType != typeof(SelfActivated);
			m_reflectionCache[thisType] = new Tuple<bool, bool>(m_hasUpdateFunction, m_hasFixedUpdateFunction);
		}
		else
		{
			m_hasUpdateFunction = updateValues.First;
			m_hasFixedUpdateFunction = updateValues.Second;
		}
	}

	public override void Enable()
	{
		base.Enable();
	}

	public override void Disable()
	{
		base.Disable();
		if (m_isActive)
			SelfDeactivate();
	}

	// When we ourselves activate, listen to the update loop
	protected virtual void SelfActivate()
	{
		OnActivated();
		if (m_hasUpdateFunction)
			UpdateSignal.AddListener(OnUpdate);
		if (m_hasFixedUpdateFunction)
			FixedUpdateSignal.AddListener(OnFixedUpdate);
		m_isActive = true;
	}

	protected virtual void SelfDeactivate()
	{
		OnDeactivated();
		if (m_hasUpdateFunction)
			UpdateSignal.RemoveListener(OnUpdate);
		if (m_hasFixedUpdateFunction)
			FixedUpdateSignal.RemoveListener(OnFixedUpdate);
		m_isActive = false;
	}

	public override void ContainerPostEnable()
	{
		base.ContainerPostEnable();
		if (DefaultActivatedByContainer)
			SelfActivate();
	}

	public override void ContainerPreDisable()
	{
		base.ContainerPreDisable();
		if (DefaultActivatedByContainer)
			SelfDeactivate();
	}

	// User classes override these to receive notification of activation.
	protected virtual void OnActivated() { }
	protected virtual void OnDeactivated() { }
	protected virtual void OnUpdate() { }
	protected virtual void OnFixedUpdate() { }
}



// The final stop for activations.  A command that can only receive its activation from elsewhere.
public abstract class ActivationCommand : SelfActivated, IEnableBindingExplicit<IActivationActions>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public class DefaultActivation : Activation { }
	public IBinding<IActivationSignals> Activation = new DefaultActivation();
	protected override bool DefaultActivatedByContainer { get { return Activation is DefaultActivation; } }
	public override int DisplayColor { get { return 1; } }

	// Bold to indicate we have full activation, not just a trigger.
	public override bool IsActivation { get { return BindingIsActivation; } }
	private bool BindingIsActivation { get { return Activation is Activation || (Activation is EnabledCommand && (Activation as EnabledCommand).IsActivation); } }

	// Allow this command's code to be used explicitly by non-containers.
	// This is not exposed in node graphs because activation commands are default activated if they are in a container and aren't bound to another activation
	// Since there is no way to track explicit invocations, we wouldn't know when to default activate
	public void Activate() { SelfActivate(); }
	public void Deactivate() { SelfDeactivate(); }
	public void Update(bool active)
	{
		if (!m_isActive && active)
			Activate();
		else if (m_isActive && !active)
			Deactivate();
	}

	private Activation m_explicitActivation = new Activation();
	IActivationActions IBinding<IActivationActions>.Get() { return m_explicitActivation; }

	// Activate self when our binding is activated
	public override void Enable()
	{
		base.Enable();
		Activation.Get().AddListener(SelfActivate, SelfDeactivate);
		m_explicitActivation.AddListener(SelfActivate, SelfDeactivate);
	}

	public override void Disable()
	{
		base.Disable();
		Activation.Get().RemoveListener(SelfActivate, SelfDeactivate);
		m_explicitActivation.RemoveListener(SelfActivate, SelfDeactivate);
	}
}

public class NamedActivationCommand : SelfActivated
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public string Name;
	public override string Title { get { return Name; } }
	public override int DisplayColor { get { return 1; } }
}

public class ActivationCommandContainer : ActivationCommand
{
	// Command List list
	[HideInInspector]
	public List<IActivationContainer> ActivationCommands = new List<IActivationContainer>();
	public virtual IEnumerable<IActivationContainer> Activations { get { return ActivationCommands; } }

	public string Name = "State";
	public override string Title { get { return Name; } set { Name = value; } }
	
	// Cache for fast iteration
	private IActivationContainer[] m_activationCommands;

	public override void Create(BaseContext context)
	{
		base.Create(context);
		ActivationCommands.RemoveAll((a) => a == null);
		m_activationCommands = Activations.ToArray();


		// As the container, we need to be the one doing the enabling/disabling.  There may be internal dependencies, but we always go first.
		foreach (var activationCommand in m_activationCommands) { activationCommand.Owned = true; }
		foreach (var activationCommand in m_activationCommands) { activationCommand.Create(context); }
	}

	public override void Destroy()
	{
		base.Destroy();
		foreach (var activationCommand in m_activationCommands) { activationCommand.Destroy(); }
	}

	// Activate/deactivate children commands
	protected override void OnActivated()
	{
		base.OnActivated();
		foreach (var activationCommand in m_activationCommands) { activationCommand.Enable(); }
		foreach (var activationCommand in m_activationCommands) { activationCommand.ContainerPostEnable(); }
	}

	protected override void OnDeactivated()
	{
		base.OnDeactivated();
		foreach (var activationCommand in m_activationCommands) { activationCommand.ContainerPreDisable(); }
		foreach (var activationCommand in m_activationCommands) { activationCommand.Disable(); }
	}
}

public interface IActivationContainer : IEnabled
{
	void ContainerPreDisable();
	void ContainerPostEnable();
}

public class BehaviorCommandContainer : ActivationCommandContainer
{
	public override void Enable()
	{
		base.Enable();
		SelfActivate();
	}

	public override void Disable()
	{
		SelfDeactivate();
		base.Disable();
	}
}

// The non-delete-able node in an Activation Signal delegate
[Name("")]
public class ActivationActionsCommandContainer : ActivationCommandContainer, IScope<BaseContextView>, IEnableBinding<IActivationActions>, IEnableBinding<Action>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[HideInInspector]
	public bool m_isActivation = false;
	public override int DisplayColor { get { return 2; } }
	public override bool IsActivation { get { return m_isActivation; } }

	private Activation m_activation = new Activation();

	public ActivationActionsCommandContainer(bool isActivation) { m_isActivation = isActivation; }
	public ActivationActionsCommandContainer() { }

	// Activate self when so signaled
	public override void Enable()
	{
		base.Enable();
		m_activation.AddListener(SelfActivate, SelfDeactivate);
	}

	public override void Disable()
	{
		base.Disable();
		m_activation.RemoveListener(SelfActivate, SelfDeactivate);
	}

	IActivationActions IBinding<IActivationActions>.Get() { return m_activation; }
	Action IBinding<Action>.Get() { return m_activation.Action; }
}

// The non-delete-able node in a Signal delegate
[Name("")]
public class ActionCommandContainer : ActivationCommandContainer, IScope<BaseContextView>, IEnableBinding<IActivationActions>, IEnableBinding<Action>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[HideInInspector]
	public bool m_isActivation = false;
	public override int DisplayColor { get { return 2; } }
	public override bool IsActivation { get { return m_isActivation; } }

	public NamedActivationSignals ActivationSignal = new NamedActivationSignals();
	public override IEnumerable<IActivationContainer> Activations { get { return base.Activations.ConcatSingle(ActivationSignal); } }

	public ActionCommandContainer(bool isActivation) { m_isActivation = isActivation; }
	public ActionCommandContainer() { }

	IActivationActions IBinding<IActivationActions>.Get() { return (ActivationSignal as IBinding<IActivationActions>).Get(); }
	Action IBinding<Action>.Get() { return (ActivationSignal as IBinding<IActivationActions>).Get().Action; }

	public override void Enable()
	{
		base.Enable();
		SelfActivate();
	}

	public override void Disable()
	{
		SelfDeactivate();
		base.Disable();
	}
}

// The non-delete-able node in an Action delegate
[Name("")]
public class ActivationSignalsCommandContainer : ActivationCommandContainer, IScope<BaseContextView>, IEnableBinding<IActivationSignals>, IEnableBinding<Signal>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[HideInInspector]
	public bool m_isActivation = false;
	public override int DisplayColor { get { return 1; } }
	public override bool IsActivation { get { return m_isActivation; } }	

	public NamedActivationCommand ActivationCommand = new NamedActivationCommand();
	public override IEnumerable<IActivationContainer> Activations { get { return base.Activations.ConcatSingle(ActivationCommand); } }

	public ActivationSignalsCommandContainer(bool isActivation) { m_isActivation = isActivation; }
	public ActivationSignalsCommandContainer() { }

	IActivationSignals IBinding<IActivationSignals>.Get() { return (ActivationCommand as IBinding<IActivationSignals>).Get(); }
	Signal IBinding<Signal>.Get() { return (ActivationCommand as IBinding<IActivationSignals>).Get().ActivatedSignal; }

	public override void Enable()
	{
		base.Enable();
		SelfActivate();
	}

	public override void Disable()
	{
		SelfDeactivate();
		base.Disable();
	}
}

public interface ISharedNodeGraph 
{
	INodeGraph NodeGraph { get; set; }
	ScriptableObject Asset { get; }
}

public class ScopedBehaviorWrapper<NodeGraphType, ViewType> where ViewType : IBaseView where NodeGraphType : INodeGraph, IEnabled, new()
{
	public NodeGraphType NodeGraph = new NodeGraphType();
}

// A shared node graph is one that uses FullInspector.SharedInstance to make a behavior that is shared among multiple objects.
// The editor exposes the option to turn any of the node graph binding types into a shared version
public class SharedNodeGraph<NodeGraphType, ViewType> : ISharedNodeGraph, IEnabled  where ViewType : IBaseView where NodeGraphType : INodeGraph, IEnabled, new()
{
	public FullInspector.SharedInstance<ScopedBehaviorWrapper<NodeGraphType, ViewType>> Behavior;
	public INodeGraph NodeGraph { get { return Behavior.Instance.NodeGraph; } set { Behavior.Instance.NodeGraph = (NodeGraphType)value; } }
	public ScriptableObject Asset { get { return Behavior; } }

	protected NodeGraphType ThisNodeGraph;
	public void Create(BaseContext context)
	{
		ThisNodeGraph = Behavior.Instance.NodeGraph.CloneCached();
		ThisNodeGraph.Create(context);
	}

	public void Enable()
	{
		ThisNodeGraph.Enable();
	}

	public void Disable()
	{
		ThisNodeGraph.Disable();
	}

	public void Destroy()
	{
		ThisNodeGraph.Destroy();
	}

	public bool Owned { get; set; }
}

public interface ISharedBehaviorNodeGraph : ISharedNodeGraph { }
public class SharedBehaviorNodeGraph<ViewType> : SharedNodeGraph<BehaviorGraph, ViewType>, ISharedBehaviorNodeGraph where ViewType : IBaseView { }

public interface ISharedActivationActionsNodeGraph : ISharedNodeGraph { }
public class SharedActivationActionsNodeGraph<ViewType> : SharedNodeGraph<ActivationActionsNodeGraph, ViewType>, ISharedActivationActionsNodeGraph, IBinding<IActivationActions> where ViewType : IBaseView
{
	IActivationActions IBinding<IActivationActions>.Get() { return (ThisNodeGraph as IBinding<IActivationActions>).Get(); }
}

public interface ISharedActionNodeGraph : ISharedNodeGraph { }
public class SharedActionNodeGraph<ViewType> : SharedNodeGraph<ActionNodeGraph, ViewType>, ISharedActionNodeGraph, IBinding<Action> where ViewType : IBaseView
{
	Action IBinding<Action>.Get() { return (ThisNodeGraph as IBinding<Action>).Get(); }
}

public interface ISharedActivationSignalsNodeGraph : ISharedNodeGraph { }
public class SharedActivationSignalsNodeGraph<ViewType> : SharedNodeGraph<ActivationSignalsNodeGraph, ViewType>, ISharedActivationSignalsNodeGraph, IBinding<IActivationSignals>, IBinding<Signal> where ViewType : IBaseView
{
	IActivationSignals IBinding<IActivationSignals>.Get() { return (ThisNodeGraph as IBinding<IActivationSignals>).Get(); }
	Signal IBinding<Signal>.Get() { return (ThisNodeGraph as IBinding<Signal>).Get(); }
}

// Signals on state activation
[Name("Activation/On Activated")]
public class StateActivatedSignal : EnabledCommand, IScope<BaseContextView>, IEnableBinding<IActivationSignals>, IEnableBinding<Signal>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }
	public override bool IsActivation { get { return false; } }
	private Activation m_activation = new Activation();
	public override int DisplayColor { get { return 2; } }

	IActivationSignals IBinding<IActivationSignals>.Get() { return m_activation; }
	Signal IBinding<Signal>.Get() { return m_activation.ActivatedSignal; }

	public override void ContainerPostEnable()
	{
		base.ContainerPostEnable();
		m_activation.Action();
	}
}

// Signals on state deactivation
[Name("Activation/On Deactivated")]
public class StateDeactivatedSignal : EnabledCommand, IScope<BaseContextView>, IEnableBinding<IActivationSignals>, IEnableBinding<Signal>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }
	public override bool IsActivation { get { return false; } }
	private Activation m_activation = new Activation();
	public override int DisplayColor { get { return 2; } }

	IActivationSignals IBinding<IActivationSignals>.Get() { return m_activation; }
	Signal IBinding<Signal>.Get() { return m_activation.ActivatedSignal; }

	public override void ContainerPreDisable()
	{
		base.ContainerPreDisable();
		m_activation.Action();
	}
}

// Signals only if the view isn't being spun up with a saved Model
[Name("Activation/First Time Activation")]
public class FirstTimeActivation : EnabledCommand, IScope<BaseContextView>, IEnableBinding<IActivationSignals>, IEnableBinding<Signal>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public override bool IsActivation { get { return false; } }
	private Activation m_activation = new Activation();
	public override int DisplayColor { get { return 2; } }

	IActivationSignals IBinding<IActivationSignals>.Get() { return m_activation; }
	Signal IBinding<Signal>.Get() { return m_activation.ActivatedSignal; }

	[Inject]
	public IBaseView IBaseView { get; set; }

	public override void ContainerPostEnable()
	{
		base.ContainerPostEnable();
		if (IBaseView.DeserializedModel == null)
			m_activation.Action();
	}
}

// Signal while parent state is activated
[Name("Activation/On Update")]
public class TriggerContinuousNode : SelfActivated, IEnableBinding<Signal>, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }
	protected override bool DefaultActivatedByContainer { get { return true; } }

	private Signal ThisSignal = new Signal();
	public override int DisplayColor { get { return 2; } }
	Signal IBinding<Signal>.Get() { return ThisSignal; }

	protected override void OnUpdate()
	{
		ThisSignal.Dispatch();
	}
}

[Name("Activation/On Fixed Update")]
public class TriggerContinuousFixedNode : SelfActivated, IEnableBinding<Signal>, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }
	protected override bool DefaultActivatedByContainer { get { return true; } }

	private Signal ThisSignal = new Signal();
	public override int DisplayColor { get { return 2; } }
	Signal IBinding<Signal>.Get() { return ThisSignal; }

	protected override void OnFixedUpdate()
	{
		ThisSignal.Dispatch();
	}
}

// An activation command that will coalesce multiple other activation actions.
[Name("")] // No node for this command
public class MultiActivationCommand : ActivationCommand, IEnableBinding<IActivationActions>
{
	public List<IBinding<IActivationActions>> m_actionList = new List<IBinding<IActivationActions>>();
	public List<IEnabled> m_enabledList = new List<IEnabled>();

	public void AddListener(IBinding<IActivationActions> action)
	{
		m_actionList.Add(action);
		if (action is IEnabled)
			m_enabledList.Add(action as IEnabled);
	}

	private Activation m_activation;
	IActivationActions IBinding<IActivationActions>.Get() { return m_activation; }

	public override void Create(BaseContext context)
	{
		base.Create(context);
		m_activation = new Activation();
		m_actionList.ForEach(a => m_activation.AddListener(a.Get()));
		m_enabledList.ForEach(e => e.Owned = true);
		m_enabledList.ForEach(e => e.Create(context));
	}

	public override void Enable() { base.Enable(); m_enabledList.ForEach(e => e.Enable()); }
	public override void Disable() { base.Disable(); m_enabledList.ForEach(e => e.Disable()); }
	public override void Destroy() { base.Destroy(); m_enabledList.ForEach(e => e.Destroy()); }
}