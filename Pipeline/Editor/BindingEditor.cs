using UnityEngine;
using System.Collections;
using System.Linq;
using FullInspector;

[FullInspector.CustomPropertyEditor(typeof(IEnabled), Inherit = true)]
public class EnableActivatedPropertyEditor : FullInspector.PropertyEditor<IEnabled>
{
	public override IEnabled Edit(Rect region, GUIContent label, IEnabled element, fiGraphMetadata metadata)
	{
		var memberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
		var currentObject = metadata.GetInheritedMetadata<OwnerMetadata>().Object;
		if (element == null && memberItem.StorageType == typeof(IEnabled))
		{
			element = new BehaviorGraph(label.text);
			GUI.changed = true;
		}

		if (memberItem.StorageType == typeof(IEnabled) && element is BehaviorGraph && !(currentObject is Asset))
		{
			var viewScopeMetadata = metadata.GetInheritedMetadata<ViewScopeMetadata>();
			System.Type viewType = viewScopeMetadata != null ? viewScopeMetadata.ViewType : typeof(BaseContextView);

			if (viewType != null)
			{
				var layout = new FullInspector.LayoutToolkit.fiHorizontalLayout() { "Field", { "Button", 25.0f } };
				if (GUI.Button(layout.GetSectionRect("Button", region), new GUIContent("\u2261")))
				{
					element = typeof(SharedBehaviorNodeGraph<>).MakeGenericType(viewType).InvokeDefaultConstructor() as IEnabled;
					GraphEditorWindow.Init(null, null, null);
				}
			}
		}

		if (element is BehaviorGraph)
			return new BehaviorGraphPropertyEditor().Edit(region, label, element as BehaviorGraph, metadata);
		else
		{
			if (element is ISharedBehaviorNodeGraph)
			{
				var layoutHorizontal = new FullInspector.LayoutToolkit.fiHorizontalLayout() { "Field", { "Button", 23.0f } };
				var layoutVertical = new FullInspector.LayoutToolkit.fiVerticalLayout() { { "Button", 18.0f } };
				var buttonRect = layoutHorizontal.GetSectionRect("Button", layoutVertical.GetSectionRect("Button", region));				

				if (GUI.Button(buttonRect, new GUIContent("-")))
				{
					element = new BehaviorGraph(label.text);
				}
			}

			fiGraphMetadataChild childMetadata = metadata.Enter("DefaultBehaviorEditor");
			childMetadata.Metadata.GetMetadata<FullInspector.Internal.DropdownMetadata>().OverrideDisable = true;

			PropertyEditorChain editorChain = FullInspector.PropertyEditor.Get(element != null ? element.GetType() : memberItem.StorageType, null);
			IPropertyEditor editor = editorChain.SkipUntilNot(typeof(EnableActivatedPropertyEditor), typeof(FullInspector.Internal.AbstractTypePropertyEditor));
			return editor.Edit(region, label, element, childMetadata);
		}
	}

	public override float GetElementHeight(GUIContent label, IEnabled element, fiGraphMetadata metadata)
	{
		if (element is BehaviorGraph)
			return new BehaviorGraphPropertyEditor().GetElementHeight(label, element as BehaviorGraph, metadata);
		else
		{
			var memberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
			fiGraphMetadataChild childMetadata = metadata.Enter("DefaultBehaviorEditor");
			childMetadata.Metadata.GetMetadata<FullInspector.Internal.DropdownMetadata>().OverrideDisable = true;

			PropertyEditorChain editorChain = FullInspector.PropertyEditor.Get(element != null ? element.GetType() : memberItem.StorageType, null);
			IPropertyEditor editor = editorChain.SkipUntilNot(typeof(EnableActivatedPropertyEditor), typeof(FullInspector.Internal.AbstractTypePropertyEditor));
			return editor.GetElementHeight(label, element, childMetadata);
		}
	}
}

[FullInspector.CustomPropertyEditor(typeof(IValueBinding), Inherit = true)]
public class IValueBindingPropertyEditor : FullInspector.PropertyEditor<IValueBinding>
{
	public override IValueBinding Edit(Rect region, GUIContent label, IValueBinding element, fiGraphMetadata metadata)
	{
		fiGraphMetadataChild childMetadata = metadata.Enter("DefaultBehaviorEditor");
		childMetadata.Metadata.GetMetadata<FullInspector.Internal.DropdownMetadata>().OverrideDisable = true;

		PropertyEditorChain editorChain = FullInspector.PropertyEditor.Get(element.ValueObject.GetType(), null);
		IPropertyEditor editor = editorChain.SkipUntilNot(typeof(IValueBindingPropertyEditor), typeof(EnableActivatedPropertyEditor), typeof(FullInspector.Internal.AbstractTypePropertyEditor));
		element.ValueObject = editor.Edit(region, label, element.ValueObject, childMetadata);
		return element;
	}

	public override float GetElementHeight(GUIContent label, IValueBinding element, fiGraphMetadata metadata)
	{
		fiGraphMetadataChild childMetadata = metadata.Enter("DefaultBehaviorEditor");
		childMetadata.Metadata.GetMetadata<FullInspector.Internal.DropdownMetadata>().OverrideDisable = true;

		PropertyEditorChain editorChain = FullInspector.PropertyEditor.Get(element.ValueObject.GetType(), null);
		IPropertyEditor editor = editorChain.SkipUntilNot(typeof(IValueBindingPropertyEditor), typeof(EnableActivatedPropertyEditor), typeof(FullInspector.Internal.AbstractTypePropertyEditor));
		return editor.GetElementHeight(label, element.ValueObject, childMetadata);
	}
}

[CustomPropertyEditor(typeof(IBindingBase), Inherit = true)]
public class BindingPropertyEditor : PropertyEditor<IBindingBase>
{
	public override IBindingBase Edit(Rect region, GUIContent label, IBindingBase element, fiGraphMetadata metadata)
	{
		var memberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
		var currentObject = metadata.GetInheritedMetadata<OwnerMetadata>().Object;

		bool isStorageBindingType = memberItem != null && memberItem.StorageType.IsGenericType && (memberItem.StorageType.GetGenericTypeDefinition() == typeof(IEnableBinding<>) || memberItem.StorageType.GetGenericTypeDefinition() == typeof(IBinding<>));
		bool inNodeGraph = GraphEditorWindow.Current != null && GraphEditorWindow.Current.graphOwner == currentObject;
		bool isImmediateValue = memberItem != null && element != null && (!inNodeGraph || (inNodeGraph && GraphEditorWindow.Current.nodeGraph.Get(currentObject).GetNode(element) == null));

		if (isStorageBindingType && !isImmediateValue)
		{
			// If we are not in the node editor, display the option to create a node graph as a delegate!
			bool isSelectedNode = inNodeGraph && (GraphEditorWindow.Current.nodeGraph.Get(currentObject) == element || !(element is INodeGraph));
			if (!isSelectedNode)
			{
				if (element == null)
				{
					bool isDelegateTypeAction = memberItem != null && (memberItem.StorageType == typeof(IBinding<System.Action>) || memberItem.StorageType == typeof(IEnableBinding<System.Action>));
					bool isDelegateTypeActivationActions = memberItem != null && (memberItem.StorageType == typeof(IBinding<IActivationActions>) || memberItem.StorageType == typeof(IEnableBinding<IActivationActions>));
					bool isDelegateTypeActivationSignals = memberItem != null && (memberItem.StorageType == typeof(IBinding<Signal>) || memberItem.StorageType == typeof(IBinding<IActivationSignals>) || memberItem.StorageType == typeof(IEnableBinding<Signal>) || memberItem.StorageType == typeof(IEnableBinding<IActivationSignals>));

					GUI.changed = true;
					if (isDelegateTypeAction)
						element = new ActionNodeGraph(label.text);
					else if (isDelegateTypeActivationActions)
						element = new ActivationActionsNodeGraph(label.text);
					else if (isDelegateTypeActivationSignals)
						element = new ActivationSignalsNodeGraph(label.text, memberItem.StorageType == typeof(IEnableBinding<IActivationSignals>));
				}
			}
			else
				return element;
		}

		// If we're an in-place node graph, display the option to make us shared
		if (isStorageBindingType && !(element is ISharedBinding) && !(element is ISharedNodeGraph) && !typeof(ISharedNodeGraph).IsAssignableFrom(memberItem.DeclaringType) && !(currentObject is Asset))
		{
			var viewScopeMetadata = metadata.GetInheritedMetadata<ViewScopeMetadata>();
			System.Type viewType = viewScopeMetadata != null ? viewScopeMetadata.ViewType : typeof(BaseContextView);

			if (viewType != null)
			{
				var layout = new FullInspector.LayoutToolkit.fiHorizontalLayout() { "Field", { "Button", 25.0f } };
				if (element is ActivationActionsNodeGraph)
				{
					if (GUI.Button(layout.GetSectionRect("Button", region), new GUIContent("\u2261")))
					{
						element = typeof(SharedActivationActionsNodeGraph<>).MakeGenericType(viewType).InvokeDefaultConstructor() as IBindingBase;
						GraphEditorWindow.Init(null, null, null);
					}
				}
				else if (element is ActionNodeGraph)
				{
					if (GUI.Button(layout.GetSectionRect("Button", region), new GUIContent("\u2261")))
					{
						element = typeof(SharedActionNodeGraph<>).MakeGenericType(viewType).InvokeDefaultConstructor() as IBindingBase;
						GraphEditorWindow.Init(null, null, null);
					}
				}
				else if (element is ActivationSignalsNodeGraph)
				{
					if (GUI.Button(layout.GetSectionRect("Button", region), new GUIContent("\u2261")))
					{
						element = typeof(SharedActivationSignalsNodeGraph<>).MakeGenericType(viewType).InvokeDefaultConstructor() as IBindingBase;
						GraphEditorWindow.Init(null, null, null);
					}
				}
				else if (element is IValueBinding)
				{
					var layoutHorizontal = new FullInspector.LayoutToolkit.fiHorizontalLayout() { "Field", { "Button", 23.0f } };
					var layoutVertical = new FullInspector.LayoutToolkit.fiVerticalLayout() { { "Button", 18.0f } };
					var buttonRect = layoutHorizontal.GetSectionRect("Button", layoutVertical.GetSectionRect("Button", region));	
					if (GUI.Button(buttonRect, new GUIContent("\u2261")))
					{
						element = (element as IValueBinding).GetSharedBinding();
					}
				}
			}
		}
		else if (element is ISharedNodeGraph || element is ISharedBinding)
		{
			var layoutHorizontal = new FullInspector.LayoutToolkit.fiHorizontalLayout() { "Field", { "Button", 23.0f } };
			var layoutVertical = new FullInspector.LayoutToolkit.fiVerticalLayout() { { "Button", 18.0f } };
			var buttonRect = layoutHorizontal.GetSectionRect("Button", layoutVertical.GetSectionRect("Button", region));				

			// If we're a shared node graph, display the option to make us in-place
			if (element is ISharedActivationActionsNodeGraph)
			{
				if (GUI.Button(buttonRect, new GUIContent("-")))
				{
					element = new ActivationActionsNodeGraph(label.text);
				}
			}
			else if (element is ISharedActionNodeGraph)
			{
				if (GUI.Button(buttonRect, new GUIContent("-")))
				{
					element = new ActionNodeGraph(label.text);
				}
			}
			else if (element is ISharedActivationSignalsNodeGraph)
			{
				if (GUI.Button(buttonRect, new GUIContent("-")))
				{
					element = new ActivationSignalsNodeGraph(label.text, memberItem.StorageType == typeof(IEnableBinding<IActivationSignals>));
				}
			}
			else if (element is ISharedBinding)
			{
				if (GUI.Button(buttonRect, new GUIContent("-")))
				{
					element = (element as ISharedBinding).GetValueBinding();
				}
			}
		}

		fiGraphMetadataChild childMetadata = metadata.Enter("DefaultBehaviorEditor");
		childMetadata.Metadata.GetMetadata<FullInspector.Internal.DropdownMetadata>().OverrideDisable = true;

		PropertyEditorChain editorChain = FullInspector.PropertyEditor.Get(element != null ? element.GetType() : memberItem.StorageType, null);
		IPropertyEditor editor = editorChain.SkipUntilNot(typeof(EnableActivatedPropertyEditor), typeof(BindingPropertyEditor), typeof(FullInspector.Internal.AbstractTypePropertyEditor));
		element = editor.Edit(region, label, element, childMetadata);

		return element;
	}

	public override float GetElementHeight(GUIContent label, IBindingBase element, fiGraphMetadata metadata)
	{
		var memberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
		var currentObject = metadata.GetInheritedMetadata<OwnerMetadata>().Object;

		bool isStorageBindingType = memberItem != null && memberItem.StorageType.IsGenericType && (memberItem.StorageType.GetGenericTypeDefinition() == typeof(IEnableBinding<>) || memberItem.StorageType.GetGenericTypeDefinition() == typeof(IBinding<>));
		bool inNodeGraph = GraphEditorWindow.Current != null && GraphEditorWindow.Current.graphOwner == currentObject;
		bool isImmediateValue = memberItem != null && element != null && (!inNodeGraph || (inNodeGraph && GraphEditorWindow.Current.nodeGraph.Get(currentObject).GetNode(element) == null));

		if (isStorageBindingType && !isImmediateValue)
		{
			// If we are not in the node editor, display the option to create a node graph as a delegate!
			if (GraphEditorWindow.Current != null && GraphEditorWindow.Current.graphOwner == currentObject && (GraphEditorWindow.Current.nodeGraph.Get(currentObject) == element || !(element is INodeGraph)))
			{
				return 0.0f;
			}
		}

		if (element != null)
		{
			fiGraphMetadataChild childMetadata = metadata.Enter("DefaultBehaviorEditor");
			childMetadata.Metadata.GetMetadata<FullInspector.Internal.DropdownMetadata>().OverrideDisable = true;

			PropertyEditorChain editorChain = FullInspector.PropertyEditor.Get(element != null ? element.GetType() : memberItem.StorageType, null);
			IPropertyEditor editor = editorChain.SkipUntilNot(typeof(EnableActivatedPropertyEditor), typeof(BindingPropertyEditor), typeof(FullInspector.Internal.AbstractTypePropertyEditor));
			return editor.GetElementHeight(label, element, childMetadata);
		}
		else
			return 0.0f;
	}
}

[CustomPropertyEditor(typeof(Activation), Inherit = true)]
public class ActivationPropertyEditor : PropertyEditor<Activation>
{
	public override Activation Edit(Rect region, GUIContent label, Activation element, fiGraphMetadata metadata)
	{
		return element;
	}

	public override float GetElementHeight(GUIContent label, Activation element, fiGraphMetadata metadata)
	{
		return 0.0f;
	}
}


[CustomPropertyEditor(typeof(strange.extensions.signal.api.IBaseSignal), Inherit = true)]
public class SignalPropertyEditor : PropertyEditor<strange.extensions.signal.api.IBaseSignal>
{
	public override strange.extensions.signal.api.IBaseSignal Edit(Rect region, GUIContent label, strange.extensions.signal.api.IBaseSignal element, fiGraphMetadata metadata)
	{
		return element;
	}

	public override float GetElementHeight(GUIContent label, strange.extensions.signal.api.IBaseSignal element, fiGraphMetadata metadata)
	{
		return 0.0f;
	}
}

public class SharedNodeGraphEditor<SharedType, NodeGraphType> : FullInspector.PropertyEditor<SharedType> where NodeGraphType : INodeGraph, new() where SharedType : ISharedNodeGraph
{
	public override SharedType Edit(Rect region, GUIContent label, SharedType element, fiGraphMetadata metadata)
	{
		if (element.Asset != null && element.NodeGraph == null)
			element.NodeGraph = new NodeGraphType();

		var memberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
		fiGraphMetadataChild childMetadata = metadata.Enter("DefaultBehaviorEditor");
		childMetadata.Metadata.GetMetadata<FullInspector.Internal.DropdownMetadata>().OverrideDisable = true;

		PropertyEditorChain editorChain = FullInspector.PropertyEditor.Get(element != null ? element.GetType() : memberItem.StorageType, null);
		IPropertyEditor editor = editorChain.SkipUntilNot(typeof(EnableActivatedPropertyEditor), this.GetType(), typeof(BindingPropertyEditor), typeof(FullInspector.Internal.AbstractTypePropertyEditor));
		return editor.Edit(region, label, element, childMetadata);
	}

	public override float GetElementHeight(GUIContent label, SharedType element, fiGraphMetadata metadata)
	{
		if (element.Asset != null && element.NodeGraph == null)
			element.NodeGraph = new NodeGraphType();

		var memberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
		fiGraphMetadataChild childMetadata = metadata.Enter("DefaultBehaviorEditor");
		childMetadata.Metadata.GetMetadata<FullInspector.Internal.DropdownMetadata>().OverrideDisable = true;

		PropertyEditorChain editorChain = FullInspector.PropertyEditor.Get(element != null ? element.GetType() : memberItem.StorageType, null);
		IPropertyEditor editor = editorChain.SkipUntilNot(typeof(EnableActivatedPropertyEditor), this.GetType(), typeof(BindingPropertyEditor), typeof(FullInspector.Internal.AbstractTypePropertyEditor));
		return editor.GetElementHeight(label, element, childMetadata);
	}
}

[FullInspector.CustomPropertyEditor(typeof(ISharedBehaviorNodeGraph))]
public class SharedBehaviorEditor : SharedNodeGraphEditor<ISharedBehaviorNodeGraph, BehaviorGraph> { }

[FullInspector.CustomPropertyEditor(typeof(ISharedActivationActionsNodeGraph))]
public class SharedActivationActionsEditor : SharedNodeGraphEditor<ISharedActivationActionsNodeGraph, ActivationActionsNodeGraph> { }

[FullInspector.CustomPropertyEditor(typeof(ISharedActionNodeGraph))]
public class SharedActionEditor : SharedNodeGraphEditor<ISharedActionNodeGraph, ActionNodeGraph> 
{
	public override ISharedActionNodeGraph Edit(Rect region, GUIContent label, ISharedActionNodeGraph element, fiGraphMetadata metadata)
	{
		return base.Edit(region, label, element, metadata);
	}
}

[FullInspector.CustomPropertyEditor(typeof(ISharedActivationSignalsNodeGraph))]
public class SharedSignalEditor : SharedNodeGraphEditor<ISharedActivationSignalsNodeGraph, ActivationSignalsNodeGraph> { }