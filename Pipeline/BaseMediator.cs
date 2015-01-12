using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Mediator that will track a BaseView to enable model (save game) serialization
public class BaseMediator : strange.extensions.mediation.impl.Mediator, IEnableContainer
{
	[Inject] public BaseContext context { get; set; }
	[Inject] public strange.extensions.mediation.api.IView IView { get; set; }

	protected IBaseView IBaseView { get { return IView as IBaseView; } }

	private List<IEnabled> m_enabledObjects = new List<IEnabled>();
	public virtual IEnumerable<IEnabled> EnabledObjects { get { return m_enabledObjects; } set { m_enabledObjects = value as List<IEnabled>; } }

	private bool m_hasStarted = false;
	protected virtual BaseViewModel SerializedModel { get { return null; } set { } }
	private bool HaveStartupModel { get { return IBaseView.DeserializedModel != null; } }

	// In Unity, OnEnable is called in-line (assuming the object is default enabled) (OnEnable is also called on subsequent enables/disables and we need those as well)
	// But we can't do anything before we get the context (which is only available on OnRegister), so if we're default enabled we have to defer enabling until OnRegister
	public override void OnRegister()
	{
		base.OnRegister();

		// See if we are being spun up from a save game.
		if (IBaseView.DeserializedModel != null)
		{
			SerializedModel = IBaseView.DeserializedModel as BaseViewModel;
		}

		if (HaveStartupModel)
			OnStartupModel(IBaseView.DeserializedModel as BaseViewModel);
		else
			OnNewModel();
		IBaseView.Mediated = true;

		using (var view = Injection.Create<IBaseView>(context, IBaseView))
		using (var typeBinding = Injection.Create(context, IView.GetType(), IView))
		using (var objectBinding = Injection.Create<GameObject>(context, gameObject))
		{
			EnableContainerImpl.Create(this);
		}

		m_hasStarted = true;
		if (gameObject.activeInHierarchy)
			OnEnable();
	}

	protected void Start()
	{
		if (IBaseView.parentContext != null)
		{
			IBaseView.parentContext.contextView.OnRegisterModel.Dispatch(IBaseView, SerializedModel);
		}
	}

	protected virtual void OnStartupModel(BaseViewModel model) { }
	protected virtual void OnNewModel() { }

	public void OnDestroy()
	{
		context.contextView.OnRemoveModel.Dispatch(IBaseView, SerializedModel);

		EnableContainerImpl.Destroy(this);
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

	protected virtual void Enable() { }
	protected virtual void Disable() { }
}

// Mediator that will track the transform of the view for serialization.  There should only be one of these on a GameObject!
public class TransformMediator : BaseMediator
{
	private TransformModel m_defaultModel = new TransformModel();
	protected sealed override BaseViewModel SerializedModel { get { return SerializedTransformModel; } }
	protected virtual TransformModel SerializedTransformModel { get { return m_defaultModel; } }

	protected override void OnStartupModel(BaseViewModel model)
	{
		base.OnStartupModel(model);
		var transformModel = model as TransformModel;
		if (transformModel != null)
		{
			IBaseView.behavior.transform.position = transformModel.Position;
			IBaseView.behavior.transform.rotation = transformModel.Rotation;
		}
	}

	protected virtual void Update()
	{
		SerializedTransformModel.Position = IBaseView.behavior.transform.position;
		SerializedTransformModel.Rotation = IBaseView.behavior.transform.rotation;
	}
}
