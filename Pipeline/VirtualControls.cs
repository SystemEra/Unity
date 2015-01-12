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
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MathHelper
{
	public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
	{
		if (val.CompareTo(min) < 0) return min;
		else if (val.CompareTo(max) > 0) return max;
		else return val;
	}
}

// Input typing rules:
// Boolean: For axes: True when any axis is non zero.
//			For buttons/keys: True when button is pressed.
// N float dimensions: For axes: First N dimensions returned
//					   For buttons: First dimension is set to maximum range when button pressed.

public interface IControl
{
	string GetName();

	// Disambuguating implementations
	bool GetBool();
	float GetFloat();
	Vector2 GetVector2();
	Vector3 GetVector3();
	float GetDimensionValue(int dimension);
	void ConnectSourceReferences(VirtualControls controls);
	void AdvanceFrame();
}

public abstract class NamedControl : ITitled
{
	public string Name = "<Unset>";
	public string GetName() { return Name; }
	public string Title { get { return Name + " (" + TypeUtil.TypeName(GetType()) + ")"; } set { } }
	public virtual void AdvanceFrame() { }
}

public class AxisControl2D : NamedControl, IControl
{
	public string AxisNameX = "<Unset>";
	public string AxisNameY = "<Unset>";
	public float DeadZone = 0.25f;

	
	public bool GetBool() { return GetVector2() != Vector2.zero; }

	protected IControl ControlX = null;
	protected IControl ControlY = null;


	public float GetFloat() { return GetVector2().magnitude; }
	public Vector2 GetVector2()
	{
		Vector2 currentValue = Vector2.zero;
		Vector2 rawValue = new Vector2(ControlX.GetFloat(), ControlY.GetFloat());

		float rawValueMag = rawValue.magnitude;
		Vector2 rawValueNorm = rawValue / rawValueMag;

		// Don't allow fast corners
		rawValueMag = Mathf.Min(1.0f, rawValueMag);

		// Lerp from zero to one magnitude from the dead zone rim to the stick limit
		if (rawValueMag > DeadZone)
			currentValue = rawValueNorm * (rawValueMag - DeadZone) / (1.0f - DeadZone);

		return currentValue;
	}

	public Vector3 GetVector3() { return GetVector2(); }

	public float GetDimensionValue(int d) { return GetVector3()[d]; }
	public void ConnectSourceReferences(VirtualControls controls)
	{
		ControlX = controls.GetControl(AxisNameX);
		ControlY = controls.GetControl(AxisNameY);
	}
}

public class AxisControl : NamedControl, IControl
{
	public int OutputDimension = 0;
	public List<ScalarSource> Sources = new List<ScalarSource>();

	public class ScalarSource
	{
		public IControl Control;	// Reconnected from SourceName after edits
		public string SourceName;
		public int SourceDimension = 0;

		// Function parameters: Currently just a*x + b
		// But could model this with a totally abstract function
		public float Scale = 1.0f;
		public float Offset = 0.0f;

		public float GetFloat()
		{
			return Control.GetDimensionValue(SourceDimension) * Scale + Offset;
		}
	}

	public float GetDimensionValue(int dimension)
	{
		return dimension == OutputDimension ? GetFloat() : 0.0f;
	}

	public float GetFloat()
	{
		return Sources.Aggregate(0.0f, (a, c) => a + c.GetFloat());
	}

	public bool GetBool()
	{
		return Sources.Any(s => s.Control.GetBool());
	}

	public Vector2 GetVector2()
	{
		return new Vector2(GetDimensionValue(0), GetDimensionValue(1));
	}

	public Vector3 GetVector3()	
	{
		return new Vector3(GetDimensionValue(0), GetDimensionValue(1), GetDimensionValue(2));
	}
	public void ConnectSourceReferences(VirtualControls controls) 
	{
		Sources.ForEach(s => s.Control = controls.GetControl(s.SourceName));
	}
}

public class ConditionControl : NamedControl, IControl
{
	private IControl m_control = null;
	private IControl m_conditionControl = null;

	public string Condition = "<Unset>";
	public string Control = "<Unset>";

	public float GetDimensionValue(int dimension)
	{
		return m_conditionControl.GetBool() ? m_control.GetDimensionValue(dimension) : 0.0f;
	}
	public float GetFloat()
	{
		return m_conditionControl.GetBool() ? m_control.GetFloat() : 0.0f;
	}

	public Vector2 GetVector2()
	{
		return m_conditionControl.GetBool() ? m_control.GetVector2() : Vector2.zero;
	}

	public Vector3 GetVector3()
	{
		return m_conditionControl.GetBool() ? m_control.GetVector3() : Vector3.zero;
	}

	public bool GetBool()
	{
		return m_conditionControl.GetBool() ? m_control.GetBool() : false;
	}

	public void ConnectSourceReferences(VirtualControls controls)
	{
		m_control = controls.GetControl(Control);
		m_conditionControl = controls.GetControl(Condition);
	}
}

public class AggregateControl : NamedControl, IControl
{
	protected List<IControl> m_controls = null;
	public List<string> Controls = new List<string>();
	public float GetDimensionValue(int dimension)
	{
		return m_controls.Aggregate(0.0f, (a, c) => a + c.GetDimensionValue(dimension));
	}
	public float GetFloat() 
	{
		return m_controls.Aggregate(0.0f, (a, c) => a + c.GetFloat());
	}

	public Vector2 GetVector2()
	{
		return m_controls.Aggregate(Vector2.zero, (a, c) => a + c.GetVector2());
	}

	public Vector3 GetVector3()
	{
		return m_controls.Aggregate(Vector3.zero, (a, c) => a + c.GetVector3());
	}

	public virtual bool GetBool()
	{
		return m_controls.Any(c => c.GetBool());
	}

	public void ConnectSourceReferences(VirtualControls controls)
	{
		m_controls = Controls.Select(c => controls.GetControl(c)).ToList();
	}
}

public class AggregateUnionControl : AggregateControl
{
	public override bool GetBool()
	{
		return m_controls.All(c => c.GetBool());
	}
}

public class VectorControl : NamedControl, IControl
{
	private List<IControl> m_dimensions;
	public List<string> Dimensions = new List<string>();

	public float GetDimensionValue(int dimension)
	{
		float f = 0.0f;
		if (m_dimensions.Count() > dimension)
		{
			f = m_dimensions.ElementAt(dimension).GetFloat(); 
		}
		return f;
	}

	public bool GetBool()
	{
		return m_dimensions.Any(d => d.GetBool());
	}

	public float GetFloat()
	{
		return GetDimensionValue(0);
	}

	public Vector2 GetVector2()
	{
		return new Vector2(GetDimensionValue(0), GetDimensionValue(1));
	}

	public Vector3 GetVector3()
	{
		return new Vector3(GetDimensionValue(0), GetDimensionValue(1), GetDimensionValue(2));
	}


	public void ConnectSourceReferences(VirtualControls controls)
	{
		m_dimensions = Dimensions.Select(d => controls.GetControl(d)).ToList();
	}
}

public class UnityInput : NamedControl, IControl
{
	public enum Type
	{
		Axis,
		Button,
		MouseButton,
		Key,
	};

	public string UnityName;
	public Type InputType;
	
	// Disambuguating implementations
	public bool GetBool()
	{
		return GetFloat() != 0.0f;
	}

	public float GetFloat()
	{
		switch (InputType)
		{
			case Type.Axis:
				return Input.GetAxis(UnityName);
			case Type.Button:
				return Input.GetButton(UnityName) ? 1.0f : 0.0f;
			case Type.MouseButton:
				int button;
				if (Int32.TryParse(UnityName, out button))
				{
					return Input.GetMouseButton(button) && (button != 0 || VirtualControls.CanvasMouseDown) ? 1.0f : 0.0f;
				}
				return 0.0f;
			case Type.Key:
				return Input.GetKey(UnityName) ? 1.0f : 0.0f;
		}
		return 0.0f;
	}

	public Vector2 GetVector2()
	{
		return new Vector2(GetFloat(), 0.0f);
	}
	public Vector3 GetVector3()
	{
		return new Vector3(GetFloat(), 0.0f, 0.0f);
	}

	public float GetDimensionValue(int dimension)
	{
		return dimension == 0 ? GetFloat() : 0.0f;
	}

	public void ConnectSourceReferences(VirtualControls controls) { }
}

public class VirtualControls
{
	public static bool CanvasMouseHover = false;
	public static bool CanvasMouseDown = false;

	public List<IControl> Controls = new List<IControl>();
	private bool m_debugDisplay = false;

	public IControl GetControl(string name)
	{
		if (name.EndsWith(" Key"))
			return GetKeyControl(name.Substring(0, name.Length - " Key".Length).ToLower());

		return Controls.First(c => c.GetName() == name);
	}

	public IControl GetKeyControl(string key)
	{
		return new UnityInput() { InputType = UnityInput.Type.Key, UnityName = key };
	}

	// @TODO: Attribute reference fields and reference target lists so we can have nice
	// creation buttons in Inspector without the need for this.
	public void BuildHierarchy()
	{
		foreach (IControl c in Controls)
		{
			c.ConnectSourceReferences(this);
		}
	}

	public void Update()
	{
		if (Input.GetKey(KeyCode.LeftShift) ||
			Input.GetKey(KeyCode.RightShift))
		{
			if (Input.GetKeyDown("c"))
			{
				m_debugDisplay = !m_debugDisplay;
			}
		}

		Controls.ForEach(c => c.AdvanceFrame());
	}

	public void OnGUI()
	{	
		if (m_debugDisplay)
		{
			BuildHierarchy();
			string text = "";
			foreach (IControl c in Controls)
			{
				text += string.Format("Name: {0}, bool({1}), float({2}), Vec2({3},{4}), Vec3({5},{6},{7})\n",
					c.GetName(), c.GetBool(), c.GetFloat(), c.GetVector2().x, c.GetVector2().y, c.GetVector3().x, c.GetVector3().y, c.GetVector3().z);
			}

			GUI.Label(new Rect(0, 0, Screen.width, Screen.height), text);
		}
	}
}

public class TriggerControl2D : NamedControl, IControl
{
	private Vector2 m_lastFrame = Vector2.zero;
	private Vector2 m_thisFrame = Vector2.zero;

	private IControl m_control;
	public string Control;

	private Vector2 GetRawVector()
	{
		Vector2 axis = m_control.GetVector2();
		if (axis.magnitude > 0.0f)
		{
			if (axis.x < 0.0f && axis.x < -Mathf.Abs(axis.y))
				return -Vector2.right;
			else if (axis.y <= 0.0f && axis.y < -Mathf.Abs(axis.x))
				return Vector2.up;
			else if (axis.x > 0.0f && axis.x > Mathf.Abs(axis.y))
				return Vector2.right;
			else if (axis.y >= 0.0f && axis.y > Mathf.Abs(axis.x))
				return -Vector2.up;
		}
		return Vector2.zero;
	}
	public override void AdvanceFrame()
	{
		m_lastFrame = m_thisFrame;
		m_thisFrame = GetRawVector();
	}

	public float GetDimensionValue(int dimension)
	{
		return GetBool() ? m_control.GetDimensionValue(dimension) : 0.0f;
	}

	public bool GetBool()
	{
		return m_lastFrame != m_thisFrame;
	}

	public float GetFloat()
	{
		return GetBool() ? m_control.GetFloat() : 0.0f;
	}

	public Vector2 GetVector2()
	{
		return GetBool() ? m_control.GetVector2() : Vector2.zero;
	}

	public Vector3 GetVector3()
	{
		return GetBool() ? m_control.GetVector3() : Vector3.zero;
	}


	public void ConnectSourceReferences(VirtualControls controls)
	{
		m_control = controls.GetControl(Control);
	}
}