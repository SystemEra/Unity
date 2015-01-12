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


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using strange.extensions.mediation.api;
using SE;

public interface IValueBinding 
{
	object ValueObject { get; set; }
	IBindingBase GetSharedBinding();
}
public class ValueBinding<T> : IValueBinding, IBinding<T> where T : new()
{
	public T Value = new T();

	public object ValueObject { get { return Value; } set { Value = (T)value; } }

	T IBinding<T>.Get() { return Value; }

	public IBindingBase GetSharedBinding()
	{
		return new SharedBinding<T>();
	}
}

public interface ISharedBinding 
{
	IBindingBase GetValueBinding();
}
public class SharedBinding<T> : ISharedBinding, IBinding<T> where T : new()
{
	public FullInspector.SharedInstance<T> Instance;
	T IBinding<T>.Get() { return Instance.Instance.CloneCached(); }

	public IBindingBase GetValueBinding()
	{
		return new ValueBinding<T>();
	}
}

// Take an enabled command and adapt it into a component (view)
public abstract class EnabledCommandView<CommandType> : BaseView where CommandType : EnabledCommand, new()
{
	[FullInspector.InspectorName("Value")] public IBinding<CommandType> ValueOrInstance = new ValueBinding<CommandType>();

	protected CommandType m_value = null;
	[Enable] public CommandType EnabledValue 
	{
		get
		{
			if (m_value == null)
				m_value = ValueOrInstance.Get();
			return m_value;
		}
	}
}

// Take an activation command and adapt it into a component (view)
public abstract class ActivationCommandView<NodeType> : EnabledCommandView<NodeType> where NodeType : ActivationCommand, new()
{
	protected override void Enable()
	{
		base.Enable();
		m_value.Activate();
	}

	protected override void Disable()
	{
		base.Disable();
		m_value.Deactivate();
	}
}

public class PrefabViewActivation<ViewType> : GameObjectActivationBase, IScope<BaseContextView> where ViewType : MonoBehaviour
{
	public ViewType PrefabView = null;
	protected ViewType spawnedView = null;

	public override void Enable()
	{
		base.Enable();
		spawnedView = spawnedGameObject.GetComponent<ViewType>();
	}

	public override GameObject Get()
	{
		return PrefabView.gameObject;
	}

	public override string Title { get { return PrefabView == null ? "Activate Game Object" : "Activate " + PrefabView.name; } }
}

public abstract class SpawnGameObjectCommandView<OwnerViewType> : Command where OwnerViewType : IBaseView
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[Inject] public GameObject viewGameObject { get; set; }
	[Inject] public OwnerViewType ownerView { get; set; }
	
	public bool ParentTransform = false;
	public GameObject Prefab = null;

	protected GameObject spawnedGameObject = null;
	public override void Execute()
	{
		spawnedGameObject = (ownerView.context.contextView as BaseContextView).Instantiate(Prefab);

		if (ParentTransform) spawnedGameObject.transform.parent = viewGameObject.transform;
		spawnedGameObject.transform.position = viewGameObject.transform.position;
		spawnedGameObject.transform.rotation = viewGameObject.transform.rotation;
	}
}

public abstract class SpawnSpecifiedGameObjectCommandView<OwnerViewType> : Command<GameObject> where OwnerViewType : IBaseView
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }
	
	[Inject] public GameObject viewGameObject { get; set; }
	[Inject] public OwnerViewType ownerView { get; set; }
	
	public bool ParentTransform = false;
	
	protected GameObject spawnedGameObject = null;
	public override void Execute(GameObject Prefab)
	{
		spawnedGameObject = (ownerView.context.contextView as BaseContextView).Instantiate(Prefab, false);

		if (ParentTransform) spawnedGameObject.transform.parent = viewGameObject.transform;
		spawnedGameObject.transform.position = viewGameObject.transform.position;
		spawnedGameObject.transform.rotation = viewGameObject.transform.rotation;
	}
}


[Name("Objects/Spawn Prefab")]
public class SpawnGameObjectCommand : SpawnGameObjectCommandView<BaseContextView>, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public override string Title { get { return Prefab == null ? "Spawn Prefab" : "Activate " + Prefab.name; } }
}

public abstract class SpawnSpecifiedGameObjectCommand : SpawnSpecifiedGameObjectCommandView<BaseContextView>, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }
}

[Name("FX/Light Color")]
public class LightColorActivation : ActivationCommand, IScope<BaseContextView>
{
	private Color m_initialColor;
	public Light Target;
	public Color Color;

	public override void Create(BaseContext context)
	{
		base.Create(context);
		m_initialColor = Target.color;
	}

	protected override void OnActivated()
	{
		base.OnActivated();
		Target.color = Color;
	}

	protected override void OnDeactivated()
	{
		base.OnDeactivated();
		if (Target.color == Color)
			Target.color = m_initialColor;
	}
}


[Name("FX/Light Cookie")]
public class LightCookieActivation : ActivationCommand, IScope<BaseContextView>
{
	private Texture m_initialCookie;
	public Light Target;
	public Texture Cookie;

	public override void Create(BaseContext context)
	{
		base.Create(context);
		m_initialCookie = Target.cookie;
	}

	protected override void OnActivated()
	{
		base.OnActivated();
		Target.cookie = Cookie;
	}

	protected override void OnDeactivated()
	{
		base.OnDeactivated();
		if (Target.cookie == Cookie)
			Target.cookie = m_initialCookie;
	}
}