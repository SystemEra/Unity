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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FullInspector;

// Data we need to store when doing the editor traversal

// Last known Unity object owner
public class OwnerMetadata : FullInspector.IGraphMetadataItem
{
	public UnityEngine.Object Object;
}



// Context scope
public class ViewScopeMetadata : FullInspector.IGraphMetadataItem
{
	public Type ViewType;
}

public class DefaultFolderAttribute : GameAssetNewableAttribute
{
	public string Folder = "";
	public DefaultFolderAttribute(string folder) { Folder = folder; }
}

public class DefaultFolderMetadata : FullInspector.IGraphMetadataItem
{
	public string Folder;
}

public class GameAssetNewableAttribute : System.Attribute { }

public static class EditorUtil
{
	public static fiNestedMemberTraversal SelectedItem = null;
	public static UnityEngine.Object SelectedOwner = null;
	public static object Clipboard;

	private static GameObject m_sceneTrash = null;
	public static GameObject SceneTrash
	{
		get
		{
			if (m_sceneTrash != null)
				return m_sceneTrash;
			else
			{
				m_sceneTrash = GameObject.Find("Trash Because Unity Sucks");
				if (m_sceneTrash == null)
				{
					m_sceneTrash = new GameObject("Trash Because Unity Sucks");
					m_sceneTrash.SetActive(false);
					m_sceneTrash.hideFlags = HideFlags.HideAndDontSave;
				}
				return m_sceneTrash;
			}
		}
	}

	public static bool ViewScoped(Type testView, Type view)
	{
		if (testView == view) return true;
		var parentViews = view.GetCustomAttributes(typeof(ParentScopeAttribute), true).SelectMany(a => (a as ParentScopeAttribute).Parents);
		foreach (Type parent in parentViews)
		{
			if (ViewScoped(testView, parent)) return true;
		}
		return false;
	}

	public static IEnumerable<Transform> GetChildTransforms(Transform transform)
	{
		List<Transform> childTransforms = new List<Transform>();
		foreach (Transform child in transform) { childTransforms.Add(child); }
		return childTransforms.Concat(childTransforms.SelectMany(t => GetChildTransforms(t)));
	}

	public static Vector2 GetLabelOffset(Vector2 cross, float labelWidth)
	{
		Vector2 labelOffset = new Vector2(6.0f, Mathf.Abs(cross.y) * 12.0f);
		if (cross.y > 0.0f)
		{
			labelOffset.x = -labelOffset.x - labelWidth;
		}
		if (cross.x > 0.0f)
		{
			labelOffset.y = -labelOffset.y;
		}

		return labelOffset;
	}

#if UNITY_EDITOR
	public static UnityEngine.Object DuplicateAsset(Type objectType, string folder)
	{
		string fileName = UnityEditor.EditorUtility.SaveFilePanel("Copy " + TypeUtil.TypeName(objectType), folder, "Copy " + TypeUtil.TypeName(objectType), "asset");

		UnityEngine.Object element = null;
		if (fileName != "")
		{
			fileName = "Assets" + fileName.Substring(Application.dataPath.Length, fileName.Length - Application.dataPath.Length);
			UnityEditor.AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(element), fileName);
			UnityEditor.AssetDatabase.Refresh();
		}
		return element;
	}

	public static UnityEngine.Object NewAsset(Type objectType, string folder)
	{
		var newAsset = ScriptableObject.CreateInstance(objectType);
		string fileName = UnityEditor.EditorUtility.SaveFilePanel("New " + TypeUtil.TypeName(objectType), folder, "New " + TypeUtil.TypeName(objectType), "asset");

		if (fileName != "")
		{
			fileName = "Assets" + fileName.Substring(Application.dataPath.Length, fileName.Length - Application.dataPath.Length);
			UnityEditor.AssetDatabase.CreateAsset(newAsset, fileName);
			return newAsset;
		}
		return null;
	}
#endif
}
