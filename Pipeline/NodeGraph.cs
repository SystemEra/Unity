/*-------------------------------------------------------------------------+
NodeGraph.cs
2014
Jacob Liechty

Interfaces for a directed graph - just nodes and edges.
 
GraphNode classes may implement GetEdgeTypes and GetEdgeConstructors to 
specify the types of outgoing edges that can be created from that node, 
along with the constructor delegates that create them. 
 
GraphEdge classes may implement CanConnect to narrow the types of nodes 
they will be allowed to connect into.

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FullInspector;

// Nodes in the graph, requires many properties needed for display and editing
public interface IGraphNode : IDisplayColor, ITitled, IProxy
{
	object Parent { get; set; }
	bool IsBold { get; }
	bool KeepMinWidth { get; }
	Rect Position { get; set; }
	bool CanConnect { get; }

	IEnumerable<IGraphNode> SlaveNodes { get; }

	void RemoveEdge(IGraphEdge edge);
	void AddEdge(IGraphEdge edge);	
	void DoubleClick();

#if UNITY_EDITOR
	void DrawMenu(UnityEditor.GenericMenu genericMenu, Action onInvalidate);
#endif
	
	ConstructorSelection[] GetEdgeConstructors(Type viewType);
	IEnumerable<IGraphEdge> GetEdges(INodeGraph nodeGraph);
	IEnumerable<fiMemberTraversalImmediate<IGraphEdge>> GetEdgeItems(INodeGraph nodeGraph);
}

// Implementation of graph node as a wrapper on *any* object.  That object is allowed to implement a few interfaces to aid in the display, but 
// otherwise all graph code is handled by this class, not the object itself
public abstract class GraphNode<ValueType> : ITitled, IGraphNode, IProxy<ValueType> where ValueType : new()
{
#if !UNITY_EDITOR
	[NonSerialized]
#endif
	[HideInInspector] public Rect m_position;
	public Rect Position { get { return m_position; } set { m_position = value; } }

	public virtual string Title { get { return Value is ITitled ? (Value as ITitled).Title : TypeUtil.TypeName(Value.GetType()); } set { } }
	public ValueType Value = new ValueType();

	// IProxy
	public object ValueObject { get { return Value; } set { Value = (ValueType)value; } }
		
	// Editor only stuff.
	[HideInInspector] public virtual int DisplayColor { get { return Value is IDisplayColor ? (Value as IDisplayColor).DisplayColor : 0; } }
	[HideInInspector] public virtual bool KeepMinWidth { get { return true; } }
	[HideInInspector] public bool CanConnect { get { return true; } }
	[HideInInspector] public virtual bool IsBold { get { return (this is INodeGraph && (this as INodeGraph).Nodes.Count() > 0) || (Value is IBold && (Value as IBold).IsBold); } }
	[HideInInspector] public object Parent { get; set; }
		
	#if UNITY_EDITOR
	public virtual void DrawMenu(UnityEditor.GenericMenu genericMenu, Action onInvalidate) {}
	#endif

	public virtual ConstructorSelection[] GetEdgeConstructors(Type viewType) { return new ConstructorSelection[] { }; }

	public virtual IEnumerable<IGraphEdge> GetEdges(INodeGraph nodeGraph) { return Enumerable.Empty<IGraphEdge>(); }
	public virtual IEnumerable<fiMemberTraversalImmediate<IGraphEdge>> GetEdgeItems(INodeGraph nodeGraph) { return Enumerable.Empty<fiMemberTraversalImmediate<IGraphEdge>>(); }
	public virtual void RemoveEdge(IGraphEdge edge) { }
	public virtual void AddEdge(IGraphEdge edge) { }
	public virtual IEnumerable<IGraphNode> SlaveNodes { get { return Enumerable.Empty<IGraphNode>(); } }
	
	// Open MonoDevelop or Visual Studio to the line number of this object pointed to by this node
	public void DoubleClick()
	{
#if UNITY_EDITOR
		if (this is INodeGraph)
			return;

		if (ScriptInfo != null)
		{
			string scriptsApp = UnityEditor.EditorPrefs.GetString("kScriptsDefaultApp");
			System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

			if (scriptsApp.Contains("devenv") || scriptsApp.Contains("UnityVS"))
			{
				startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
				startInfo.FileName = "OpenDevenvFile.exe";
				// Whoever designed command line argument handling in .NET 3.5 should be shot.
				startInfo.Arguments = ScriptInfo.FilePath + " " + (ScriptInfo.LineNumber - 2).ToString() + " \"\\\"" + scriptsApp + "\\\"\"";
			}
			else
			{
				string unityPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\..\\";
				startInfo.FileName = unityPath + "MonoDevelop\\bin\\MonoDevelop.exe";
				startInfo.Arguments = "--nologo " + ScriptInfo.FilePath + ";" + (ScriptInfo.LineNumber - 2).ToString();
			}
			System.Diagnostics.Process.Start(startInfo);
		}
#endif
	}
	
	////////////////// This property allows your custom node to have double-click automatically open MonoDevelop at your class //////////////////
	protected virtual ScriptInfo ScriptInfo { get { return Value is IScriptInfo ? (Value as IScriptInfo).ScriptInfo : null; } } 
	////////////////// To implement ScriptInfo, uncomment the commented line below and place the top of your node class. //////////////////
	
	// public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } } // Enable double-click-to-editor.
}

public interface IGraphEdge : IBold, ITitled, IProxy
{
	IGraphNode GetToNode(INodeGraph nodeGraph);
	IGraphNode GetFromNode(INodeGraph nodeGraph);
	void OnConnect();
	void OnDisconnect();
	bool CanConnect(Type type);
	object FromObject { get; set; }
	object ToObject { get; set; }
	bool Reverse { get; }
	Color Color { get; }
}

public abstract class GraphEdge<EdgeType> : IGraphEdge, IProxy<EdgeType> where EdgeType : new()
{
	public EdgeType Value = new EdgeType();
	public object ValueObject { get { return Value; } set { Value = (EdgeType)value; } }

	public virtual string Title { get { return Value is ITitled ? (Value as ITitled).Title : ""; } set {} }

	public abstract IGraphNode GetToNode(INodeGraph nodeGraph);
	public abstract IGraphNode GetFromNode(INodeGraph nodeGraph);

	public virtual bool Reverse { get { return false; } }
	public virtual bool IsBold { get { return false; } }
	public virtual bool CanConnect(Type type) { return true; }

	public virtual Color Color { get { return Color.white; } }
	public virtual void Start(BaseContext context) { context.Inject(this); }
	public virtual void OnConnect() { }
	public virtual void OnDisconnect() { }

	public abstract object FromObject { get; set; }
	public abstract object ToObject { get; set; }
}

public interface IEdgeOwner
{
	IGraphEdge Owner { get; }
}

public interface INodeGraph : ITitled
{
	System.Type[] GetTypes(System.Type viewType);

	IEnumerable<IGraphNode> Nodes { get; }
	IEnumerable<fiMemberTraversalImmediate<IGraphNode>> NodeItems { get; }
	void RemoveNode(IGraphNode node);
	void AddNode(IGraphNode node);
	IGraphNode GetNode(object value);
}

