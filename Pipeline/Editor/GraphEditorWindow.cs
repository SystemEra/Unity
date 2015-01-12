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

/*-------------------------------------------------------------------------+
UI for editing our node graphs. Uses reflection to populate context menus
with binding options.
+-------------------------------------------------------------------------*/

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FullInspector;

public class GraphEditorWindow : EditorWindow 
{
	public static GraphEditorWindow Current = null;

	public const float kNodeWidth = 150f;
	public const float kNodeHeight = 30f;

	private bool isDraggingNode;
	private bool isDraggingBox;

	private bool m_didDrag = false;

	private Rect dragBox;
	protected List<fiNestedMemberTraversal<IGraphNode>> selectedNodes = new List<fiNestedMemberTraversal<IGraphNode>>();
	protected fiNestedMemberTraversal<IGraphEdge> selectedEdge;
	private Color kGridMinorColorDark = new Color(0f, 0f, 0f, 0.18f);
	private Color kGridMajorColorDark = new Color(0f, 0f, 0f, 0.28f);
	private bool gridDrag;
	protected Vector2 scroll;
	private Vector2 scrollView= new Vector2(10000,10000);
	
	public fiNestedMemberTraversal<INodeGraph> nodeGraph = null;
	public List<fiNestedMemberTraversal<INodeGraph>> nodeGraphChain = new List<fiNestedMemberTraversal<INodeGraph>>();
	public UnityEngine.Object graphOwner = null;
	public UnityEngine.Object graphSelectedOwner = null;
	public Type viewType = null;

	private IGraphNode connectionNode = null;
	private ConstructorSelection connectionConstructor;

	static GraphEditorWindow()
	{
		BaseBehaviorEditor.EditDelegate += () => { if (Current != null) Current.Repaint(); };
	}

	public static void Init(List<fiNestedMemberTraversal<INodeGraph>> nodeGraphChain, UnityEngine.Object behaviorOwner, Type viewType)
	{
		if (nodeGraphChain == null)
		{
			Current = null;
			return;
		}

		Current = EditorWindow.GetWindow<GraphEditorWindow>();
		if (Application.isPlaying) Current.autoRepaintOnSceneChange = true;

		Current.nodeGraph = nodeGraphChain.Last();
		Current.nodeGraphChain = nodeGraphChain;
		Current.title = "Behavior";
		Current.graphOwner = behaviorOwner;
		Current.graphSelectedOwner = UnityEditor.Selection.activeObject;
		Current.selectedEdge = null;
		Current.selectedNodes.Clear();
		Current.isDraggingNode = false;
		Current.isDraggingBox = false;
		Current.scroll = Vector2.zero;

		if (viewType == null)
		{
			if (behaviorOwner is BaseContextView || behaviorOwner is BaseView)
			{
				viewType = behaviorOwner.GetType();
			}
			else if (behaviorOwner is IScoped)
			{
				viewType = (behaviorOwner as IScoped).ScopedContextView;
			}
		}

		Current.viewType = viewType;
		if (nodeGraphChain.Count == 1)
		{
			Current.SetTarget(null);
		}
	}

	public void SelectParent()
	{
		if (nodeGraphChain.Count > 1)
		{
			nodeGraphChain.RemoveAt(nodeGraphChain.Count - 1);
			Init(nodeGraphChain, graphOwner, viewType);
		}
	}

	[MenuItem("Window/Node Editor")]
	public static void ShowWindow ()
	{
		Init (null, null, null);
	}

	private void Invalidate()
	{
		(graphOwner as FullInspector.ISerializedObject).SaveState();
	}

	private void SetTarget(fiNestedMemberTraversal item)
	{
		EditorUtil.SelectedItem = item != null ? item.Append(new fiMemberTraversal(o => (o as IProxy).ValueObject)) : null;
		EditorUtil.SelectedOwner = graphOwner;

		UnityEditor.Selection.activeObject = graphSelectedOwner;
		FullInspector.FullInspectorCommonSerializedObjectEditor.RepaintEditor();
	}

	private void OnGUI()
	{
		INodeGraph thisNodeGraph = null;
		
		try
		{
			thisNodeGraph = nodeGraph != null ? nodeGraph.Get(graphOwner) : null;
		}
		catch (System.Exception )
		{
			nodeGraph = null;
			Init(null, null, null);
			return;
		}
		
		GUIStyle verticalScrollbar =new GUIStyle( GUI.skin.verticalScrollbar);
		GUI.skin.verticalScrollbar = GUIStyle.none;
		GUIStyle horizontalScrollbar = GUI.skin.horizontalScrollbar;
		GUI.skin.horizontalScrollbar = GUIStyle.none;

		scroll = GUI.BeginScrollView (new Rect(0,0,position.width,position.height), scroll, new Rect (0, 0, scrollView.x, scrollView.y), false, false);

		DrawGrid(position, scroll);

		if (thisNodeGraph != null)
		{
			thisNodeGraph.RemoveNode(null);
			DrawOverlay(thisNodeGraph);
			OnGraphGUI(thisNodeGraph);
		}

		GUI.EndScrollView ();

		GUI.skin.verticalScrollbar = verticalScrollbar;
		GUI.skin.horizontalScrollbar = horizontalScrollbar;
		HandleEvents ();
	}

	private void DrawOverlayNode()
	{
		foreach (fiNestedMemberTraversal<INodeGraph> nodeGraphItem in nodeGraphChain)
		{
			INodeGraph nodeGraph = nodeGraphItem.Get(graphOwner);
			string labelText = "- " + (nodeGraph as ITitled).Title;
			
			GUILayout.BeginVertical ((GUIStyle)"PopupCurveSwatchBackground",GUILayout.Width(199));
			if (nodeGraph is MachineState && (nodeGraph as MachineState).Active)
			{
				labelText += " (Active)";
			}
			EditorGUILayout.LabelField(labelText);
			GUILayout.EndVertical ();
		}
	}
	
	private void DrawOverlay(INodeGraph thisNodeGraph)
	{
		if (nodeGraph != null)
		{
			GUILayout.BeginArea(new Rect (scroll.x, scroll.y, 200, 500));

			DrawOverlayNode();

			if (nodeGraphChain.Count > 1 && GUILayout.Button("Select Parent", "flow overlay header upper left", GUILayout.ExpandWidth(true)))
			{
				SelectParent();
			}

			GUILayout.EndArea();
		}
	}

	private IEnumerable<fiNestedMemberTraversalImmediate<IGraphNode>> GetIGraphNodes(INodeGraph thisNodeGraph)
	{
		return thisNodeGraph.NodeItems.Select(n => new fiNestedMemberTraversalImmediate<IGraphNode>(n));
	}

	private fiNestedMemberTraversal<IGraphNode> m_clickedNode = null;
	private fiNestedMemberTraversalImmediate<IGraphNode> MouseHoverNode(INodeGraph thisNodeGraph)
	{
		var nodesUnderMouse = GetIGraphNodes(thisNodeGraph).Where(n => n.Immediate.Position.Contains(Event.current.mousePosition));
		return nodesUnderMouse.Count() > 0 ? nodesUnderMouse.First() : null;
	}

	private fiNestedMemberTraversalImmediate<IGraphEdge> MouseHoverEdge(INodeGraph thisNodeGraph, float threshold)
	{
		float bestEdgeDist = threshold;
		float bestArrowDist = threshold;
		fiNestedMemberTraversalImmediate<IGraphEdge> bestEdge = null;

		foreach (fiMemberTraversalImmediate<IGraphNode> node in thisNodeGraph.NodeItems)
		{
			Dictionary<object, int> edgeCounts = new Dictionary<object, int>();
			foreach (fiMemberTraversalImmediate<IGraphEdge> edge in node.Immediate.GetEdgeItems(thisNodeGraph))
			{
				var fromNode = edge.Immediate.GetFromNode(thisNodeGraph);
				var toNode = edge.Immediate.GetToNode(thisNodeGraph);
				if (fromNode != null && toNode != null)
				{
					if (!edgeCounts.ContainsKey(toNode.ValueObject))
						edgeCounts[toNode.ValueObject] = 0;

					float edgeDist = MathUtil.LineSegmentPointDistance2(EdgeStart(thisNodeGraph, edge.Immediate), EdgeEnd(thisNodeGraph, edge.Immediate), Event.current.mousePosition);
					float arrowDist = ((Vector2)EdgeArrow(thisNodeGraph, node.Immediate, edge.Immediate, edgeCounts[toNode.ValueObject]) - Event.current.mousePosition).sqrMagnitude;
					if (edgeDist < bestEdgeDist || (edgeDist == bestEdgeDist && arrowDist < bestArrowDist))
					{
						bestArrowDist = arrowDist;
						bestEdge = new fiNestedMemberTraversalImmediate<IGraphNode>(node).Append(edge);
						bestEdgeDist = edgeDist;
					}
					edgeCounts[toNode.ValueObject]++;
				}
			}
		}

		return bestEdge;
	}

	struct InputState
	{
		public fiNestedMemberTraversalImmediate<IGraphNode> mouseNode;
		public fiNestedMemberTraversalImmediate<IGraphEdge> mouseEdge;
		public bool additiveSelection;
		public bool leftDoubleClick;
		public bool clickDown;
		public bool clickUp;
		public bool leftDown;
		public bool rightDown;
		public bool leftUp;
		public bool rightUp;
		public bool dragging;
	}
	
	private InputState GetInputState(INodeGraph thisNodeGraph)
	{
		InputState s = new InputState();
		s.mouseNode = MouseHoverNode(thisNodeGraph);
		s.mouseEdge = MouseHoverEdge(thisNodeGraph, 20.0f);

		s.additiveSelection = Event.current.control;

		s.leftDoubleClick = Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2;
		s.clickDown = Event.current.type == EventType.MouseDown && Event.current.clickCount == 1;
		s.clickUp = Event.current.type == EventType.MouseUp;
		s.leftDown = s.clickDown && Event.current.button == 0;
		s.rightDown = s.clickDown && Event.current.button == 1;
		s.leftUp = s.clickUp && Event.current.button == 0;
		s.rightUp = s.clickUp && Event.current.button == 1;
		s.dragging = Event.current.type == EventType.MouseDrag;
		return s;
	}

	private void UpdateSelections(INodeGraph thisNodeGraph, InputState inputState)
	{
		// On LMB down, start node/box dragging, but don't commit a selection until
		// dragging begins or mouse up.
		if (inputState.leftDown)
		{
			isDraggingNode = false;
			isDraggingBox = false;
			m_didDrag = false;

			if (inputState.mouseNode != null)
			{
				m_clickedNode = inputState.mouseNode;
				isDraggingNode = true;
			}
			else
			{
				isDraggingBox = true;
				dragBox = new Rect(Event.current.mousePosition.x + scroll.x, Event.current.mousePosition.y + scroll.y, 0, 0);

				SelectEdge(null);
				if (!inputState.additiveSelection)
				{
					ClearNodeSelection();
					SetTarget(null);
					FullInspector.FullInspectorCommonSerializedObjectEditor.RepaintEditor();
				}
			}
		}

		// On left up, commit selection
		if (inputState.leftUp)
		{
			if (inputState.mouseNode != null)
			{
				if (!m_didDrag)
				{
					SelectEdge(null);

					if (!inputState.additiveSelection)
					{
						ClearNodeSelection();
						NodeSelectionAdd(thisNodeGraph, inputState.mouseNode);
					}
					else
					{
						if (selectedNodes.Any(n => n.Get(thisNodeGraph) == inputState.mouseNode.Immediate))
						{
							DeselectNode(inputState.mouseNode);
						}
						else
						{
							NodeSelectionAdd(thisNodeGraph, inputState.mouseNode);
						}
					}
				}
			}
			else if (inputState.mouseEdge != null)
			{
				ClearNodeSelection();
				SelectEdge(inputState.mouseEdge);
			}

			m_clickedNode = null;

			if (connectionNode != null)
			{
				IEnumerable<fiNestedMemberTraversalImmediate<IGraphNode>> IGraphNodes = GetIGraphNodes(thisNodeGraph);
				foreach (fiNestedMemberTraversalImmediate<IGraphNode> node in IGraphNodes)
				{
					if (node.Immediate.Position.Contains(Event.current.mousePosition) && node.Immediate.CanConnect && node.Immediate != connectionNode)
					{
						IGraphEdge edge = (IGraphEdge)connectionConstructor.Constructor(new object[] { });
						if (edge.CanConnect(node.Immediate.GetType()))
						{
							edge.FromObject = connectionNode.ValueObject;
							edge.ToObject = node.Immediate.ValueObject;
							edge.OnConnect();
						}
						else
							connectionNode.RemoveEdge(edge);
					}
				}

				connectionNode = null;
				Repaint();
			}
		}

		// Any button up, commit drag box selection.
		if (inputState.clickUp)
		{
			if (isDraggingBox)
			{
				Rect dragRect = new Rect(dragBox.xMin - scroll.x, dragBox.y - scroll.y, dragBox.width, dragBox.height);
				IEnumerable<fiNestedMemberTraversalImmediate<IGraphNode>> IGraphNodes = GetIGraphNodes(thisNodeGraph);
				foreach (fiNestedMemberTraversalImmediate<IGraphNode> node in IGraphNodes.Where(n => dragRect.Intersect(n.Immediate.Position)))
				{
					if (inputState.additiveSelection &&
						selectedNodes.Any(n => n.Get(thisNodeGraph) == node.Immediate))
					{
						DeselectNode(node);
					}
					else
					{
						NodeSelectionAdd(thisNodeGraph, node);
					}
				}
				isDraggingBox = false;
			}
			if (isDraggingNode)
			{
				Invalidate();
			}
			isDraggingNode = false;
		}

		// On right button down, in preparation for context menus, do selection, unless
		// additive, in which case we want to maintain the group selection we have.
		if (inputState.rightDown && !inputState.additiveSelection)
		{
			if (inputState.mouseNode != null)
			{
				SelectEdge(null);
				if (!selectedNodes.Any(n => n.Get(thisNodeGraph) == inputState.mouseNode.Immediate))
				{
					ClearNodeSelection();
					NodeSelectionAdd(thisNodeGraph, inputState.mouseNode);
				}
			}
			else if (inputState.mouseEdge != null)
			{
				SelectEdge(inputState.mouseEdge);
			}
		}
	}

	private void UpdateContextMenus(INodeGraph thisNodeGraph, InputState inputState)
	{
		if (inputState.rightUp)
		{
			IGraphNode contextMenuNode = inputState.additiveSelection && selectedNodes.Count() > 0 ?
				selectedNodes.Last().Get(thisNodeGraph) : (inputState.mouseNode != null ? inputState.mouseNode.Get(thisNodeGraph) : null);

			// Node context menu
			if (contextMenuNode != null)
			{
				IEnumerable<ConstructorSelection> constructorSelections = contextMenuNode.GetEdgeConstructors(viewType);
				GenericMenu genericMenu = new GenericMenu();

				genericMenu.AddItem(new GUIContent("Copy"), 
					false, 
					new GenericMenu.MenuFunction2(this.OnCopyNode),
					thisNodeGraph);
				genericMenu.AddItem(new GUIContent("Cut"), 
					false, 
					new GenericMenu.MenuFunction2(this.OnCutNode),
					thisNodeGraph);
				genericMenu.AddItem(new GUIContent("Delete"), 
					false, 
					new GenericMenu.MenuFunction2(this.OnDeleteNode),
					thisNodeGraph);

				contextMenuNode.DrawMenu(genericMenu, Invalidate);

				foreach (ConstructorSelection selection in constructorSelections.OrderBy(c => c.Title))
				{
					genericMenu.AddItem(new GUIContent(selection.Title), 
						false, 
						new GenericMenu.MenuFunction2(this.OnCreateEdge), 
						new object[] { contextMenuNode, selection });
				}

				genericMenu.ShowAsContext();
				Repaint();
				Invalidate();
			}
			// Edge context menu
			else if (inputState.mouseEdge != null)
			{
				GenericMenu genericMenu = new GenericMenu();
				genericMenu.AddItem(new GUIContent("Delete"), 
					false, 
					new GenericMenu.MenuFunction2(this.OnDeleteEdge), 
					new object[] { inputState.mouseEdge.Immediate, inputState.mouseEdge.Immediate.GetFromNode(thisNodeGraph) });
				genericMenu.ShowAsContext();
			}
			// Generic context menu
			else
			{
				ClearNodeSelection();
				GenericMenu genericMenu = new GenericMenu();
				IEnumerable<Type> types = thisNodeGraph.GetTypes(viewType);

				genericMenu.AddItem(new GUIContent("Paste"), 
					false, 
					new GenericMenu.MenuFunction2(this.OnPasteNode), 
					new object[] { thisNodeGraph, types, Event.current.mousePosition });
				genericMenu.AddSeparator("");

				foreach (Type type in types.OrderBy(t => TypeUtil.TypeName(t, false)))
				{
					string typeName = TypeUtil.TypeName(type, false);
					if (typeName != "")
					{
						genericMenu.AddItem(new GUIContent(typeName), 
							false, 
							new GenericMenu.MenuFunction2(this.OnCreateNode), 
							new object[] { thisNodeGraph, type, Event.current.mousePosition	});
					}
				}
				genericMenu.ShowAsContext();
			}

		}
	}

	private void UpdateDrag(INodeGraph thisNodeGraph, InputState inputState)
	{
		if (inputState.dragging)
		{
			if (isDraggingNode)
			{
				if (!inputState.additiveSelection && !selectedNodes.Any(n => n.Get(thisNodeGraph) == m_clickedNode.Get(thisNodeGraph))) ClearNodeSelection();

				NodeSelectionAdd(thisNodeGraph, m_clickedNode);
				m_didDrag = true;

				foreach (fiNestedMemberTraversal<IGraphNode> selectedNode in selectedNodes)
				{
					Rect nodePos = selectedNode.Get(thisNodeGraph).Position;
					nodePos.x += Event.current.delta.x;
					nodePos.y += Event.current.delta.y;

					if (nodePos.y < 10)
					{
						nodePos.y = 10;
					}
					if (nodePos.x < 10)
					{
						nodePos.x = 10;
					}
					selectedNode.Get(thisNodeGraph).Position = nodePos;
				}
			}
			if (isDraggingBox)
			{
				dragBox.xMax += Event.current.delta.x;
				dragBox.yMax += Event.current.delta.y;
			}
		}
	}
	
	private void UpdateKeyBoardEvents(INodeGraph thisNodeGraph)
	{
		Event ev = Event.current;
		if (ev.type == EventType.KeyDown)
		{
			if (ev.keyCode == KeyCode.C)
			{
				OnCopyNode(thisNodeGraph);
			}
			else if (ev.keyCode == KeyCode.X)
			{
				OnCutNode(thisNodeGraph);
			}
			else if (ev.keyCode == KeyCode.V)
			{
				IEnumerable<Type> types = thisNodeGraph.GetTypes(viewType).Where(type => typeof(IGraphNode).IsAssignableFrom(type));
				OnPasteNode(new object[] { thisNodeGraph, types, Event.current.mousePosition });
				Repaint();
			}
			else if (ev.keyCode == KeyCode.Delete)
			{
				OnDeleteNode(thisNodeGraph);
				if (selectedEdge != null)
				{
					OnDeleteEdge(new object[] { selectedEdge.Get(thisNodeGraph), selectedEdge.Get(thisNodeGraph).GetFromNode(thisNodeGraph) });
				}
				Repaint();
			}
		}
	}

	protected virtual void OnGraphGUI(INodeGraph thisNodeGraph)
	{
		if (thisNodeGraph == null)
		{
			return;
		}

		InputState inputState = GetInputState(thisNodeGraph);
		UpdateSelections(thisNodeGraph, inputState);
		UpdateKeyBoardEvents(thisNodeGraph);
		UpdateContextMenus(thisNodeGraph, inputState);
		UpdateDrag(thisNodeGraph, inputState);

		if (inputState.leftDoubleClick && inputState.mouseNode != null)
		{
			if (inputState.mouseNode.Immediate is INodeGraph)
			{
				nodeGraphChain.Add(nodeGraph.Append(inputState.mouseNode.As<INodeGraph>()));
				Init(nodeGraphChain, graphOwner, viewType);
			}
			inputState.mouseNode.Immediate.DoubleClick();
		}

		DrawConnections(thisNodeGraph);
		DrawNodes(thisNodeGraph);
	}

	private int EdgeIndex(INodeGraph thisNodeGraph, IGraphNode node, IGraphEdge edge)
	{
		int index = 0;
		foreach(IGraphEdge nodeEdge in node.GetEdges(thisNodeGraph))
		{
			if (nodeEdge == edge) return index;
			if (nodeEdge.GetToNode(thisNodeGraph) == edge.GetToNode(thisNodeGraph)) ++index;
		}
		return index;
	}

	private void DrawConnections(INodeGraph thisNodeGraph)
	{
		if (connectionNode != null) 
		{
			DrawConnection (connectionNode.Position.center, Event.current.mousePosition,position, Color.green, false, false, "", 0);
		}

		foreach (IGraphNode node in thisNodeGraph.Nodes) 
		{
			Dictionary<object, int> edgeCounts = new Dictionary<object, int>();
			foreach (IGraphEdge edge in node.GetEdges(thisNodeGraph)) 
			{
				var toNode = edge.GetToNode(thisNodeGraph);
				if (!edgeCounts.ContainsKey(toNode.ValueObject))
					edgeCounts[toNode.ValueObject] = 0;

				DrawEdge(thisNodeGraph, edge, node, edgeCounts[toNode.ValueObject]);
				edgeCounts[toNode.ValueObject]++;
			}
		}
	}

	private void DrawEdge(INodeGraph thisNodeGraph, IGraphEdge target, IGraphNode node, int edgeIndex)
	{
		var fromNode = target.GetFromNode(thisNodeGraph);
		var toNode = target.GetToNode(thisNodeGraph);
		if (fromNode != null && toNode != null)
		{
			Vector2 fromPosition = fromNode.Position.center;
			Vector2 toPosition = toNode.Position.center;
			if (target.Reverse) TypeUtil.Swap(ref fromPosition, ref toPosition);

			var thisSelectedEdge = selectedEdge != null ? selectedEdge.Get(thisNodeGraph) : null;
			DrawConnection(fromPosition, toPosition, new Rect(scroll.x, scroll.y + 7f, position.width, position.height),
							(thisSelectedEdge != null && thisSelectedEdge.GetFromNode(thisNodeGraph) == fromNode && target.ValueObject.Equals(thisSelectedEdge.ValueObject)) ? Color.green : target.Color, target.IsBold, true, target.Title, edgeIndex);
		}
	}

	
	private void DrawNodes (INodeGraph thisNodeGraph)
	{
		var curSelected = selectedNodes.Select(s => s.Get(thisNodeGraph)).ToList();
		foreach (IGraphNode node in thisNodeGraph.Nodes) 
		{
			var position = node.Position;
			position.height = kNodeHeight;
			node.Position = position;
			if (!curSelected.Contains (node)) 
			{
				DrawNode(curSelected, node);
			}
		}

		curSelected.ForEach(n => DrawNode(curSelected, n));
	}

	protected virtual void HandleEvents(){
		Event e = Event.current;
		switch (e.type) {
		case EventType.mouseDown:
			gridDrag = e.button == 2;
			break;
		case EventType.mouseUp:
			isDraggingBox = false;
			gridDrag = false;
			break;
		case EventType.mouseDrag:
			if (gridDrag) {
				scroll -= e.delta;
				scroll.x=Mathf.Clamp(scroll.x,0,Mathf.Infinity);
				scroll.y=Mathf.Clamp(scroll.y,0,Mathf.Infinity);
				e.Use ();
			}
			break;
		}

		
		wantsMouseMove=true;
		if(e.isMouse){
			e.Use();
		}
	}

	protected virtual void DrawGrid(Rect rect,Vector2 scroll)
	{
		if (Event.current.type != EventType.Repaint){
			return;
		}
		this.DrawGridLines(rect,scroll,12f,kGridMinorColorDark);
		this.DrawGridLines(rect,scroll,120f, kGridMajorColorDark);

		if (isDraggingBox)
		{
			Handles.color = Color.white;
			Handles.DrawLine(new Vector2(dragBox.xMin, dragBox.yMin) - scroll, new Vector2(dragBox.xMin, dragBox.yMax) - scroll);
			Handles.DrawLine(new Vector2(dragBox.xMin, dragBox.yMin) - scroll, new Vector2(dragBox.xMax, dragBox.yMin) - scroll);
			Handles.DrawLine(new Vector2(dragBox.xMax, dragBox.yMax) - scroll, new Vector2(dragBox.xMin, dragBox.yMax) - scroll);
			Handles.DrawLine(new Vector2(dragBox.xMax, dragBox.yMax) - scroll, new Vector2(dragBox.xMax, dragBox.yMin) - scroll);
		}
	}

	protected void DrawNode (IEnumerable<IGraphNode> curSelected, IGraphNode node)
	{
		UnityEditor.Graphs.Styles.Color color = (UnityEditor.Graphs.Styles.Color)node.DisplayColor;

		GUIStyle style = UnityEditor.Graphs.Styles.GetNodeStyle("node", color, curSelected.Contains(node));
		if (node.IsBold) style.fontStyle = FontStyle.Bold;

		float nodeWidth = style.CalcSize(new GUIContent(node.Title)).x + 10;
		var temp = node.Position;
		float initWidth = temp.width;
		temp.width = Mathf.Max(node.KeepMinWidth ? kNodeWidth : 0, nodeWidth);
		temp.x += (initWidth - temp.width) / 2;
		node.Position = temp;

		GUI.Box (node.Position, node.Title, style);

		style.fontStyle = FontStyle.Normal;
	}
	
	private void OnCreateEdge (object userData)
	{
		object[] mData = (object[])userData;

		connectionNode = (IGraphNode)mData[0];
		connectionConstructor = (ConstructorSelection)mData[1];

		Invalidate();
	}

	private void SelectEdge(fiNestedMemberTraversal<IGraphEdge> edge)
	{
		selectedEdge = edge;
		if (edge != null)
		{
			SetTarget(nodeGraph.Append(edge));			
		}
	}

	private void NodeSelectionAdd (INodeGraph thisNodeGraph, fiNestedMemberTraversal<IGraphNode> node)
	{
		if (selectedNodes.Any(n => n.Get(thisNodeGraph) == node.Get(thisNodeGraph))) return;

		selectedNodes.Add(node);
		if (selectedNodes.Count == 1)
		{
			SetTarget(nodeGraph.Append(node));
		}
		else
		{
			SetTarget(null);
		}
	}

	private void ClearNodeSelection()
	{
		selectedNodes.Clear();		
	}

	private void DeselectNode(fiNestedMemberTraversal<IGraphNode> node)
	{
		selectedNodes.Remove(node);
	}

	private void OnDeleteEdge(object userData)
	{
		IGraphEdge edge = (IGraphEdge)((object[])userData)[0];
		IGraphNode node = (IGraphNode)((object[])userData)[1];

		SelectEdge(null);
		edge.OnDisconnect();
		node.RemoveEdge(edge);
		Repaint();
		Invalidate();
	}

	private void OnPasteNode(object userData)
	{
		INodeGraph thisNodeGraph = (INodeGraph)((object[])userData)[0];
		IEnumerable<Type> nodeTypes = (IEnumerable<Type>)((object[])userData)[1];
		Vector2 mousePosition = (Vector2)((object[])userData)[2];

		if (EditorUtil.Clipboard is List<IGraphNode>)
		{
			var clonedNodes = (EditorUtil.Clipboard as List<IGraphNode>).Clone();
			var nodes = clonedNodes.Where(n => nodeTypes.Contains(n.GetType()));

			Vector2 minPosition = new Vector2(float.MaxValue, float.MaxValue);
			foreach (IGraphNode node in nodes)
			{
				minPosition.x = Mathf.Min(minPosition.x, node.Position.x);
				minPosition.y = Mathf.Min(minPosition.y, node.Position.y);
			}
			
			foreach (IGraphNode node in nodes)
			{
				CreateNode(thisNodeGraph, node, mousePosition - minPosition, true);
			}

			selectedNodes = nodes.Select(n => new fiNestedMemberTraversal<IGraphNode>(new fiMemberTraversal<IGraphNode>(o => (o as INodeGraph).Nodes.ElementAt(thisNodeGraph.Nodes.ToList().IndexOf(n))))).ToList();
		}
	}

	private void OnCopyNode(object userData)
	{
		INodeGraph thisNodeGraph = (INodeGraph)userData;
		EditorUtil.Clipboard = selectedNodes.Select(n => n.Get(thisNodeGraph)).ToList().Clone();
	}
	

	private void OnCutNode(object userData)
	{
		OnCopyNode (userData);
		OnDeleteNode (userData);
	}

	private void DoDeleteNode(INodeGraph thisNodeGraph, IGraphNode deleteNode, IGraphNode node)
	{
		if (node.GetEdges(thisNodeGraph) != null) 
		{
			List<IGraphEdge> removeTransitions = new List<IGraphEdge> ();
			foreach (IGraphEdge transition in node.GetEdges(thisNodeGraph)) 
			{
				if (transition.GetToNode(thisNodeGraph) == deleteNode) 
				{
					removeTransitions.Add (transition);
				}
			}
			
			foreach (IGraphEdge edge in removeTransitions) 
			{
				edge.OnDisconnect();
				node.RemoveEdge(edge);
			}
		}
	}

	private void OnDeleteNode (object userData)
	{
		INodeGraph thisNodeGraph = (INodeGraph)userData;
		foreach (IGraphNode deleteNode in selectedNodes.Select(n => n.Get(thisNodeGraph)).ToList())
		{
			foreach (IGraphNode node in thisNodeGraph.Nodes) 
			{
				DoDeleteNode(thisNodeGraph, deleteNode, node);
			}

			thisNodeGraph.RemoveNode(deleteNode);
		}
		selectedNodes = new List<fiNestedMemberTraversal<IGraphNode>>();
		SetTarget(null);
		Invalidate();
	}

	private void OnCreateNode (object data)
	{
		object[] mData = (object[])data;
		INodeGraph thisNodeGraph = (INodeGraph)mData[0];
		Type type = (Type)mData [1];
		Vector2 position = (Vector2)mData [2];
		IGraphNode node = type.GetConstructor(new Type[] { }).Invoke(new object[] { }) as IGraphNode;

		CreateNode(thisNodeGraph, node, position, false);
	}

	private void CreateNode (INodeGraph thisNodeGraph, IGraphNode node, Vector2 position, bool offsetPosition)
	{
		node.Parent = nodeGraph;

		Vector2 nodePosition = new Vector2(node.Position.xMin, node.Position.yMin);
		node.Position = new Rect(position.x, position.y, kNodeWidth, kNodeHeight);
		if (offsetPosition)
		{
			var temp = node.Position;
			temp.x += nodePosition.x;
			temp.y += nodePosition.y;
			node.Position = temp;
		}

		thisNodeGraph.AddNode(node);
		
		selectedNodes = new List<fiNestedMemberTraversal<IGraphNode>>() { new fiNestedMemberTraversal<IGraphNode>(new fiMemberTraversal<IGraphNode>(o => (o as INodeGraph).Nodes.ElementAt(thisNodeGraph.Nodes.ToList().IndexOf(node)))) };
		SetTarget(nodeGraph.Append(selectedNodes[0]));
		Invalidate();
	}
	
	private void DrawGridLines(Rect rect,Vector2 scroll, float gridSize, Color gridColor)
	{
		Handles.color = gridColor;
		for (float i =0; i < rect.width +scroll.x; i = i + gridSize){
			Handles.DrawLine(new Vector2(i, 0), new Vector2(i, position.height+scroll.y));
		}
		for (float j = 0; j < position.height+scroll.y; j = j + gridSize){
			Handles.DrawLine(new Vector2(0, j), new Vector2(rect.width+scroll.x, j));
		}
	}

	private Vector3 EdgeStart(INodeGraph thisNodeGraph, IGraphEdge edge)
	{
		Vector3 start = edge.GetFromNode(thisNodeGraph).Position.center;
		return start;
	}

	private Vector3 EdgeArrow(INodeGraph thisNodeGraph, IGraphNode node, IGraphEdge edge, int edgeIndex)
	{
		Vector3 start = edge.GetFromNode(thisNodeGraph).Position.center;
		Vector3 end = edge.GetToNode(thisNodeGraph).Position.center;
		Vector3 cross = Vector3.Cross ((start - end).normalized, Vector3.forward);

		Vector3 vector3 = end - start;
		Vector3 vector31 = vector3.normalized;
		Vector3 vector32 = (vector3 * 0.5f) + start;
		vector32 = vector32 - (cross * 0.5f);
		Vector3 vector33 = vector32 + vector31;
		vector33 += 20.0f * vector31 * (edgeIndex + 0.5f);
		return vector33;
	}

	private Vector3 EdgeEnd(INodeGraph thisNodeGraph, IGraphEdge edge)
	{
		return edge.GetToNode(thisNodeGraph).Position.center;
	}
	
	public void DrawConnection (Vector3 start, Vector3 end, Rect rect, Color color, bool bold, bool drawArrow, string label, int index)
	{
		if (Event.current.type != EventType.repaint) {
			return;
		}

		Vector3 cross = Vector3.Cross ((start - end).normalized, Vector3.forward);
		
		Handles.color = color;
		Handles.DrawAAPolyLine (null, 5f, new Vector3[] { start, end });
		
		Vector3 vector3 = end - start;
		Vector3 vector31 = vector3.normalized;
		Vector3 vector32 = (vector3 * 0.5f) + start;
		vector32 = vector32 - (cross * 0.5f);
		Vector3 vector33 = vector32 + vector31;
		vector33 += 20.0f * vector31 * (index + 0.5f);
		if(rect.Contains(vector33))
		{
			if (drawArrow)
				DrawArrow (color, cross, vector31, vector33);

			if (label != "")
			{
				GUIStyle style = GUI.skin.label;
				if (bold) style.fontStyle = FontStyle.Bold;

				Vector2 labelPos = (Vector2)vector33 + EditorUtil.GetLabelOffset(cross, style.CalcSize(new GUIContent(label)).x);

				GUI.Label(new Rect(labelPos.x, labelPos.y - 6.0f, 200.0f, 20.0f), label);
				
				style.fontStyle = FontStyle.Normal;

			}
		}
		
	}
	
    private void DrawArrow (Color color, Vector3 cross, Vector3 direction, Vector3 center)
	{
		float size = 6.0f;
		Vector3[] vector3Array = new Vector3[] {
			center + (direction * size),
			(center - (direction * size)) + (cross * size),
			(center - (direction * size)) - (cross * size),
			center + (direction * size)
		};
		
		Color color1 = color;
		CreateMaterial ();
		material.SetPass (0);
		
		GL.Begin (4);
		GL.Color (color1);
		GL.Vertex (vector3Array [0]);
		GL.Vertex (vector3Array [1]);
		GL.Vertex (vector3Array [2]);
		GL.End ();
	}
	
	private Material material;
	private void CreateMaterial ()
	{
		if (material != null)
			return;
		
		material = new Material ("Shader \"Lines/Colored Blended\" {" +
		                         "SubShader { Pass { " +
		                         "    Blend SrcAlpha OneMinusSrcAlpha " +
		                         "    ZWrite Off Cull Off Fog { Mode Off } " +
		                         "    BindChannels {" +
		                         "      Bind \"vertex\", vertex Bind \"color\", color }" +
		                         "} } }");
		material.hideFlags = HideFlags.HideAndDontSave;
		material.shader.hideFlags = HideFlags.HideAndDontSave;
	}
}
