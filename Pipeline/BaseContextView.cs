using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Context class, holds everything necessary for a scene.
public abstract class BaseContextView : strange.extensions.nestedcontext.impl.MVCSNestedContextView, IBaseView, IEnableContainer
{
	[FullInspector.NotSerialized]
	public new BaseContext context { get { return base.context as BaseContext; } set { base.context = value; } }
	public new BaseContext parentContext { get { return context.parentContext as BaseContext; } }

	public MonoBehaviour behavior { get { return this; } }
	public new GameObject gameObject { get { return base.gameObject as GameObject; } }

	// For startup from save
	public IModel DeserializedModel { get; set; }

	private IViewReference m_viewReference;
	public IViewReference ViewReference
	{
		get
		{
			if (m_viewReference == null)
			{
				BaseContextView parentContextView = null;

				if (context == null)
				{
					var newParentContext = NestedCapableView.GetParentContext(this, this) as BaseContext;
					if (newParentContext != null)
						parentContextView = newParentContext.contextView;
				}
				else if (parentContext != null)
					parentContextView = parentContext.contextView;

				if (parentContextView == null)
					m_viewReference = new MainViewReference();
				else
					m_viewReference = GetViewReference(parentContextView.gameObject);
			}

			return m_viewReference;
		}
	}

	public BaseContextViewModel ContextViewModel { get { return DeserializedModel as BaseContextViewModel; } }
	
	[System.NonSerialized] public List<GameObject> PreregisteredPrefabs = new List<GameObject>();
	public bool Mediated { get; set; }

	// IEnableContainer
	private List<IEnabled> m_enabledObjects = new List<IEnabled>();
	public virtual IEnumerable<IEnabled> EnabledObjects { get { return m_enabledObjects; } set { m_enabledObjects = value as List<IEnabled>; } }

	// In Unity, Start is called after OnEnable (assuming the object is default enabled) (OnEnable is also called on subsequent enables/disables and we need those as well)
	// But we can't do anything before we get the context (which is only available on Start), so if we're default enabled we have to defer enabling until Start
	private bool m_hasStarted = false;

	[System.NonSerialized] public Signal<GameObject> OnInstantiatePrefab = new Signal<GameObject>();
	[System.NonSerialized] public Signal<GameObject> OnDestroyPrefab = new Signal<GameObject>();
	[System.NonSerialized] public Signal<IBaseView, IModel> OnRegisterModel = new Signal<IBaseView, IModel>();
	[System.NonSerialized] public Signal<IBaseView, IModel> OnRemoveModel = new Signal<IBaseView, IModel>();
	
	[System.NonSerialized] public Dictionary<IBaseView, IViewReference> ChildrenViews = new Dictionary<IBaseView, IViewReference>();

	protected override void Start()
	{
		base.Start();		
		
		using (var iViewBinding = Injection.Create<IBaseView>(context, this))
		using (var typeBinding = Injection.Create(context, GetType(), this))
		using (var objectBinding = Injection.Create<GameObject>(context, gameObject))
		{
			EnableContainerImpl.Create(this);
		}

		Create();

		m_hasStarted = true;
		if (gameObject.activeInHierarchy)
			OnEnable();
	}

	protected void OnDestroy()
	{
		if (m_hasStarted)
		{
			EnableContainerImpl.Destroy(this);
			Destroy();
		}
	}

	// Seal OnEnable and OnDisable, which Unity calls at unfortunate times.  We'll handle it.
	protected void OnEnable()
	{
		if (m_hasStarted)
		{
			EnableContainerImpl.Enable(this);
			Enable();
		}
	}

	protected void OnDisable()
	{
		if (m_hasStarted)
		{
			Disable();
			EnableContainerImpl.Disable(this);
		}
	}

	protected virtual void Create() { }
	protected virtual void Enable() { }
	protected virtual void Disable() { }
	protected virtual void Destroy() { }

	public override strange.extensions.context.impl.Context CreateContext() { return GetContext(); }
	public virtual BaseContext GetContext()
	{
		return new BaseContext(this);
	}

	[Inject] public Canvas Canvas { get; set; }

	public BaseContext BaseContext { get { return context as BaseContext; } }
	
	// If we are forced to instantiate a child object under a different transform, we can manually set the context
	// (Normally contexts are registered using the hierarchy)
	public static void RegisterContext(GameObject go, BaseContext context)
	{
		foreach (var componentView in GetNestedViews(go).ToList())
		{
			componentView.overrideContext = context;
		}
	}

	public static IEnumerable<strange.extensions.nestedcontext.api.INestedCapableView> GetNestedViews(GameObject go)
	{
		var componentViews = go.GetComponentsInChildren(typeof(strange.extensions.nestedcontext.api.INestedCapableView), true).Cast<strange.extensions.nestedcontext.api.INestedCapableView>();
		var contextViews = go.GetComponentsInChildren(typeof(BaseContextView), true);
		List<strange.extensions.nestedcontext.api.INestedCapableView> nestedViews = new List<strange.extensions.nestedcontext.api.INestedCapableView>();

		foreach (var contextView in contextViews)
		{
			nestedViews.AddRange(contextView.GetComponentsInChildren(typeof(strange.extensions.nestedcontext.api.INestedCapableView), true).Cast<strange.extensions.nestedcontext.api.INestedCapableView>().Where(v => v != contextView));
		}

		return componentViews.Except(nestedViews);
	}

	public GameObject Instantiate(GameObject original, bool isPrefab = true)
	{
		return Instantiate(transform, original, isPrefab);
	}

	public GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, bool isPrefab = true)
	{
		var newObject = Instantiate(original, isPrefab);
		newObject.transform.position = position;
		newObject.transform.rotation = rotation;
		return newObject;
	}

	public GameObject InstantiateUI(GameObject original)
	{
		return InstantiateUI(original, context);
	}

	public GameObject InstantiateUI(GameObject original, BaseContext context)
	{
		GameObject ui = Instantiate(Canvas.transform, original);
		if (ui == null) return null;

		RegisterContext(ui, context);
		return ui;
	}

	private static GameObject GetInstance(GameObject original)
	{
		if (original == null) return null;
		GameObject newObject = null;

#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			newObject = UnityEditor.PrefabUtility.InstantiatePrefab(original) as GameObject;
		}
		else
#endif
		{
			newObject = (GameObject)UnityEngine.Object.Instantiate(original);
			newObject.name = newObject.name.Substring(0, newObject.name.Length - "(Clone)".Length);
		}

		return newObject;
	}

	public static GameObject Instantiate(Transform parent, GameObject original, bool isPrefab = true)
	{
		GameObject newObject = GetInstance(original);
		if (newObject == null) return null;

		if (newObject.transform is RectTransform)
		{
			Vector2 offsetMin = (newObject.transform as RectTransform).offsetMin;
			Vector2 offsetMax = (newObject.transform as RectTransform).offsetMax;

			Vector3 pos = newObject.transform.localPosition;
			newObject.transform.SetParent(parent);
			newObject.transform.localPosition = pos;
			newObject.transform.localRotation = Quaternion.identity;

			(newObject.transform as RectTransform).offsetMin = offsetMin;
			(newObject.transform as RectTransform).offsetMax = offsetMax;
		}
		else
		{
			newObject.transform.SetParent(parent);
			if (isPrefab)
			{
				newObject.transform.localPosition = Vector3.zero;
				newObject.transform.localRotation = Quaternion.identity;
			}
			else
			{
				newObject.transform.position = original.transform.position;
				newObject.transform.rotation = original.transform.rotation;
				newObject.transform.localScale = original.transform.localScale;
			}
		}

		return newObject;
	}

	private void SetPrefabReference(GameObject prefab, PrefabReference prefabReference)
	{
		prefabReference.ContextView = this.ViewReference;
		List<string> hierarchy = new List<string>();

		var t = prefab.transform;
		while (t != this.transform)
		{
			if (t == null)
				return;

			hierarchy.Add(t.name);
			t = t.parent;
		}
		hierarchy.Reverse();
		prefabReference.Hierarchy = hierarchy;
	}

	public void RegisterPrefab(GameObject prefab, PrefabReference prefabReference)
	{
		SetPrefabReference(prefab, prefabReference);
		prefab.AddComponent<PrefabReferenceTag>().Tag = prefabReference;

		var nestedViews = prefab.GetComponentsInChildren(typeof(IBaseView), true).Cast<IBaseView>();
		foreach (var view in nestedViews)
		{
			RegisterView(view, prefab);
		}
	}

	public void RegisterView(IBaseView view, GameObject prefab)
	{
		var viewReference = view.GetViewReference(prefab);
		ChildrenViews[view] = viewReference;

		IModel childModel;
		if (ContextViewModel != null && ContextViewModel.TryGetChildModel(viewReference, out childModel))
		{
			viewReference.GetView(prefab).DeserializedModel = childModel;
		}
	}

	public void SavePrefab(GameObject prefab)
	{
		SavePrefab(prefab, new PrefabGuid());
	}
	
	public void SavePrefab(GameObject prefab, PrefabReference prefabReference)
	{
		SetPrefabReference(prefab, prefabReference);
		prefab.AddComponent<PrefabReferenceTag>().Tag = prefabReference;		

		var nestedViews = prefab.GetComponentsInChildren(typeof(IBaseView), true).Cast<IBaseView>();
		foreach (var view in nestedViews)
		{
			var viewReference = view.GetViewReference(prefab);
			ChildrenViews[view] = viewReference;
		}

		OnInstantiatePrefab.Dispatch(prefab);
	}

	public void DestroyObject(GameObject obj)
	{
		var nestedViews = obj.GetComponentsInChildren(typeof(IBaseView), true).Cast<IBaseView>();
		foreach (var view in nestedViews)
			ChildrenViews.Remove(view);

		OnDestroyPrefab.Dispatch(obj);

		Object.Destroy(obj);
	}

	public IViewReference GetViewReference(GameObject prefab)
	{
		return BaseViewImpl.GetViewReference(this, prefab);
	}
}

// Serializable and unique reference to a prefab's view
public interface IViewReference 
{
	IBaseView GetView(GameObject prefab);
}

// Serializable and unique reference to a prefab under the global hierarchy
public interface IPrefabReference
{
	GameObject GetPrefab(MainContextView mainContextView);
}

// A prefab reference using a context view reference and a hierarchy
public abstract class PrefabReference : IPrefabReference
{
	public IViewReference ContextView;
	public List<string> Hierarchy = new List<string>();

	public override bool Equals(object obj)
	{
		PrefabReference rhs = obj as PrefabReference;
		return rhs != null && ((rhs.ContextView == null && ContextView == null) || (rhs.ContextView != null && rhs.ContextView.Equals(ContextView))) && rhs.Hierarchy.SequenceEqual(Hierarchy);
	}

	public override int GetHashCode()
	{
		return (ContextView != null ? ContextView.GetHashCode() : 0) ^ Hierarchy.Aggregate(0, (a, b) => a ^ b.GetHashCode());
	}

	public virtual GameObject GetPrefab(MainContextView mainContextView)
	{
		var contextView = ContextView.GetView(mainContextView.gameObject) as BaseContextView;
		Transform t = contextView.transform;
		foreach (string child in Hierarchy)
		{
			t = t.Find(child);
			if (t == null)
				return null;
		}
		return t.gameObject;
	}
}

// Just uniquely point to the global main context
public class MainViewReference : IViewReference
{
	public IBaseView GetView(GameObject prefab)
	{
		return prefab.GetComponent<MainContextView>();
	}

	public override bool Equals(object obj)
	{
		return obj is MainViewReference;
	}

	public override int GetHashCode()
	{
		return 0;
	}
}

// Default view reference specifies its parent prefab's uniqueness
public abstract class ViewReference : IViewReference
{
	public PrefabReference Prefab;
	public override bool Equals(object obj)
	{
		ViewReference rhs = obj as ViewReference;
		return rhs != null && rhs.Prefab.Equals(Prefab);
	}

	public override int GetHashCode()
	{
		return Prefab.GetHashCode();
	}

	public abstract IBaseView GetView(GameObject prefab);
}

// View reference using a type and hierarchy from its parent prefab
public class HierarchyViewReference : ViewReference
{
	public List<string> Hierarchy;
	public string Type;

	public override bool Equals(object obj)
	{
		HierarchyViewReference rhs = obj as HierarchyViewReference;
		return rhs != null && rhs.Hierarchy.SequenceEqual(Hierarchy) && Type == rhs.Type && base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return Type.GetHashCode() ^ Hierarchy.Aggregate(0, (a, b) => a ^ b.GetHashCode()) ^ base.GetHashCode();
	}

	public override IBaseView GetView(GameObject prefab)
	{
		Transform t = prefab.transform;
		foreach (string child in Hierarchy)
		{
			t = t.Find(child);
		}
		return t.gameObject.GetComponent(Type) as IBaseView;
	}
}

public class BaseContextViewModel : TransformModel
{
	// Somewhat annoyingly, dictionaries don't serialize properly when there are polymorphic keys, so we have to maintain lists
	public List<KeyValuePair<PrefabReference, Prefab>> PrefabList = new List<KeyValuePair<PrefabReference, Prefab>>();
	private Dictionary<PrefabReference, Prefab> m_prefabDictionary = null;

	private Dictionary<PrefabReference, Prefab> PrefabDictionary
	{
		get
		{
			if (m_prefabDictionary == null)
				m_prefabDictionary = PrefabList.ToDictionary(k => k.Key, k => k.Value);
			return m_prefabDictionary;
		}
	}

	public void AddPrefab(PrefabReference prefabReference, GameObject prefab)
	{
		if (!PrefabDictionary.ContainsKey(prefabReference))
		{
			PrefabList.Add(new KeyValuePair<PrefabReference, Prefab>(prefabReference, new Prefab(prefab)));
			PrefabDictionary[prefabReference] = new Prefab(prefab);
		}
	}

	public void RemovePrefab(PrefabReference prefabReference)
	{
		if (PrefabDictionary.ContainsKey(prefabReference))
		{
			PrefabList.RemoveAll(v => v.Key == prefabReference);
			PrefabDictionary.Remove(prefabReference);
		}
		else if (!PrefabRemoveSet.Contains(prefabReference))
		{
			PrefabRemoveList.Add(prefabReference);
			PrefabRemoveSet.Add(prefabReference);
		}
	}

	public bool TryGetChildPrefab(PrefabReference prefabReference, out Prefab prefab)
	{
		return PrefabDictionary.TryGetValue(prefabReference, out prefab);
	}

	public List<PrefabReference> PrefabRemoveList = new List<PrefabReference>();
	private HashSet<PrefabReference> m_prefabRemoveSet = null;

	private HashSet<PrefabReference> PrefabRemoveSet
	{
		get
		{
			if (m_prefabRemoveSet == null)
			{
				m_prefabRemoveSet = new HashSet<PrefabReference>();
				foreach (var p in PrefabRemoveList) m_prefabRemoveSet.Add(p);
			}
			return m_prefabRemoveSet;
		}
	}

	public bool IsPrefabRemoved(PrefabReference prefabReference)
	{
		return PrefabRemoveSet.Contains(prefabReference);
	}

	public List<KeyValuePair<IViewReference, IModel>> ViewModelList = new List<KeyValuePair<IViewReference, IModel>>();
	private Dictionary<IViewReference, IModel> m_viewModelDictionary = null;

	private Dictionary<IViewReference, IModel> ViewModelDictionary
	{
		get
		{
			if (m_viewModelDictionary == null)
				m_viewModelDictionary = ViewModelList.ToDictionary(k => k.Key, k => k.Value);
			return m_viewModelDictionary;
		}
	}

	public void AddModel(IViewReference viewReference, IModel model)
	{
		if (!ViewModelDictionary.ContainsKey(viewReference))
		{
			ViewModelList.Add(new KeyValuePair<IViewReference, IModel>(viewReference, model));
			ViewModelDictionary[viewReference] = model;
		}
	}

	public void RemoveModel(IViewReference viewReference)
	{
		ViewModelList.RemoveAll(v => v.Key == viewReference);
		ViewModelDictionary.Remove(viewReference);
	}

	public bool TryGetChildModel(IViewReference viewReference, out IModel outModel) 
	{
		return ViewModelDictionary.TryGetValue(viewReference, out outModel); 
	}
}

public class BaseContextMediator : TransformMediator
{
	protected BaseContextView BaseContextView { get { return IBaseView as BaseContextView; } }
	private BaseContextViewModel m_defaultModel = new BaseContextViewModel();

	protected sealed override TransformModel SerializedTransformModel { get { return SerializedContextModel; } }
	protected virtual BaseContextViewModel SerializedContextModel { get { return m_defaultModel; } set { m_defaultModel = value; } }

	// Listen to the context view to catch events that require us to update our model
	public override void OnRegister()
	{
		base.OnRegister();

		BaseContextView.OnRegisterModel.AddListener(RegisterModel);
		BaseContextView.OnRemoveModel.AddListener(RemoveModel);
		BaseContextView.OnInstantiatePrefab.AddListener(InstantiatePrefab);
		BaseContextView.OnDestroyPrefab.AddListener(DestroyPrefab);

		foreach (var prefab in SerializedContextModel.PrefabList)
		{
			GameObject newObject = BaseContextView.Instantiate(prefab.Value.Value);
			BaseContextView.RegisterPrefab(newObject, prefab.Key);
		}
	}

	public override void OnRemove()
	{
		base.OnRemove();

		BaseContextView.OnRegisterModel.RemoveListener(RegisterModel);
		BaseContextView.OnRemoveModel.RemoveListener(RemoveModel);
		BaseContextView.OnInstantiatePrefab.RemoveListener(InstantiatePrefab);
		BaseContextView.OnDestroyPrefab.RemoveListener(DestroyPrefab);
	}

	// When prefabs are instantiated and destroyed, register this in the model
	private void InstantiatePrefab(GameObject prefab)
	{			
		SerializedContextModel.AddPrefab(prefab.GetComponent<PrefabReferenceTag>().Tag, prefab);
	}

	private void DestroyPrefab(GameObject prefab)
	{
		var prefabReferenceTag = prefab.GetComponent<PrefabReferenceTag>();
		if (prefabReferenceTag != null)
		{
			SerializedContextModel.RemovePrefab(prefabReferenceTag.Tag as PrefabReference);
		}
	}

	// Child mediators will notify their parent context views (us) of any child models
	private void RegisterModel(IBaseView view, IModel model)
	{
		IViewReference viewReference;
		if (BaseContextView.ChildrenViews.TryGetValue(view, out viewReference))
		{
			SerializedContextModel.AddModel(viewReference, model);
		}
	}
	
	private void RemoveModel(IBaseView view, IModel model)
	{
		IViewReference viewReference;
		if (BaseContextView.ChildrenViews.TryGetValue(view, out viewReference))
		{
			SerializedContextModel.RemoveModel(viewReference);			
		}
	}
}

public class MainContextView : BaseContextView
{
	public VirtualControlSettings VirtualControlSettings = null;
	public VirtualControls VirtualControls { get { return VirtualControlSettings != null ? VirtualControlSettings.Controls : new VirtualControls(); } }
    public override BaseContext GetContext() { return new MainContext<MainContextView>(this); }
	public IMainContext MainContext { get { return base.context as IMainContext; } }

	[Inject] public UpdateSignal UpdateSignal { get; set; }
	[Inject] public FixedUpdateSignal FixedUpdateSignal { get; set; }

	protected virtual void Update()
	{
		if (MainContext != null && MainContext.IsMainContext)
		{
			UpdateSignal.Dispatch();
		}
	}

	protected virtual void FixedUpdate()
	{
		if (MainContext != null && MainContext.IsMainContext)
		{
			FixedUpdateSignal.Dispatch();
		}
	}

	[FullInspector.InspectorButton]
	public void CreateVirtualControlsAsset()
	{
		var newAsset = EditorUtil.NewAsset(typeof(VirtualControlSettings), "Assets/");
		if (newAsset != null)
			VirtualControlSettings = newAsset as VirtualControlSettings;
	}
}

public class SaveSignal : Signal { }