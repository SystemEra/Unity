/*-------------------------------------------------------------------------+
NodeCommands.cs
5/2014
Jacob Liechty

Node graph integration with Unity's animation systems.  Our node graphs
still function as the owners of behaviors, and completely encapsulate
any animations, which are seen as properties of the View
 
Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using System;
using UnityEngine;
using SE;

public class SetMecanimCustomParameter<T> : Command, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public string Name = "";
	public T Value = default(T);

	protected Animator animator = null;
	public Animator Animator = null;

	public override void Enable()
	{
		base.Enable();
		animator = Animator ?? gameObject.GetComponentInChildren<Animator>();
		animator.logWarnings = false;
	}

	public override string Title { get { return "Animation \"" + Name + "\" = " + Value.ToString(); } }
}

[Name("Animation/Set Mecanim Bool")]
public class SetMecanimCustomBool : SetMecanimCustomParameter<bool>
{
	public override void Execute() { animator.SetBool(Name, Value); }
}

[Name("Animation/Set Mecanim Float")]
public class SetMecanimCustomFloat : SetMecanimCustomParameter<float>
{
	public override void Execute() { animator.SetFloat(Name, Value); }
}

[Name("Animation/Set Mecanim Int")]
public class SetMecanimCustomInt : SetMecanimCustomParameter<int>
{
	public override void Execute() { animator.SetInteger(Name, Value); }
}


public class ActivateMecanimCustomParameter<T> : ActivationCommand, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public string Name = "";
	public T OffValue = default(T);
	public T OnValue = default(T);
	protected Animator animator = null;
	public Animator Animator = null;

	public override void Enable()
	{
		base.Enable();
		animator = Animator ?? gameObject.GetComponentInChildren<Animator>();
		animator.logWarnings = false;
	}

	public override string Title { get { return "Animation \"" + Name + "\": [" + OffValue.ToString() + " -> " + OnValue.ToString() + "]"; } }
}

[Name("Animation/Activate Mecanim Bool")]
public class ActivateMecanimCustomBool : ActivationCommand, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public string Name = "";
	public bool Invert = false;
	protected Animator animator = null;
	public Animator Animator = null;
	public override void Enable()
	{
		base.Enable();
		animator = Animator ?? gameObject.GetComponentInChildren<Animator>();
		animator.logWarnings = false;
		animator.SetBool(Name, Invert);
	}

	protected override void OnActivated()
	{
		animator.SetBool(Name, !Invert);
	}

	protected override void OnDeactivated()
	{
		animator.SetBool(Name, Invert);
	}

	public override string Title { get { return "Animation \"" + Name + "\"" + (Invert ? " (Inverted)" : ""); } }
}

[Name("Animation/Activate Mecanim Float")]
public class ActivateMecanimCustomFloat : ActivateMecanimCustomParameter<float>
{
	public override void Enable()
	{
		base.Enable();
		animator.SetFloat(Name, OffValue);
	}

	protected override void OnActivated()
	{
		animator.SetFloat(Name, OnValue);
	}

	protected override void OnDeactivated()
	{
		animator.SetFloat(Name, OffValue);
	}
}

[Name("Animation/Activate Mecanim Int")]
public class ActivateMecanimCustomInt : ActivateMecanimCustomParameter<int>
{
	public override void Enable()
	{
		base.Enable();
		animator.SetInteger(Name, OffValue);
	}

	protected override void OnActivated()
	{
		animator.SetInteger(Name, OnValue);
	}

	protected override void OnDeactivated()
	{
		animator.SetInteger(Name, OffValue);
	}
}
