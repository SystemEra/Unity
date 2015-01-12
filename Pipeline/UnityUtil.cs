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

public static class UnityUtil
{
	public static Transform FindRecursive(this Transform t, string name)
	{
		return (from x in t.gameObject.GetComponentsInChildren<Transform>()
				where x.name == name
				select x).First();
	}

	public static void SetMeshRendererMaterials(GameObject gameObject, Material material)
	{
		var moduleRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
		foreach (var renderer in moduleRenderers)
		{
			renderer.materials = renderer.materials.Select(m => material).ToArray();
		}
	}

	private static void ProcessChild<T>(Transform aObj, ref List<T> aList) where T : Component
	{
		T c = aObj.GetComponent<T>();
		if (c != null)
			aList.Add(c);
		foreach (Transform child in aObj)
			ProcessChild<T>(child, ref aList);
	}

	public static T[] GetAllComponentsInChildren<T>(this GameObject aObj) where T : Component
	{
		List<T> result = new List<T>();
		ProcessChild<T>(aObj.transform, ref result);
		return result.ToArray();
	}
}
