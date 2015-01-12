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

 * The Strange IOC Framework is hosted at https://github.com/strangeioc/strangeioc. 
 * Code contained here may contain modified code which is subject to the license 
 * at this link.
 */

using System;
using System.Collections.Generic;
using strange.extensions.signal.impl;

namespace strange.extensions.policysignal.impl
{
	/// Base concrete form for a Signal with no parameters
	public class PolicySignalBase : BaseSignal
	{
		public event Action Listener = delegate { };
		public event Action OnceListener = delegate { };
		public void AddListener(Action callback) { Listener += callback; }
		public void AddOnce(Action callback) { OnceListener += callback; }
		public void RemoveListener(Action callback) { Listener -= callback; }
		public override List<Type> GetTypes()
		{
			return new List<Type>();
		}
		public void Dispatch()
		{
			Listener();
			OnceListener();
			OnceListener = delegate { };
			base.Dispatch(null);
		}
	}

	public class PolicySignal : PolicySignalBase
	{
		private Dictionary<object, Signal> namedSignals = new Dictionary<object, Signal>();
		public void Dispatch(object name)
		{
			if (name != null && namedSignals.ContainsKey(name))
				namedSignals[name].Dispatch();
		}

		public void AddListener(object name, Action callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal());
			namedSignals[name].AddListener(callback);
		}

		public void AddOnce(object name, Action callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal());
			namedSignals[name].AddOnce(callback);
		}

		public void RemoveListener(object name, Action callback) { if (namedSignals.ContainsKey(name)) namedSignals[name].RemoveListener(callback); }
	}

	/// Base concrete form for a Signal with one parameter
	public class PolicySignalBase<T> : BaseSignal
	{
		public event Action<T> Listener = delegate { };
		public event Action<T> OnceListener = delegate { };
		public void AddListener(Action<T> callback) { Listener += callback; }
		public void AddOnce(Action<T> callback) { OnceListener += callback; }
		public void RemoveListener(Action<T> callback) { Listener -= callback; }
		public override List<Type> GetTypes()
		{
			List<Type> retv = new List<Type>();
			retv.Add(typeof(T));
			return retv;
		}
		public void Dispatch(T type1)
		{
			Listener(type1);
			OnceListener(type1);
			OnceListener = delegate { };
			object[] outv = { type1 };
			base.Dispatch(outv);
		}
	}

	public class PolicySignal<T> : PolicySignalBase<T>
	{
		private Dictionary<object, Signal<T>> namedSignals = new Dictionary<object, Signal<T>>();
		public void Dispatch(object name, T type1)
		{
			if (name != null && namedSignals.ContainsKey(name))
				namedSignals[name].Dispatch(type1);
		}

		public void AddListener(object name, Action<T> callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal<T>());
			namedSignals[name].AddListener(callback);
		}

		public void AddOnce(object name, Action<T> callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal<T>());
			namedSignals[name].AddOnce(callback);
		}

		public void RemoveListener(object name, Action<T> callback) { if (namedSignals.ContainsKey(name)) namedSignals[name].RemoveListener(callback); }
	}

	/// Base concrete form for a Signal with two parameters
	public class PolicySignalBase<T, U> : BaseSignal
	{
		public event Action<T, U> Listener = delegate { };
		public event Action<T, U> OnceListener = delegate { };
		public void AddListener(Action<T, U> callback) { Listener += callback; }
		public void AddOnce(Action<T, U> callback) { OnceListener += callback; }
		public void RemoveListener(Action<T, U> callback) { Listener -= callback; }
		public override List<Type> GetTypes()
		{
			List<Type> retv = new List<Type>();
			retv.Add(typeof(T));
			retv.Add(typeof(U));
			return retv;
		}
		public void Dispatch(T type1, U type2)
		{
			Listener(type1, type2);
			OnceListener(type1, type2);
			OnceListener = delegate { };
			object[] outv = { type1, type2 };
			base.Dispatch(outv);
		}
	}

	public class PolicySignal<T, U> : PolicySignalBase<T, U>
	{
		private Dictionary<object, Signal<T, U>> namedSignals = new Dictionary<object, Signal<T, U>>();
		public void Dispatch(object name, T type1, U type2)
		{
			if (name != null && namedSignals.ContainsKey(name))
				namedSignals[name].Dispatch(type1, type2);
		}

		public void AddListener(object name, Action<T, U> callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal<T, U>());
			namedSignals[name].AddListener(callback);
		}

		public void AddOnce(object name, Action<T, U> callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal<T, U>());
			namedSignals[name].AddOnce(callback);
		}

		public void RemoveListener(object name, Action<T, U> callback) { if (namedSignals.ContainsKey(name)) namedSignals[name].RemoveListener(callback); }
	}

	/// Base concrete form for a Signal with three parameters
	public class PolicySignalBase<T, U, V> : BaseSignal
	{
		public event Action<T, U, V> Listener = delegate { };
		public event Action<T, U, V> OnceListener = delegate { };
		public void AddListener(Action<T, U, V> callback) { Listener += callback; }
		public void AddOnce(Action<T, U, V> callback) { OnceListener += callback; }
		public void RemoveListener(Action<T, U, V> callback) { Listener -= callback; }
		public override List<Type> GetTypes()
		{
			List<Type> retv = new List<Type>();
			retv.Add(typeof(T));
			retv.Add(typeof(U));
			retv.Add(typeof(V));
			return retv;
		}
		public void Dispatch(T type1, U type2, V type3)
		{
			Listener(type1, type2, type3);
			OnceListener(type1, type2, type3);
			OnceListener = delegate { };
			object[] outv = { type1, type2, type3 };
			base.Dispatch(outv);
		}
	}

	public class PolicySignal<T, U, V> : PolicySignalBase<T, U, V>
	{
		private Dictionary<object, Signal<T, U, V>> namedSignals = new Dictionary<object, Signal<T, U, V>>();
		public void Dispatch(object name, T type1, U type2, V type3)
		{
			if (name != null && namedSignals.ContainsKey(name))
				namedSignals[name].Dispatch(type1, type2, type3);
		}

		public void AddListener(object name, Action<T, U, V> callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal<T, U, V>());
			namedSignals[name].AddListener(callback);
		}

		public void AddOnce(object name, Action<T, U, V> callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal<T, U, V>());
			namedSignals[name].AddOnce(callback);
		}

		public void RemoveListener(object name, Action<T, U, V> callback) { if (namedSignals.ContainsKey(name)) namedSignals[name].RemoveListener(callback); }
	}

	/// Base concrete form for a Signal with four parameters
	public class PolicySignalBase<T, U, V, W> : BaseSignal
	{
		public event Action<T, U, V, W> Listener = delegate { };
		public event Action<T, U, V, W> OnceListener = delegate { };
		public void AddListener(Action<T, U, V, W> callback) { Listener += callback; }
		public void AddOnce(Action<T, U, V, W> callback) { OnceListener += callback; }
		public void RemoveListener(Action<T, U, V, W> callback) { Listener -= callback; }
		public override List<Type> GetTypes()
		{
			List<Type> retv = new List<Type>();
			retv.Add(typeof(T));
			retv.Add(typeof(U));
			retv.Add(typeof(V));
			retv.Add(typeof(W));
			return retv;
		}
		public void Dispatch(T type1, U type2, V type3, W type4)
		{
			Listener(type1, type2, type3, type4);
			OnceListener(type1, type2, type3, type4);
			OnceListener = delegate { };
			object[] outv = { type1, type2, type3, type4 };
			base.Dispatch(outv);
		}
	}

	public class PolicySignal<T, U, V, W> : PolicySignalBase<T, U, V, W>
	{
		private Dictionary<object, Signal<T, U, V, W>> namedSignals = new Dictionary<object, Signal<T, U, V, W>>();
		public void Dispatch(object name, T type1, U type2, V type3, W type4)
		{
			if (name != null && namedSignals.ContainsKey(name))
				namedSignals[name].Dispatch(type1, type2, type3, type4);
		}

		public void AddListener(object name, Action<T, U, V, W> callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal<T, U, V, W>());
			namedSignals[name].AddListener(callback);
		}

		public void AddOnce(object name, Action<T, U, V, W> callback)
		{
			if (!namedSignals.ContainsKey(name))
				namedSignals.Add(name, new Signal<T, U, V, W>());
			namedSignals[name].AddOnce(callback);
		}

		public void RemoveListener(object name, Action<T, U, V, W> callback) { if (namedSignals.ContainsKey(name)) namedSignals[name].RemoveListener(callback); }
	}

}
