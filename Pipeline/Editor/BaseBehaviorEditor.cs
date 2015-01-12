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

using FullInspector;
using FullInspector.Internal;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityObject = UnityEngine.Object;

public static class BaseBehaviorEditor
{
	public static System.Action EditDelegate;
}

public class BaseBehaviorEditor<ObjectType> : BehaviorEditor<ObjectType> where ObjectType : UnityObject
{
	protected override void OnSceneGUI(ObjectType behavior)
	{
	}

	protected override void OnEdit(Rect rect, ObjectType behavior, fiGraphMetadata metadata)
	{
		fiGraphMetadataChild childMetadata = metadata.Enter("BaseBehaviorEditor");
		childMetadata.Metadata.GetMetadata<DropdownMetadata>().OverrideDisable = true;

		childMetadata.Metadata.GetMetadata<OwnerMetadata>().Object = behavior;
		if (EditorUtil.SelectedItem != null && EditorUtil.SelectedOwner == behavior)
		{
			var target = EditorUtil.SelectedItem.Get(EditorUtil.SelectedOwner);

			var layout = new FullInspector.LayoutToolkit.fiVerticalLayout() 
			{
				{ "Label", 20.0f },
				{ "Data", DoGetHeight(target, childMetadata) },
			};

			GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontStyle = FontStyle.BoldAndItalic;
			EditorGUI.LabelField(layout.GetSectionRect("Label", rect), target is ITitled ? (target as ITitled).Title : target.ToString(), labelStyle);
			DoEdit(layout.GetSectionRect("Data", rect), GUIContent.none, target, childMetadata);
		}
		else
			DoEdit(rect, GUIContent.none, behavior, childMetadata);

		if (GUI.changed && BaseBehaviorEditor.EditDelegate != null)
			BaseBehaviorEditor.EditDelegate();
	}

	public object DoEdit(Rect rect, GUIContent label, object target, fiGraphMetadataChild childMetadata)
	{
		// We don't want to get the IObjectPropertyEditor for the given target, which extends
		// UnityObject, so that we can actually edit the property instead of getting a Unity
		// reference field. We also don't want the AbstractTypePropertyEditor, which we will get
		// if the behavior has any derived types.
		PropertyEditorChain editorChain = PropertyEditor.Get(target.GetType(), null);
		IPropertyEditor editor = editorChain.SkipUntilNot(
			typeof(FullInspector.Modules.Common.IObjectPropertyEditor),
			typeof(AbstractTypePropertyEditor));


		// Run the editor
		return editor.Edit(rect, label, target, childMetadata);
	}

	protected override float OnGetHeight(ObjectType behavior, fiGraphMetadata metadata)
	{
		fiGraphMetadataChild childMetadata = metadata.Enter("BaseBehaviorEditor");
		childMetadata.Metadata.GetMetadata<DropdownMetadata>().OverrideDisable = true;

		childMetadata.Metadata.GetMetadata<OwnerMetadata>().Object = behavior;
		float height = 0;

		object target = behavior;
		if (EditorUtil.SelectedItem != null && EditorUtil.SelectedOwner == behavior)
		{
			height += 20.0f;
			target = EditorUtil.SelectedItem.Get(EditorUtil.SelectedOwner); //jliechty
		}

		height += DoGetHeight(target, childMetadata);

		return height;
	}

	public float DoGetHeight(object target, fiGraphMetadataChild childMetadata)
	{
		// We don't want to get the IObjectPropertyEditor for the given target, which extends
		// UnityObject, so that we can actually edit the property instead of getting a Unity
		// reference field. We also don't want the AbstractTypePropertyEditor, which we will get
		// if the behavior has any derived types.

		PropertyEditorChain editorChain = PropertyEditor.Get(target.GetType(), null);
		IPropertyEditor editor = editorChain.SkipUntilNot(
			typeof(FullInspector.Modules.Common.IObjectPropertyEditor),
			typeof(AbstractTypePropertyEditor));

		return editor.GetElementHeight(GUIContent.none, target, childMetadata);
	}
	
    public static BaseBehaviorEditor<ObjectType> Instance = new BaseBehaviorEditor<ObjectType>();
}

[CustomBehaviorEditor(typeof(BaseScriptableObject), Inherit = true)] public class AssetEditor : BaseBehaviorEditor<UnityObject> {}
[CustomBehaviorEditor(typeof(BaseContextView), Inherit = true)] public class BaseContextViewEditor : BaseBehaviorEditor<UnityObject> {}
[CustomBehaviorEditor(typeof(BaseView), Inherit = true)] public class BaseViewEditor : BaseBehaviorEditor<UnityObject> {}
[CustomBehaviorEditor(typeof(BaseMediator), Inherit = true)] public class BaseMediatorEditor : BaseBehaviorEditor<UnityObject> {}
