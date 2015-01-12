using UnityEngine;
using System.Collections;
using System.Linq;

[Name("FX/Activate Particle System")]
public class ActivateParticleEmitter: ActivationCommand
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	private ParticleSystem system = null;
	public ParticleSystem System = null;

	public override void Enable()
	{
		base.Enable();
		system = System ?? gameObject.GetComponentInChildren<ParticleSystem>();
		system.enableEmission = false;
	}
	protected override void OnActivated()
	{
		system.enableEmission = true;
	}

	protected override void OnDeactivated()
	{
		system.enableEmission = false;
	}
}
