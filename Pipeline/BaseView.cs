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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// A view that can also contain serialized models, and contain enabled objects
public interface IBaseView : IPrefabSerializableView
{
	BaseContext context { get; set; }
	BaseContext parentContext { get; }
	GameObject gameObject { get; }
	MonoBehaviour behavior { get; }
	bool registeredWithContext { get; set; }
	IModel DeserializedModel { get; set; }
	IViewReference GetViewReference(GameObject prefab);
	bool Mediated { get; set; }
}

public static class BaseViewImpl
{
	// Get a serializable reference to where a view is in the hierarchy, for save games
	public static IViewReference GetViewReference(IBaseView view, GameObject prefab)
	{
		List<string> hierarchy = new List<string>();

		var t = view.behavior.transform;
		while (t != prefab.transform)
		{
			if (t == null)
				return null;

			hierarchy.Add(t.name);
			t = t.parent;
		}
		hierarchy.Reverse();

		var referenceTag = prefab.GetComponent<PrefabReferenceTag>();
		var prefabReference = referenceTag != null ? referenceTag.Tag : new PrefabSingleton();
		return new HierarchyViewReference() { Prefab = prefabReference, Hierarchy = hierarchy, Type = view.GetType().ToString() };
	}
}

public class NestedCapableView : strange.extensions.nestedcontext.impl.NestedCapableView { }

// Behaviors viewable with  custom inspector and serialized
public class BaseView : strange.extensions.nestedcontext.impl.NestedCapableView, IBaseView, IEnableContainer
{
	public new BaseContext context { get { return base.context as BaseContext; } set { base.context = value; } }
	public BaseContext parentContext { get { return context; } }
	public MonoBehaviour behavior { get { return this; } }

	// IEnableContainer
	private List<IEnabled> m_enabledObjects = new List<IEnabled>();
	public virtual IEnumerable<IEnabled> EnabledObjects { get { return m_enabledObjects; } set { m_enabledObjects = value as List<IEnabled>; } }

	// For startup from save
	public IModel DeserializedModel { get; set; }
	public bool Mediated { get; set; }

	// In Unity, Start is called after OnEnable (assuming the object is default enabled) (OnEnable is also called on subsequent enables/disables and we need those as well)
	// But we can't do anything before we get the context (which is only available on Start), so if we're default enabled we have to defer enabling until Start
	private bool m_hasStarted = false;

	protected sealed override void Start()
	{
		base.Start();
		using (var iViewBinding = Injection.Create<IBaseView>(context, this))
		using (var typeBinding = Injection.Create(context, GetType(), this))
		using (var objectBinding = Injection.Create<GameObject>(context, gameObject))
		{
			EnableContainerImpl.Create(this);
		}

		Create();
		m_hasStarted = true;
		if (gameObject.activeInHierarchy)
			OnEnable();
	}

	protected sealed override void OnDestroy()
	{
		base.OnDestroy();
		if (m_hasStarted)
		{
			EnableContainerImpl.Destroy(this);
			Destroy();
		}
	}

	// Seal OnEnable and OnDisable, which Unity calls at unfortunate times.  We'll handle it.
	protected void OnEnable()
	{
		if (m_hasStarted)
		{
			EnableContainerImpl.Enable(this);
			Enable();
		}
	}

	protected void OnDisable()
	{
		if (m_hasStarted)
		{
			Disable();
			EnableContainerImpl.Disable(this);
		}
	}

	protected virtual void Create() { }
	protected virtual void Enable() { }
	protected virtual void Disable() { }
	protected virtual void Destroy() { }

	public IViewReference GetViewReference(GameObject prefab)
	{
		return BaseViewImpl.GetViewReference(this, prefab);
	}
}
