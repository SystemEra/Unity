using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;


public interface INestedPrefab
{
	GameObject PrefabGameObject { get; }
	GameObject InstanceGameObject { set; }
}

public class NestedPrefab<T> : INestedPrefab where T : UnityEngine.Object
{
	public GameObject PrefabGameObject { get { return Prefab != null ? ((Prefab is GameObject) ? Prefab as GameObject : (Prefab as MonoBehaviour).gameObject) : null; } }
	public GameObject InstanceGameObject 
	{ 
		set 
		{
			if (Prefab is GameObject)
				Instance = value as T;
			else
				Instance = value.GetComponent(typeof(T)) as T;
		} 
	}
	public T Prefab;
	[NonSerialized] [HideInInspector] public T Instance;

	public NestedPrefab() { Prefab = null; Instance = null; }
	public NestedPrefab(T prefab) { Prefab = prefab; Instance = null; }
}

public interface IPrefabInstantiation
{
	Transform PrefabTransform { get; }
	INestedPrefab PrefabObject { get; }
}

// Information on where to instantiate a nested (or procedural) prefab.
public class PrefabInstantiation : IPrefabInstantiation
{
	public Transform PrefabTransform { get { return Transform; } }
	public INestedPrefab PrefabObject { get { return NestedPrefab; } }

	public Transform Transform;
	public INestedPrefab NestedPrefab;

	public PrefabInstantiation(Transform transform, INestedPrefab nestedInstance) { Transform = transform; NestedPrefab = nestedInstance; }
}

[Serializable]
public class PrefabInstantiation<T> : IPrefabInstantiation where T : UnityEngine.Object
{
	public Transform PrefabTransform { get { return Transform; } }
	public INestedPrefab PrefabObject { get { return NestedPrefab; } }

	public Transform Transform;
	public NestedPrefab<T> NestedPrefab = new NestedPrefab<T>();

	public T Instance { get { return NestedPrefab.Instance; } }

	public PrefabInstantiation() { }
	public PrefabInstantiation(Transform transform, NestedPrefab<T> nestedInstance) { Transform = transform; NestedPrefab = nestedInstance; }
}

public interface INestedPrefabView : IBaseView
{
	IEnumerable<IPrefabInstantiation> PrefabInstantiations { get; }
	IEnumerable<INestedPrefab> NestedPrefabs { get; }
	Transform transform { get; }
	UnityEngine.Object PrefabObject { get; }
}

public static class NestedPrefabViewImpl
{
	public static void DeleteNestedPrefabs(INestedPrefabView nestedPrefab)
	{
		// Destroy previously created nested prefabs
		List<GameObject> childObjects = new List<GameObject>();
		foreach (Transform child in EditorUtil.GetChildTransforms(nestedPrefab.transform).ToList()) { if (child.tag == "Nested Prefab") childObjects.Add(child.gameObject); }

		if (!Application.isPlaying)
		{
			// ScrollRect destroyed exception issue
			// childObjects.ForEach(o => { GameObject.DestroyImmediate(o); });
			childObjects.ForEach(o =>
			{
				o.transform.parent = EditorUtil.SceneTrash.transform;
			});

			EditorUtil.GetChildTransforms(EditorUtil.SceneTrash.transform).ToList().ForEach(t => { t.gameObject.SetActive(false); t.gameObject.hideFlags = HideFlags.DontSave; });
		}
		else
			childObjects.ForEach(o => { GameObject.Destroy(o); });
	}

	public static void ApplyPrefab(INestedPrefabView nestedPrefab)
	{
#if UNITY_EDITOR
		DeleteNestedPrefabs(nestedPrefab);
		GameObject prefabGameObject = (nestedPrefab.PrefabObject as MonoBehaviour).gameObject;
		var prefabType = UnityEditor.PrefabUtility.GetPrefabType(prefabGameObject);
		if (!Application.isPlaying && (prefabType == UnityEditor.PrefabType.DisconnectedPrefabInstance || prefabType == UnityEditor.PrefabType.PrefabInstance))
		{
			var prefabParent = UnityEditor.PrefabUtility.GetPrefabParent(prefabGameObject);
			if (prefabParent != null)
			{
				UnityEditor.PrefabUtility.ReplacePrefab(prefabGameObject, prefabParent);
			}
		}
		InstantiatePrefabs(nestedPrefab);
#endif
	}

	// Instantiate prefabs onto this game object.  We want them always to appear as if they were nested prefabs.
	public static void InstantiatePrefabs(INestedPrefabView nestedPrefab)
	{
		DeleteNestedPrefabs(nestedPrefab);

		// Always instantiate at runtime, otherwise make sure we only instantiate on prefabs that are instanced in the hierarchy
#if UNITY_EDITOR
		var prefabType = UnityEditor.PrefabUtility.GetPrefabType(nestedPrefab.PrefabObject);
		if (Application.isPlaying || prefabType == UnityEditor.PrefabType.DisconnectedPrefabInstance || prefabType == UnityEditor.PrefabType.PrefabInstance || prefabType == UnityEditor.PrefabType.None)
#endif
		{
			// Rebuild them based on virtual PrefabInstantiations
			int i = 0;
			foreach (IPrefabInstantiation prefabInstantiation in nestedPrefab.PrefabInstantiations)
			{
				if (prefabInstantiation.PrefabObject.PrefabGameObject != null)
				{
					var prefabInstance = BaseContextView.Instantiate(prefabInstantiation.PrefabTransform, prefabInstantiation.PrefabObject.PrefabGameObject);
					if (nestedPrefab.context != null)
						nestedPrefab.context.contextView.RegisterPrefab(prefabInstance, new PrefabInstantiationReference() { Index = i });
					BaseContextView.RegisterContext(prefabInstance, nestedPrefab.context);
					if (prefabInstance != null)
					{
						prefabInstance.tag = "Nested Prefab";
						prefabInstantiation.PrefabObject.InstanceGameObject = prefabInstance;
					}
					++i;
				}
			}
		}
	}

	public static NestedPrefab<T>[] NestedPrefabRepeat<T>(T t, int count) where T : UnityEngine.Object
	{
		NestedPrefab<T>[] arrayOut = new NestedPrefab<T>[count];
		for (int i = 0; i < count; ++i)
			arrayOut[i] = new NestedPrefab<T>(t);
		return arrayOut;
	}

	public static IEnumerable<IPrefabInstantiation> PrefabInstantiationSingle(Transform t, INestedPrefab p)
	{
		return Enumerable.Repeat(new PrefabInstantiation(t, p), 1).Cast<IPrefabInstantiation>();
	}

	public static IEnumerable<IPrefabInstantiation> PrefabInstantiationSelect<T>(Transform t, IEnumerable<NestedPrefab<T>> n) where T : UnityEngine.Object
	{
		return n.Select(i => new PrefabInstantiation(t, i)).Cast<IPrefabInstantiation>();
	}
}

public abstract class NestedPrefabView : BaseView, INestedPrefabView
{
	public virtual IEnumerable<IPrefabInstantiation> PrefabInstantiations { get { return NestedPrefabs.Select(p => new PrefabInstantiation(transform, p)).Cast<IPrefabInstantiation>(); } }
	public virtual IEnumerable<INestedPrefab> NestedPrefabs { get { return Enumerable.Empty<INestedPrefab>(); } }

	public UnityEngine.Object PrefabObject { get { return this; } }

	protected override void Create()
	{
		base.Create();
		InstantiatePrefabs();
	}

	private bool m_prefabsInstantiated = false;
	protected virtual void Update()
	{
		//InstantiatePrefabs();
	}

	// Because of initialization order issues, parent views can explicitly start up their children views if they provide a context
	public void InstantiatePrefabs(BaseContext context)
	{
		this.context = context;
		InstantiatePrefabs();
	}

	private void InstantiatePrefabs()
	{
		if (!m_prefabsInstantiated && Application.isPlaying)
			DoInstantiatePrefabs();
	}

	private void DoInstantiatePrefabs()
	{
		NestedPrefabViewImpl.InstantiatePrefabs(this);
		m_prefabsInstantiated = true;
	}

	[FullInspector.InspectorButton]
	public void UpdateAutoLayout()
	{
		DoInstantiatePrefabs();
	}

	[FullInspector.InspectorButton]
	public void ClearAutoLayout()
	{
		NestedPrefabViewImpl.DeleteNestedPrefabs(this);
	}

	protected static NestedPrefab<T>[] NestedPrefabRepeat<T>(T t, int count) where T : UnityEngine.Object
	{
		return NestedPrefabViewImpl.NestedPrefabRepeat<T>(t, count);
	}

	protected static IEnumerable<IPrefabInstantiation> PrefabInstantiationSingle(Transform t, INestedPrefab p)
	{
		return NestedPrefabViewImpl.PrefabInstantiationSingle(t, p);
	}

	protected static IEnumerable<IPrefabInstantiation> PrefabInstantiationSelect<T>(Transform t, IEnumerable<NestedPrefab<T>> n) where T : UnityEngine.Object
	{
		return NestedPrefabViewImpl.PrefabInstantiationSelect<T>(t, n);
	}
}

public abstract class NestedPrefabContextView : BaseContextView, INestedPrefabView
{
	public virtual IEnumerable<IPrefabInstantiation> PrefabInstantiations { get { return NestedPrefabs.Select(p => new PrefabInstantiation(transform, p)).Cast<IPrefabInstantiation>(); } }
	public virtual IEnumerable<INestedPrefab> NestedPrefabs { get { return Enumerable.Empty<INestedPrefab>(); } }

	public UnityEngine.Object PrefabObject { get { return this; } }

	protected override void Create()
	{
		base.Create();
		if (!m_prefabsInstantiated && Application.isPlaying)
			InstantiatePrefabs();
	}

	private bool m_prefabsInstantiated = false;
	protected virtual void Update()
	{
		if (!m_prefabsInstantiated && Application.isPlaying)
			InstantiatePrefabs();
	}

	public void InstantiatePrefabs()
	{
		NestedPrefabViewImpl.InstantiatePrefabs(this);
		OnInstantiatePrefabs();
		m_prefabsInstantiated = true;
	}

	protected virtual void OnInstantiatePrefabs() { }

	[FullInspector.InspectorButton]
	public void UpdateAutoLayout()
	{
		InstantiatePrefabs();
	}

	[FullInspector.InspectorButton]
	public void ClearAutoLayout()
	{
		NestedPrefabViewImpl.DeleteNestedPrefabs(this);
	}

	protected static NestedPrefab<T>[] NestedPrefabRepeat<T>(T t, int count) where T : UnityEngine.Object
	{
		return NestedPrefabViewImpl.NestedPrefabRepeat<T>(t, count);
	}

	protected static IEnumerable<IPrefabInstantiation> PrefabInstantiationSingle(Transform t, INestedPrefab p)
	{
		return NestedPrefabViewImpl.PrefabInstantiationSingle(t, p);
	}

	protected static IEnumerable<IPrefabInstantiation> PrefabInstantiationSelect<T>(Transform t, IEnumerable<NestedPrefab<T>> n) where T : UnityEngine.Object
	{
		return NestedPrefabViewImpl.PrefabInstantiationSelect<T>(t, n);
	}
}
