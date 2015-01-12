using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[Name("Inputs/Dragging Mouse")]
public class CanvasClickSignal : Signal, IScope<BaseContextView> { }

[Name("Inputs/Virtual")]
public class VirtualInputActivation : 
	PollingActivatingCommand, 
	IEnableBinding<bool>, 
	IEnableBinding<float>, 
	IEnableBinding<Vector2>, 
	IEnableBinding<Vector3>,
	IEnableBinding<Signal<bool>>,
	IEnableBinding<Signal<float>>,
	IEnableBinding<Signal<Vector2>>,
	IEnableBinding<Signal<Vector3>>,
	IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[Inject] public VirtualControls Controls { get; set; }

	public string ControlName = "<Unset>";
	public bool Invert = false;
	public override string Title { get { return !Invert ? ControlName : ControlName + " Off"; } }
	private IControl Control = null;

	public VirtualInputActivation() { }
	public VirtualInputActivation(string controlName) { ControlName = controlName; }

	protected override bool Active
	{
		get
		{
			return (this as IBinding<bool>).Get();
		}
	}

	public override void Create(BaseContext context)
	{
		base.Create(context);
		try
		{
			Control = Controls.GetControl(ControlName);
		}
		catch (System.Exception e)
		{
			if (Controls != null)
			{
				Debug.LogError("\"" + ControlName + "\" is not a valid control");
			}
			throw e;
		}
	}

	protected override void SignalActivation()
	{
		if (!Invert)
		{
			base.SignalActivation();
			m_boolSignal.Dispatch(Control.GetBool());
			m_floatSignal.Dispatch(Control.GetFloat());
			m_vec2Signal.Dispatch(Control.GetVector2());
			m_vec3Signal.Dispatch(Control.GetVector3());
		}
		else
			base.SignalDeactivation();
	}

	protected override void SignalDeactivation()
	{
		if (Invert)
		{
			base.SignalActivation();
			m_boolSignal.Dispatch(Control.GetBool());
			m_floatSignal.Dispatch(Control.GetFloat());
			m_vec2Signal.Dispatch(Control.GetVector2());
			m_vec3Signal.Dispatch(Control.GetVector3());
		}
		else
			base.SignalDeactivation();
	}

	bool IBinding<bool>.Get()
	{
		return Control.GetBool();
	}
	float IBinding<float>.Get()
	{
		return Control.GetFloat();
	}
	Vector2 IBinding<Vector2>.Get()
	{
		return Control.GetVector2();
	}
	Vector3 IBinding<Vector3>.Get()
	{
		return Control.GetVector3();
	}

	private Signal<bool> m_boolSignal = new Signal<bool>();
	private Signal<float> m_floatSignal = new Signal<float>();
	private Signal<Vector2> m_vec2Signal = new Signal<Vector2>();
	private Signal<Vector3> m_vec3Signal = new Signal<Vector3>();

	Signal<bool> IBinding<Signal<bool>>.Get()
	{
		return m_boolSignal;
	}

	Signal<float> IBinding<Signal<float>>.Get()
	{
		return m_floatSignal;
	}

	Signal<Vector2> IBinding<Signal<Vector2>>.Get()
	{
		return m_vec2Signal;
	}

	Signal<Vector3> IBinding<Signal<Vector3>>.Get()
	{
		return m_vec3Signal;
	}
}
