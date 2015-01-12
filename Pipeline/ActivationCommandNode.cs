/*-------------------------------------------------------------------------+
NodeActivation.cs
2014
Jacob Liechty

Node wrappers for ActivationCommand
 
Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using strange.extensions.signal.impl;
using FullInspector;

// For nodes that wrap activation commands
public interface IActivationCommandNode
{
	EnabledCommand ActivationCommand { get; }
}

// Wrap an ActivationCommand, and also expose its bindings and interfaces
public class ActivationCommandNode<ValueType> : BindableNode<ValueType>, IEnabled, IActivationCommandNode, IActivationContainer where ValueType : EnabledCommand, new()
{
	public EnabledCommand ActivationCommand { get { return Value; } }
	public void Create(BaseContext context) { Value.Create(context); }
	public void Enable() { Value.Enable(); }
	public void Disable() { Value.Disable(); }
	public void Destroy() { Value.Destroy(); }
	public void ContainerPostEnable() { Value.ContainerPostEnable(); }
	public void ContainerPreDisable() { Value.ContainerPreDisable(); }
	public bool Owned { get; set; }
}

public interface IActivationCommandContainerNode
{
	ActivationCommandContainer ActivationCommandContainer { get; }
}

// Node for ActivationCommandContainer
public class ActivationCommandContainerNode<ValueType> : ActivationCommandNode<ValueType>, IActivationCommandContainerNode, INodeGraph where ValueType : ActivationCommandContainer, new()
{
	public ActivationCommandContainer ActivationCommandContainer { get { return Value; } }

	// Store list of nodes, which are no more than metadata that wrap the ValueType object
	[HideInInspector] public List<IGraphNode> m_nodes = new List<IGraphNode>();
	public virtual IEnumerable<IGraphNode> Nodes { get { return m_nodes.Concat(m_nodes.SelectMany(n => n.SlaveNodes)); } }
	public IEnumerable<fiMemberTraversalImmediate<IGraphNode>> NodeItems { get { return Nodes.Select((n, i) => new fiMemberTraversalImmediate<IGraphNode>(n, o => (o as ActivationCommandContainerNode<ValueType>).Nodes.ElementAt(i))); } }

	// Get types that are allowed to be sub-nodes.  These are all Activation types and also signal wrappers
	public System.Type[] GetTypes(Type viewType)
	{
		var scopedTypes = Value.GetType().Assembly.GetTypes()
			.Where(t => !t.IsAbstract)
			.Select(t => Tuple.New(t, TypeUtil.GetGenericSubclassArguments(t, typeof(IScope<>))))
			.Where(t => t.Second.Length > 0)
			.Where(t => EditorUtil.ViewScoped(t.Second[0], viewType) || t.Second[0] == typeof(BaseContextView))
			.Select(t => t.First);

		return scopedTypes
			.Where(t => typeof(EnabledCommand).IsAssignableFrom(t) && !typeof(MachineState).IsAssignableFrom(t))
			.Select(t => typeof(ActivationCommandNode<>).MakeGenericType(t))
			.Concat(scopedTypes.Where(t => typeof(MachineState).IsAssignableFrom(t)).Select(t => typeof(MachineStateNode<>).MakeGenericType(t)))
			.Concat(GetSignalTypes(scopedTypes))
			.ToArray();
	}

	// Create node types for named signals
	private IEnumerable<Type> GetSignalTypes(IEnumerable<Type> scopedTypes)
	{
		return scopedTypes.Where(t => typeof(BaseSignal).IsAssignableFrom(t))
			.Select(t =>
			{
				if (TypeUtil.IsGenericSubclass(t, typeof(Signal<>)))
					return typeof(ReceiveSignalCommand<,>).MakeGenericType(new Type[] { t }.Concat(TypeUtil.GetGenericSubclassArguments(t, typeof(Signal<>))).ToArray());
				else if (TypeUtil.IsGenericSubclass(t, typeof(Signal<,>)))
					return typeof(ReceiveSignalCommand<,,>).MakeGenericType(new Type[] { t }.Concat(TypeUtil.GetGenericSubclassArguments(t, typeof(Signal<,>))).ToArray());
				else if (TypeUtil.IsGenericSubclass(t, typeof(Signal<,,>)))
					return typeof(ReceiveSignalCommand<,,,>).MakeGenericType(new Type[] { t }.Concat(TypeUtil.GetGenericSubclassArguments(t, typeof(Signal<,,>))).ToArray());
				else
					return typeof(ReceiveSignalCommand<>).MakeGenericType(t);
			}).Concat(scopedTypes.Where(t => typeof(ActivationBase).IsAssignableFrom(t)).Select(t => typeof(ReceiveActivation<>).MakeGenericType(t))).Select(t => typeof(ActivationCommandNode<>).MakeGenericType(t));
	}

	// Find a node that wraps the provided value
	public IGraphNode GetNode(object value)
	{
		if (value == null)
			return null;
		return Nodes.Where(n => (n as IProxy).ValueObject == value).FirstOrDefault();
	}

	// Called by graph editor when nodes are added or removed
	public void AddNode(IGraphNode node)
	{
		var activationCommand = (node as IActivationCommandNode).ActivationCommand;
		Value.ActivationCommands.Add(activationCommand);
		m_nodes.Add(node);
	}

	public void RemoveNode(IGraphNode node)
	{
		if (node is IActivationCommandNode)
		{
			var activationCommand = (node as IActivationCommandNode).ActivationCommand;
			Value.ActivationCommands.Remove(activationCommand);
			m_nodes.Remove(node);
		}
		else if (node == null)
		{
			m_nodes.RemoveAll(n => n == null);
			m_nodes.RemoveAll(n => n.ValueObject == null);
			Value.ActivationCommands.RemoveAll(a => a == null);
		}
	}
}

// Derived classes can specify a boolean every frame, and this class will trigger the proper activation signals
public abstract class PollingActivatingCommand : ActivationSignalBase, IEnableBinding<bool>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	protected abstract bool Active { get; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		bool active = Active;

		// Make sure we perform these checks.  It's possible that in between update functions the value can change.
		if (active && !SignalingActivation.GetActive())
			SignalActivation();
		else if (!active && SignalingActivation.GetActive())
			SignalDeactivation();
	}

	public override void ContainerPostEnable()
	{
		base.ContainerPostEnable();

		// If we have spun up active, so signal
		if (Active && !SignalingActivation.GetActive())
			SignalActivation();
	}

	public override void ContainerPreDisable()
	{
		base.ContainerPreDisable();

		// If we are still active when exiting, signal deactivation
		if (SignalingActivation.GetActive())
			SignalDeactivation();
	}
	bool IBinding<bool>.Get() { return Active; }
}

// For in place construction of IEnableActivated as a behavior node graph
public class BehaviorGraph : ActivationCommandContainerNode<BehaviorCommandContainer>
{
	public BehaviorGraph() { Value.Name = "Behavior"; }
	public BehaviorGraph(string name) { Value.Name = name; }
}

// For in place construction of IEnableBinding<IActivationActions> as a node graph
public class ActivationActionsNodeGraph : ActivationCommandContainerNode<ActivationActionsCommandContainer>, IEnableBinding<IActivationActions>, IEnableBinding<Action>
{
	public ActivationActionsNodeGraph(string name) { Value.Name = name; }

	public ActivationActionsNodeGraph() { Value.Name = "Activation"; }

	IActivationActions IBinding<IActivationActions>.Get() { return (Value as IBinding<IActivationActions>).Get(); }
	Action IBinding<Action>.Get() { return (Value as IBinding<Action>).Get(); }
}

// For in place construction of IEnableBinding<Action> as a node graph
public class ActionNodeGraph : ActivationCommandContainerNode<ActionCommandContainer>, IEnableBinding<IActivationActions>, IEnableBinding<Action>
{
	public ActivationCommandNode<NamedActivationSignals> m_node = new ActivationCommandNode<NamedActivationSignals>();
	public override IEnumerable<IGraphNode> Nodes { get { return base.Nodes.ConcatSingle(m_node); } }

	public ActionNodeGraph(string name)
	{
		m_node.Value = Value.ActivationSignal;
		m_node.Position = new Rect(200.0f, 200.0f, 150.0f, 30.0f);
		m_node.Value.Name = name;
	}

	public ActionNodeGraph()
	{
		m_node.Value = Value.ActivationSignal;
		m_node.Position = new Rect(200.0f, 200.0f, 150.0f, 30.0f);
		m_node.Value.Name = "Action";
	}

	IActivationActions IBinding<IActivationActions>.Get() { return (Value as IBinding<IActivationActions>).Get(); }
	Action IBinding<Action>.Get() { return (Value as IBinding<Action>).Get(); }
}

// For in place construction of IEnableBinding<IActivationSignals> as a node graph
public class ActivationSignalsNodeGraph : ActivationCommandContainerNode<ActivationSignalsCommandContainer>, IEnableBinding<IActivationSignals>, IEnableBinding<Signal>
{
	public ActivationCommandNode<NamedActivationCommand> m_node = new ActivationCommandNode<NamedActivationCommand>();
	public override IEnumerable<IGraphNode> Nodes { get { return base.Nodes.ConcatSingle(m_node); } }

	public ActivationSignalsNodeGraph(string name, bool isActivation)
	{
		m_node.Value = Value.ActivationCommand;
		m_node.Position = new Rect(200.0f, 200.0f, 150.0f, 30.0f);
		m_node.Value.Name = name;
	}

	public ActivationSignalsNodeGraph()
	{
		m_node.Value = Value.ActivationCommand;
		m_node.Position = new Rect(200.0f, 200.0f, 150.0f, 30.0f);
		m_node.Value.Name = "Signal";
	}

	IActivationSignals IBinding<IActivationSignals>.Get() { return (Value.ActivationCommand as IBinding<IActivationSignals>).Get(); }
	Signal IBinding<Signal>.Get() { return (Value.ActivationCommand as IBinding<Signal>).Get(); }
}
