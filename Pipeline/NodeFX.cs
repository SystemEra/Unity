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
