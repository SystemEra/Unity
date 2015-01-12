/*-------------------------------------------------------------------------+
NodeBindings.cs
2014
Jacob Liechty

Support for data binding between nodes.

BindableNode<T> wraps any object and exposes the IBindings that it implements,
as well as any [Expose]d fields
 
NodeBinding<T> is the Edge class that represents the connection between
a BindableNode and a BindingNode.

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using FullInspector;

public class HideInNodeEditorAttribute : System.Attribute 
{
	public string ControllingField = null;
	public HideInNodeEditorAttribute(string controllingField) { ControllingField = controllingField; }
	public HideInNodeEditorAttribute() { }
}

// Functor to construct temporary NodeBinding<T>'s that are used when dragging new edges
public class ConstructValueBindingEdgeFieldTemp<T>
{
	private object Node;
	protected FieldInfo FieldInfo;
	public ConstructValueBindingEdgeFieldTemp(object node, FieldInfo fieldInfo) { Node = node;  FieldInfo = fieldInfo; }
	public object Invoke(object[] o)
	{		
		var oldEdge = FieldInfo.GetValue(Node) as IGraphEdge;
		if (oldEdge != null)
			oldEdge.OnDisconnect();

		var newBinding = new NodeBinding<T>(FieldInfo);

		return newBinding;
	}

	public Func<object[], object> GetInvoke() { return Invoke; }
}

// Functor to make NodeBinding<T>'s that represent edges, used transiently each frame for rendering and editing of objects in node graphs
public class ConstructValueBindingEdgeField<T>
{
	protected FieldInfo FieldInfo;
	public ConstructValueBindingEdgeField(FieldInfo fieldInfo) { FieldInfo = fieldInfo; }

	public NodeBinding<T> Get(object fromObject, IBinding<T> toBinding)
	{
		return new NodeBinding<T>(FieldInfo) { FromObject = fromObject, ToBinding = toBinding, Value = new Ref<IBinding<T>>(toBinding) };
	}
}

// Use reflection to find fields of type IBinding<> or List<IBinding<>>, and expose those as graph edges
public abstract class BindableNode<ValueType> : GraphNode<ValueType> where ValueType : new()
{
	// Injection done by containing MachineState
	[Inject] public BaseContext context { get; set; }
	[Inject] public GameObject gameObject { get; set; }
	
	// We never save edges, only reflect them with GetEdges
	public override void AddEdge(IGraphEdge edge) { }

	// Delete internally the reference that this edge represents
	public override void RemoveEdge(IGraphEdge edge)	
	{
		if (edge.ToObject == null)
			return;

		if (Value == null)
			return;

		Value
			.GetType()
			.GetFields()
			.Where(f => f.IsPublic && !f.IsStatic && f.FieldType.IsGenericType && (f.FieldType.GetGenericTypeDefinition() == typeof(IBinding<>) || f.FieldType.GetGenericTypeDefinition() == typeof(IEnableBinding<>)))
			.Where(f => f.GetValue(Value) == edge.ToObject)
			.ToList()
			.ForEach(f => f.SetValue(Value, f.GetValue(Value.GetType().GetConstructor(new Type[] { }).Invoke(new object[] { }))));
	}

	public struct SlaveNode
	{
		public IGraphNode Node;
		public Func<bool> Visible;
	}

	private SlaveNode[] m_slaveNodes = null;

	// Nodes that represent internal fields that are tagged with [Expose]
	public override IEnumerable<IGraphNode> SlaveNodes
	{
		get
		{
			if (Value == null)
				return Enumerable.Empty<IGraphNode>();
			
			// Cache off the slave nodes we create, using functors for whether they should be 
			// visible based on attributes
			if (m_slaveNodes == null)
				m_slaveNodes = Value
				.GetType()
				.GetFields()
				.Where(f => f.IsPublic && !f.IsStatic)
				.Where(f =>
				{
					// Check for any non-parameterized hide attributes
					var hideAttributes = f.GetCustomAttributes(typeof(HideInNodeEditorAttribute), true);
					var hideAttribute = hideAttributes.Any() ? hideAttributes[0] as HideInNodeEditorAttribute : null;
					if (hideAttribute != null && hideAttribute.ControllingField == null)
						return false;
					return true;
				})
				.Where(f =>
				{
					// Check for any nodes that we want to explicitly expose
					if (!(f.FieldType.IsGenericType && (f.FieldType.GetGenericTypeDefinition() == typeof(IBinding<>) || f.FieldType.GetGenericTypeDefinition() == typeof(IEnableBinding<>))))
						return f.GetCustomAttributes(typeof(ExposeAttribute), true).Any();

					// Initialize any action bindings with Signals or Activations
					var typeArgs = TypeUtil.GetGenericSubclassArguments(f.FieldType, typeof(IBinding<>));
					if (typeArgs.Any())
					{
						if (typeArgs[0] == typeof(Action))
						{
							if (f.GetValue(Value) == null)
								f.SetValue(Value, new Signal());
							return true;
						}
						else if (typeArgs[0] == typeof(IActivationActions))
						{
							if (f.GetValue(Value) == null)
								f.SetValue(Value, new Activation());
							return true;
						}
					}
					return false;
				})
				.Select(f =>
				{
					var fieldValue = f.GetValue(Value);

					// Create a NodeExposure for each field we want to expose
					Type nodeType = typeof(NodeExposure<>).MakeGenericType(fieldValue.GetType());
					IGraphNode node = nodeType.GetConstructor(new Type[] { }).Invoke(new object[] { }) as IGraphNode;

					node.Title = TypeUtil.CleanName(f.Name);
					node.ValueObject = fieldValue;
					
					Func<bool> visible = () => true;

					var hideAttributes = f.GetCustomAttributes(typeof(HideInNodeEditorAttribute), true);
					var hideAttribute = hideAttributes.Any() ? hideAttributes[0] as HideInNodeEditorAttribute : null;
					if (hideAttribute != null && hideAttribute.ControllingField != null)
					{
						if (hideAttribute.ControllingField.StartsWith("!"))
						{
							var controllingField = Value.GetType().GetField(hideAttribute.ControllingField.Substring(1, hideAttribute.ControllingField.Length - 1));
							visible = () => (bool)controllingField.GetValue(Value);
						}
						else
						{
							var controllingField = Value.GetType().GetField(hideAttribute.ControllingField);
							visible = () =>  !(bool)controllingField.GetValue(Value);
						}
					}

					return new SlaveNode() { Node = node, Visible = visible };
				}).ToArray();

			float fHeight = 0.0f;
			foreach (var n in m_slaveNodes)
			{
				if (n.Visible())
				{
					fHeight += 31.0f;
					n.Node.Position = new Rect(Position.x, Position.y + fHeight, Position.width, Position.height);
				}
			}

			return m_slaveNodes.Where(n => n.Visible()).Select(n => n.Node);
		}
	}

	private IEnumerable<FieldInfo> GetBindingFields()
	{
		if (Value == null)
			return Enumerable.Empty<FieldInfo>();

		return Value
			.GetType()
			.GetFields()
			.Where(f => f.IsPublic && !f.IsStatic && f.FieldType.IsGenericType && (f.FieldType.GetGenericTypeDefinition() == typeof(IBinding<>) || f.FieldType.GetGenericTypeDefinition() == typeof(IEnableBinding<>)))
			.Where(f =>
			{
				var typeArgs = TypeUtil.GetGenericSubclassArguments(f.FieldType, typeof(IBinding<>));
				if (typeArgs.Any())
				{
					if (typeArgs[0] == typeof(Action))
						return false;
					else if (typeArgs[0] == typeof(IActivationActions))
						return false;
					return !f.GetCustomAttributes(typeof(ExposeAttribute), true).Any();
				}
				return false;
			})
			.Where(f => !f.GetCustomAttributes(typeof(HideInNodeEditorAttribute), true).Any());
	}

	// Get field bindings as graph edges - These are temporary and not saved.
	public override IEnumerable<IGraphEdge> GetEdges(INodeGraph nodeGraph)
	{
		var bindingFields = GetBindingFields();

		List<IGraphEdge> enumerableEdges = new List<IGraphEdge>();
		foreach(var field in bindingFields)
		{
			Type constructValueType = typeof(ConstructValueBindingEdgeField<>).MakeGenericType(TypeUtil.GetGenericSubclassArguments(field.FieldType, typeof(IBinding<>)));
			object constructValue = constructValueType.GetConstructor(new Type[] { typeof(FieldInfo) }).Invoke(new object[] { field });

			object toValue = field.GetValue(Value);
			if (nodeGraph.GetNode(toValue) != null)
				enumerableEdges.Add(constructValueType.GetMethod("Get").Invoke(constructValue, new object[] { Value, toValue }) as IGraphEdge);
		}
		return enumerableEdges;
	}
	public override IEnumerable<fiMemberTraversalImmediate<IGraphEdge>> GetEdgeItems(INodeGraph nodeGraph) { return GetEdges(nodeGraph).Select((e, i) => new fiMemberTraversalImmediate<IGraphEdge>(e, o => (o as BindableNode<ValueType>).GetEdges(nodeGraph).ElementAt(i))); }

	// Get constructors for field binding
	public override ConstructorSelection[] GetEdgeConstructors(Type viewType)
	{
		var bindingFields = GetBindingFields();
		return bindingFields
			.Select(f =>
			{
				Type constructValueType = typeof(ConstructValueBindingEdgeFieldTemp<>).MakeGenericType(TypeUtil.GetGenericSubclassArguments(f.FieldType, typeof(IBinding<>)));
				object constructValue = constructValueType.GetConstructor(new Type[] { typeof(object), typeof(FieldInfo) }).Invoke(new object[] { Value, f });
				var edgeConstruct = constructValueType.GetMethod("GetInvoke").Invoke(constructValue, new object[] { }) as Func<object[], object>;
				return new ConstructorSelection("Bind " + TypeUtil.CleanName(f.Name) + " (" + TypeUtil.TypeName(f.FieldType.GetGenericArguments()[0], true, false) + ")", edgeConstruct, new object[] { });
			})
			.ToArray();
	}
}

// A NodeBinding is a structure that the graph editor can use to represent references visually.  They have no settings and thus are not saved
public class NodeBinding<T> : GraphEdge<Ref<IBinding<T>>>, IBinding<T>
{
	public override IGraphNode GetFromNode(INodeGraph nodeGraph) { return nodeGraph.GetNode(FromObject); }
	public override IGraphNode GetToNode(INodeGraph nodeGraph) { return nodeGraph.GetNode(ToBinding); }

	public override object FromObject { get; set; }
	public IBinding<T> ToBinding { get { return Value.Get(); } set { Value.Set(value); } }

	public override object ToObject { get { return ToBinding; } set { ToBinding = (IBinding<T>)value; } }

	private FieldInfo m_fieldInfo;
	public NodeBinding(FieldInfo fieldInfo) { m_fieldInfo = fieldInfo; }

	public T Get() { return ToBinding.Get(); }

	// Display parameters
	public override Color Color { get { return (IsSignal || IsAction) ? Color.white : Color.cyan; } }
	public override bool Reverse { get { return !IsAction; } }
	private bool IsAction { get { return typeof(T) == typeof(IActivationActions) || typeof(T) == typeof(Action) || (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(Action<>) || typeof(T).GetGenericTypeDefinition() == typeof(Action<,>) || typeof(T).GetGenericTypeDefinition() == typeof(Action<,,>))); } }
	private bool IsSignal { get { return typeof(T) == typeof(IActivationSignals) || typeof(T) == typeof(Signal) || (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(Signal<>) || typeof(T).GetGenericTypeDefinition() == typeof(Signal<,>) || typeof(T).GetGenericTypeDefinition() == typeof(Signal<,,>))); } }
	public override string Title { get { return (m_fieldInfo.Name == "Trigger" || m_fieldInfo.Name == "Activation") ? "" : TypeUtil.CleanName(m_fieldInfo.Name); } }
	
	public override string ToString()
	{
		return Title;
	}

	public override bool CanConnect(Type type) 
	{
		var proxyTypes = TypeUtil.GetGenericSubclassArguments(type, typeof(IProxy<>));
		return proxyTypes.Any() && typeof(IBinding<T>).IsAssignableFrom(proxyTypes[0]) && !typeof(IEnableBindingExplicit<T>).IsAssignableFrom(proxyTypes[0]) && !typeof(IBindingExplicit<T>).IsAssignableFrom(proxyTypes[0]);
	}

	public override void OnConnect()
	{
		base.OnConnect();
		if (ToBinding == null)
			Debug.LogError("To binding is null!");
		else
			m_fieldInfo.SetValue(FromObject, ToBinding);
	}
}


// The nodes displayed under parent nodes that are created with the [Expose] attribute
public class NodeExposure<ValueType> : GraphNode<ValueType>, IBinding<ValueType> where ValueType : new()
{
	public override bool IsBold { get { return Value is ActivationBase; } }
	public override int DisplayColor { get { return 2; } }
	public override bool KeepMinWidth { get { return false; } }

	ValueType IBinding<ValueType>.Get() { return Value; }

	public string Name;
	public bool KeepMinSize { get { return false; } }
	public override string Title { get { return Name; } set { Name = value; } }
}
