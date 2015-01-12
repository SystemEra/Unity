/*-------------------------------------------------------------------------+
NodeGraphEditor.cs
2014
Jacob Liechty

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;
using FullInspector;

public class NodeGraphPropertyEditor<NodeGraphType> : FullInspector.PropertyEditor<NodeGraphType> where NodeGraphType : INodeGraph
{
	public override NodeGraphType Edit(Rect region, GUIContent label, NodeGraphType element, FullInspector.fiGraphMetadata metadata)
	{
		var currentObject = metadata.GetInheritedMetadata<OwnerMetadata>().Object;
		if (GraphEditorWindow.Current != null && GraphEditorWindow.Current.graphOwner == currentObject && GraphEditorWindow.Current.nodeGraph.Get(currentObject).Equals(element))
		{
			GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontStyle = FontStyle.BoldAndItalic;
			EditorGUI.LabelField(region, label, labelStyle);
		}
		else
		{
			var layout = new FullInspector.LayoutToolkit.fiHorizontalLayout() { "Field", { "Button", 25.0f } };
			if (FullInspector.Internal.fiEditorGUI.LabeledButton(layout.GetSectionRect("Field", region), label, new GUIContent("Edit Node Graph")))
			{
				fiNestedMemberTraversal<INodeGraph> nodeGraph = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>().NestedMemberTraversal.As<INodeGraph>();
				UnityEngine.Object behavior = metadata.GetInheritedMetadata<OwnerMetadata>().Object;

				var viewScopeMetadata = metadata.GetInheritedMetadata<ViewScopeMetadata>();
				Type viewType = viewScopeMetadata != null ? viewScopeMetadata.ViewType : behavior.GetType();
				GraphEditorWindow.Init(new List<fiNestedMemberTraversal<INodeGraph>>() { nodeGraph }, behavior, viewType);
			}
		}

		return element;
	}
	public override float GetElementHeight(GUIContent label, NodeGraphType element, FullInspector.fiGraphMetadata metadata)
	{
		return 20.0f;
	}
}

[FullInspector.CustomPropertyEditor(typeof(BehaviorGraph), Inherit = true)]
public class BehaviorGraphPropertyEditor : NodeGraphPropertyEditor<BehaviorGraph> { }

[FullInspector.CustomPropertyEditor(typeof(ActionNodeGraph), Inherit = true)]
public class ActionNodeGraphPropertyEditor : NodeGraphPropertyEditor<ActionNodeGraph> { }

[FullInspector.CustomPropertyEditor(typeof(ActivationActionsNodeGraph), Inherit = true)]
public class ActivationNodeGraphPropertyEditor : NodeGraphPropertyEditor<ActivationActionsNodeGraph> { }

[FullInspector.CustomPropertyEditor(typeof(ActivationSignalsNodeGraph), Inherit = true)]
public class ActivationNodeGraphSignalPropertyEditor : NodeGraphPropertyEditor<ActivationSignalsNodeGraph> { }
