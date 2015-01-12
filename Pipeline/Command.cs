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

using System;
using UnityEngine;
using System.Collections;

namespace SE
{
	public abstract class CommandCommon : SelfActivated, IBinding<IActivationSignals>, IEnableBinding<Signal>, IDisplayColor, ITitled
	{
		// GraphNode attributes
		public override int DisplayColor { get { return 1; } }

		// Command nodes are always themselves active, and just listen for execute events
		public override bool IsActivation { get { return false; } }
		private Activation Activation = new Activation();

		// Auto fire the signal after this command fires.
		protected void PostDispatch()
		{
			Activation.Action();
		}

		public override void Enable()
		{
			base.Enable();
			SelfActivate();
		}

		public override void Disable()
		{
			base.Disable();
			SelfDeactivate();
		}

		Signal IBinding<Signal>.Get() { return Activation.ActivatedSignal; }
		IActivationSignals IBinding<IActivationSignals>.Get() { return Activation; }
	}

	public abstract class CommandBase : CommandCommon
	{
		public void Dispatch()
		{
			Execute();
			base.PostDispatch();
		}
		public virtual void Execute() { }
	}

	// Command node for zero parameters
	public abstract class Command : CommandBase, IEnableBinding<Action>
	{
		public IBinding<Signal> Trigger = new Signal();

		public override void Enable()
		{
			base.Enable();
			Trigger.Get().AddListener(Dispatch);
			Trigger.Get().AddListener(gameObject, Dispatch);
		}

		public override void Disable()
		{
			base.Disable();
			Trigger.Get().RemoveListener(Dispatch);
			Trigger.Get().RemoveListener(gameObject, Dispatch);
		}

		public Action Get() { return Dispatch; }
	}

	public abstract class CommandBase<A> : CommandCommon, IEnableBinding<Signal<A>>
	{
		private Signal<A> FireSignal = new Signal<A>();
		Signal<A> IBinding<Signal<A>>.Get() { return FireSignal; }

		public void Dispatch(A a)
		{
			Execute(a);
			base.PostDispatch();
			FireSignal.Dispatch(a);
		}
		public virtual void Execute(A a) { }
	}

	// Command node for one parameter
	public abstract class Command<A> : CommandBase<A>, IEnableBinding<Action<A>>
	{
		public IBinding<Signal<A>> Trigger = new Signal<A>();
		Action<A> IBinding<Action<A>>.Get() { return Dispatch; }

		public override void Enable()
		{
			base.Enable();
			Trigger.Get().AddListener(Dispatch);
			Trigger.Get().AddListener(gameObject, Dispatch);
		}

		public override void Disable()
		{
			base.Disable();
			Trigger.Get().RemoveListener(Dispatch);
			Trigger.Get().RemoveListener(gameObject, Dispatch);
		}
	}

	public abstract class CommandBase<A, B> : CommandCommon, IEnableBinding<Signal<A, B>>
	{
		private Signal<A, B> FireSignal = new Signal<A, B>();
		Signal<A, B> IBinding<Signal<A, B>>.Get() { return FireSignal; }

		public void Dispatch(A a, B b)
		{
			Execute(a, b);
			base.PostDispatch();
			FireSignal.Dispatch(a, b);
		}
		public virtual void Execute(A a, B b) { }
	}

	// Command node for two parameters
	public abstract class Command<A, B> : CommandBase<A, B>, IEnableBinding<Action<A, B>>
	{
		public IBinding<Signal<A, B>> Trigger = new Signal<A, B>();
		Action<A, B> IBinding<Action<A, B>>.Get() { return Dispatch; }

		public override void Enable()
		{
			base.Enable();
			Trigger.Get().AddListener(Dispatch);
			Trigger.Get().AddListener(gameObject, Dispatch);
		}

		public override void Disable()
		{
			base.Disable();
			Trigger.Get().RemoveListener(Dispatch);
			Trigger.Get().RemoveListener(gameObject, Dispatch);
		}
	}

	public abstract class CommandBase<A, B, C> : CommandCommon, IEnableBinding<Signal<A, B, C>>
	{
		private Signal<A, B, C> FireSignal = new Signal<A, B, C>();
		Signal<A, B, C> IBinding<Signal<A, B, C>>.Get() { return FireSignal; }

		public void Dispatch(A a, B b, C c)
		{
			Execute(a, b, c);
			base.PostDispatch();
			FireSignal.Dispatch(a, b, c);
		}
		public virtual void Execute(A a, B b, C c) { }
	}

	// Command node for three parameters
	public abstract class Command<A, B, C> : CommandBase<A, B, C>, IEnableBinding<Action<A, B, C>>
	{
		public IBinding<Signal<A, B, C>> Trigger = new Signal<A, B, C>();
		Action<A, B, C> IBinding<Action<A, B, C>>.Get() { return Dispatch; }

		public override void Enable()
		{
			base.Enable();
			Trigger.Get().AddListener(Dispatch);
			Trigger.Get().AddListener(gameObject, Dispatch);
		}

		public override void Disable()
		{
			base.Disable();
			Trigger.Get().RemoveListener(Dispatch);
			Trigger.Get().RemoveListener(gameObject, Dispatch);
		}
	}
}