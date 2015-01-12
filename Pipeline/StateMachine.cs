/*-------------------------------------------------------------------------+
StateMachine.cs
2014
Jacob Liechty

Machine States are objects which can be activated / deactivated by signals.
MachinesStates can also be turned on and off by the state machine behavior
of MachineTransition.  The state machine marches once per frame, checking
these edges to transition state.
 
Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


// A machine state is an activation, and can be made into a node by wrapping it with MachineStateNode
// In addition to allowing transitions, it also just wraps its children activations
[Name("Machine/State")]
public class MachineState : ActivationCommandContainer, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

#if !UNITY_EDITOR
	[Newtonsoft.ModifiedJson.JsonIgnore]
#endif

	// Activation and display
	[HideInInspector] public bool Starting = true;
	public bool Active { get; set; }
	public override int DisplayColor {	get { return (Active || (Starting && !Application.isPlaying)) ? 5 : 0; } }
	protected override bool DefaultActivatedByContainer { get { return Starting; } }

	// Our list of outgoing transition
	[HideInInspector] public List<MachineTransition> Transitions = new List<MachineTransition>();

	// Caches for fast iteration
	private MachineState[] m_subStates;
	

	// Ctor and Start
	public MachineState() { Active = false; FromTransition = null; }

	public override void Create(BaseContext context)
	{
		base.Create(context);

		m_subStates = Activations.Where(n => n is MachineState).Cast<MachineState>().ToArray();
	}
	
	// March of the machine
	protected void March()
	{
		foreach(MachineState state in m_subStates.Where(s => s.Active))
		{
			// March children
			state.March();
			
			MachineState toTransitionState = null;
			
			foreach(MachineTransition transition in state.Transitions)
			{
				// If the transition is active, transition
				if (transition.Get())
				{
					toTransitionState = transition.ToState;
					toTransitionState.FromTransition = transition;
				}
			}
			
			if (toTransitionState == null && state.FromTransition != null)
			{
				if (!state.FromTransition.Get())
				{
					if (state.FromTransition.IsBidirectional() && toTransitionState == null)
					{
						toTransitionState = state.FromTransition.FromState;
					}
				}
			}
			
			if (toTransitionState != null)
			{
				state.SelfDeactivate();
				toTransitionState.SelfActivate();
			}
			else
				state.Active = true;
		}
	}

	protected override void OnActivated()
	{
		base.OnActivated();
		Active = true;
	}

	protected override void OnDeactivated()
	{
		base.OnDeactivated();
		Active = false;
	}
	
	// The transition we most recently came from
	public MachineTransition FromTransition { get; set; }
}

public class StateMachine : MachineState
{
	protected override void OnUpdate()
	{
		base.OnUpdate();		
		
		// March the finite state machine forward
		// TODO - Detect if march is required 
		March();
	}
}

// Machine transitions
public enum TransitionType
{
	On, Off, WhileOn, WhileOff
}

public class MachineTransition : IBinding<bool>, ITitled, IBold
{
	public MachineState FromState;
	public MachineState ToState;

	public virtual bool Get() { return true; }

	public bool Bidirectional = false;
	public virtual bool IsBidirectional() { return Bidirectional; }

	public virtual string Title { get { return TypeUtil.TypeName(GetType()); } set { } }

	public bool IsBold { get { return Bidirectional; } }
}

public abstract class MachineTransition<ViewType> : MachineTransition, IScope<ViewType> { }

// User defined transition types
[HideInInspector]
public class MachineCustomTransition : MachineTransition
{
	public bool Active = false;
	public string Name = "";
	public override string Title { get { return Name; } }
	public override bool Get() { return Active; }
}
