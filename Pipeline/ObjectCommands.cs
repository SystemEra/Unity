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

// Take a virtually specified prefab and keep it enabled for the duration of the activation
public abstract class GameObjectActivationBase : ActivationCommand
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[Inject] public GameObject viewGameObject { get; set; }
	public abstract GameObject Get();
	public bool ParentTransform = true;

	protected GameObject spawnedGameObject = null;

	public override void Enable()
	{
		base.Enable();

		spawnedGameObject = (context.contextView as BaseContextView).Instantiate(Get());
		spawnedGameObject.SetActive(false);

		if (ParentTransform) spawnedGameObject.transform.parent = viewGameObject.transform;
		spawnedGameObject.transform.position = viewGameObject.transform.position;
		spawnedGameObject.transform.rotation = viewGameObject.transform.rotation;
	}

	protected override void OnActivated()
	{
		base.OnActivated();
		if (spawnedGameObject != null)
			spawnedGameObject.SetActive(true);
	}
	protected override void OnDeactivated()
	{
		base.OnDeactivated();
		if (spawnedGameObject != null)
			spawnedGameObject.SetActive(false);
	}
}

// Enable prefab specified by user
[Name("Objects/Activate Prefab")]
public class PrefabActivation : GameObjectActivationBase, IScope<BaseContextView>
{
	public GameObject Prefab = null;
	public override GameObject Get()
	{
		return Prefab;
	}

	public override string Title { get { return Prefab == null ? "Activate Prefab" : "Activate " + Prefab.name; } }
}

// Activate/deactivate an object in the hierarchy of this object
[Name("Objects/Activate Game Object")]
public class HierarchyGameObjectActivation: ActivationCommand, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	[Inject] public GameObject viewGameObject { get; set; }
	public GameObject GameObject;

	public override void Enable()
	{
		base.Enable();
		if (GameObject != null)
		GameObject.SetActive(false);
	}

	protected override void OnActivated()
	{
		base.OnActivated();
		if (GameObject != null)
			GameObject.SetActive(true);
	}
	protected override void OnDeactivated()
	{
		base.OnDeactivated();
		if (GameObject != null)
			GameObject.SetActive(false);
	}
	public override string Title { get { return GameObject == null ? "Activate Game Object" : "Activate " + GameObject.name; } }

}
