/*-------------------------------------------------------------------------+
StateMachine.cs
2014
Jacob Liechty

Definition of a state machine node graph. The graph represents a single state
In a larger machine composed of a hierarchy of such states. 
 
Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using strange.extensions.signal.impl;

public class ConstructMachineTransitionEdgeTemp<T> where T : MachineTransition, new()
{
	public object Invoke(object[] o) { return new MachineTransitionEdge<T>() { Value = new T() }; }
	public Func<object[], object> GetInvoke() { return Invoke; }
}

public class ConstructMachineTransitionEdge<T> where T : MachineTransition, new()
{
	public T Transition;
	public ConstructMachineTransitionEdge(T transition) { Transition = transition; }
	public object Invoke() { return new MachineTransitionEdge<T>() { Value = Transition }; }
}

// Node wrapper for a MachineState
public class MachineStateNode<ValueType> : ActivationCommandContainerNode<ValueType> where ValueType : MachineState, new()
{
	// Called by graph editor when edges are added or removed
	public override void AddEdge(IGraphEdge edge) { Value.Transitions.Add(edge.ValueObject as MachineTransition); }
	public override void RemoveEdge(IGraphEdge edge)
	{
		base.RemoveEdge(edge);

		if (edge is MachineTransition)
			Value.Transitions.Remove(edge as MachineTransition);
	}

	// Our transitions are our edges
	public override IEnumerable<IGraphEdge> GetEdges(INodeGraph nodeGraph) 
	{
		return Value.Transitions
			.Where(t => t != null && t.ToState != null && t.FromState != null)
			.Select(t =>
				{
					Type constructValueType = typeof(ConstructMachineTransitionEdge<>).MakeGenericType(t.GetType());
					object constructValue = constructValueType.GetConstructor(new Type[] { t.GetType() }).Invoke(new object[] { t });
					return constructValueType.GetMethod("Invoke").Invoke(constructValue, new object[] { }) as IGraphEdge;
				})
			.Concat(base.GetEdges(nodeGraph)); 
	}

	// Edge types, which are all transition types
	public override ConstructorSelection[] GetEdgeConstructors(Type viewType)
	{
		return Value.GetType().Assembly.GetTypes()
			.Where(t => !t.IsAbstract)
			.Select(t => Tuple.New(t, TypeUtil.GetGenericSubclassArguments(t, typeof(IScope<>))))
			.Where(t => t.Second.Length > 0)
			.Where(t => EditorUtil.ViewScoped(t.Second[0], viewType) || t.Second[0] == typeof(BaseContextView))
			.Select(t => t.First)
			.Where(t => typeof(MachineTransition).IsAssignableFrom(t))
			.Select(t =>
			{
				Type constructValueType = typeof(ConstructMachineTransitionEdgeTemp<>).MakeGenericType(t);
				object constructValue = constructValueType.GetConstructor(new Type[] { }).Invoke(new object[] { });
				var edgeConstruct = constructValueType.GetMethod("GetInvoke").Invoke(constructValue, new object[] { }) as Func<object[], object>;
				return new ConstructorSelection(TypeUtil.TypeName(t), edgeConstruct, new object[] { });
			}).ToArray();
	}

	// For editor context menus
#if UNITY_EDITOR
	public override void DrawMenu(UnityEditor.GenericMenu genericMenu, Action onInvalidate)
	{
		genericMenu.AddItem(new GUIContent("Toggle Starting"), false, new UnityEditor.GenericMenu.MenuFunction(() => { ToggleStartCallback(); onInvalidate(); }));
	}

	private void ToggleStartCallback()
	{
		Value.Starting = !Value.Starting;
	}
#endif
}

public class MachineTransitionEdge<TransitionType> : GraphEdge<TransitionType> where TransitionType : MachineTransition, new()
{
	public override IGraphNode GetFromNode(INodeGraph nodeGraph) { return nodeGraph.GetNode(Value.FromState); }
	public override IGraphNode GetToNode(INodeGraph nodeGraph) { return nodeGraph.GetNode(Value.ToState); }

	public override object FromObject { get { return Value.FromState; } set { Value.FromState = (MachineState)value; } }
	public override object ToObject { get { return Value.ToState; } set { Value.ToState = (MachineState)value; } }

	public override void OnConnect()
	{
		base.OnConnect();
		Value.FromState.Transitions.Add(Value);
	}
}