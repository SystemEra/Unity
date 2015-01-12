using UnityEditor;
using UnityEngine;
using System.Collections;
using FullInspector;

[CustomPropertyEditor(typeof(ParameterRangeReal), Inherit = true)]
public class RangeEditor : PropertyEditor<ParameterRangeReal>
{
	public override ParameterRangeReal Edit(Rect region, GUIContent label, ParameterRangeReal element, fiGraphMetadata metadata)
	{
		if (element == null)
			element = new ParameterRangeReal();

		var layout = new FullInspector.LayoutToolkit.fiHorizontalLayout() { { "Label", region.width / 3.0f + 8.0f}, { "Min", 70.0f }, { "Max", 70.0f } };

		float labelWidth = EditorGUIUtility.labelWidth;

		EditorGUIUtility.labelWidth = 50.0f;
		EditorGUI.LabelField(layout.GetSectionRect("Label", region), label);

		EditorGUIUtility.labelWidth = 30.0f;
		element.Min = EditorGUI.FloatField(layout.GetSectionRect("Min", region), "Min", element.Min);

		EditorGUIUtility.labelWidth = 30.0f;
		element.Max = EditorGUI.FloatField(layout.GetSectionRect("Max", region), "Max", element.Max);

		EditorGUIUtility.labelWidth = labelWidth;
		return element;
	}

	public override float GetElementHeight(GUIContent label, ParameterRangeReal element, fiGraphMetadata metadata)
	{
		if (element == null)
			return 0.0f;

		return 16.0f;
	}
}