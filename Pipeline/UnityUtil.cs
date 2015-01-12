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
