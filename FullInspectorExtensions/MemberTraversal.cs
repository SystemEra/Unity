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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace FullInspector
{
	/// <summary>
	/// Takes a parent object and uses one stored delegates retrieve
	/// a related object, presumably from a child field or enumerable
	/// </summary>
	[Serializable]
	public class fiMemberTraversal {

		/// <summary>
		/// Traversal getter
		/// </summary>
		private Func<object, object> m_traversal;

		/// <summary>
		/// Traversal setter 
		/// </summary>
		private Action<object, object> m_setter;

		/// <summary>
		/// Invoke the delegate to retrieve the internal
		/// </summary>
		public object Get(object parent){
			return m_traversal(parent);
		}

		/// <summary>
		/// Invoke the setter delegate
		/// </summary>
		public void Set(object parent, object value) {
			m_setter(parent, value);
		}

		/// <summary>
		/// Create a traversal
		/// </summary>
		public fiMemberTraversal(Func<object, object> traversal) { 
			m_traversal = traversal; 
		}

		/// <summary>
		/// Create a traversal
		/// </summary>
		public fiMemberTraversal(Func<object, object> traversal, Action<object, object> setter) { 
			m_traversal = traversal; 
			m_setter = setter; 
		}

		public string Name { get; set; }
	}

	/// <summary>
	/// Generic version of a single item traversal
	/// </summary>
	public class fiMemberTraversal<T> : fiMemberTraversal where T : class
	{
		/// <summary>
		/// Invoke the delegate to retrieve the typed internal
		/// </summary>
		public new T Get(object parent) { return Get(parent) as T; }

		/// <summary>
		/// Create a traversal
		/// </summary>
		public fiMemberTraversal(Func<object, T> traversal) : base(t => traversal(t)) { }
	}

	/// <summary>
	/// A traversal with an immediate return value available, for utility only. 
	/// </summary>
	public class fiMemberTraversalImmediate<T> : fiMemberTraversal<T> where T : class {
		/// <summary>
		/// The immediate value.  Presumed not serialized and not valid on clone.
		/// </summary>
		public T Immediate = null;

		/// <summary>
		/// Create an immediate traversal
		/// </summary>
		public fiMemberTraversalImmediate(T immediate, Func<object, T> traversal) 
			: base(t => traversal(t)) { 
			Immediate = immediate; 
		}
	}

	/// <summary>
	/// Takes a parent object and uses stored delegates to traverse downward
	/// into its internals.  Useful for representing how to "get" to an item
	/// stored deep in a complex object.
	/// </summary>
	[Serializable]
	public class fiNestedMemberTraversal {
		/// <summary>
		/// List of traversals which will in order retrieve the item
		/// </summary>
		public List<fiMemberTraversal> Item = new List<fiMemberTraversal>();

		/// <summary>
		/// Invoke the delegates to retrieve the internal
		/// </summary>
		public object Get(object parent)
		{
			object o = parent;
			foreach (fiMemberTraversal traversal in Item)
			{
				o = traversal.Get(o);
			}
			return o;
		}

		/// <summary>
		/// Invoke the delegates to retrieve the internal, and then set on the
		/// final item
		/// </summary>
		public void Set(object parent, object value)
		{
			object o = parent;
			foreach (fiMemberTraversal traversal in Item.Take(Item.Count() - 1))
			{
				o = traversal.Get(o);
			}
			Item.Last().Set(o, value);
		}

		/// <summary>
		/// Type cast the traversal into a different generic
		/// </summary>
		public fiNestedMemberTraversal<U> As<U>() where U : class { return new fiNestedMemberTraversal<U>(Item); }

		/// <summary>
		/// Construct a null traversal (returns the parent)
		/// </summary>
		public fiNestedMemberTraversal() { 
		}

		/// <summary>
		/// Copy a traversal
		/// </summary>
		public fiNestedMemberTraversal(fiNestedMemberTraversal other) { 
			Item = other.Item; 
		}

		/// <summary>
		/// Concatenate two traversals
		/// </summary>
		public fiNestedMemberTraversal(fiNestedMemberTraversal a, fiNestedMemberTraversal b) {
			Item = a.Item.Concat(b.Item).ToList(); 
		}

		/// <summary>
		/// Build a nested traversal off a single traversal
		/// </summary>
		public fiNestedMemberTraversal(fiMemberTraversal traversal) { 
			Item = new List<fiMemberTraversal>() { traversal }; 
		}

		/// <summary>
		/// Build a traversal off a list of single traversals
		/// </summary>
		public fiNestedMemberTraversal(List<fiMemberTraversal> item) { Item = item; }

		/// <summary>
		/// Concatenate a nested traversal with an additional single traversal
		/// </summary>
		public fiNestedMemberTraversal(fiNestedMemberTraversal other, fiMemberTraversal traversal) { 
			Item = other != null 
				? other.Item.Concat(new List<fiMemberTraversal>() { traversal }).ToList() 
				: new List<fiMemberTraversal>(); 
		}

		/// <summary>
		/// Concatenate a single traversal onto this one
		/// </summary>
		public fiNestedMemberTraversal Append(fiMemberTraversal traversal) { 
			return new fiNestedMemberTraversal(this, traversal); 
		}

		/// <summary>
		/// Concatenate a typed nested traversal onto this one
		/// </summary>
		public fiNestedMemberTraversal<U> Append<U>(fiNestedMemberTraversal<U> b) where U : class { 
			return new fiNestedMemberTraversal<U>(this, b); 
		}

		/// <summary>
		/// Concatenate a typed traversal onto this one
		/// </summary>
		public fiNestedMemberTraversal<U> Append<U>(fiMemberTraversal<U> traversal) where U : class { return new fiNestedMemberTraversal<U>(this, traversal); }

		/// <summary>
		/// Concatenate a typed immediate traversal onto this one
		/// </summary>
		public fiNestedMemberTraversalImmediate<U> Append<U>(fiMemberTraversalImmediate<U> traversal) where U : class { 
			return new fiNestedMemberTraversalImmediate<U>(this, traversal); 
		}

		public override string ToString()
		{
			return string.Join(" » ", Item.Where(i => i.Name != null).Select(i => i.Name).ToArray());
		}
	}

	/// <summary>
	/// Typed version of a nested traversal
	/// </summary>
	public class fiNestedMemberTraversal<T> : fiNestedMemberTraversal where T : class {
		/// <summary>
		/// Get the typed traversed value
		/// </summary>
		public new T Get(object parent) { return base.Get(parent) as T; }

		/// <summary>
		/// Typed setter
		/// </summary>
		public void Set(object parent, T value) { base.Set(parent, value); }

		/// <summary>
		/// Construct a null traversal (returns the parent)
		/// </summary>
		public fiNestedMemberTraversal() { }

		/// <summary>
		/// Build a traversal of the same type
		/// </summary>
		public fiNestedMemberTraversal(fiNestedMemberTraversal<T> other) : base(other) { }

		/// <summary>
		/// Concatenate a specified traversal with one of this type
		/// </summary>
		public fiNestedMemberTraversal(fiNestedMemberTraversal a, fiNestedMemberTraversal<T> b) : base(a, b) { }

		/// <summary>
		/// Build a typed single traversal
		/// </summary>
		public fiNestedMemberTraversal(fiMemberTraversal<T> traversal) { 
			Item = new List<fiMemberTraversal>() { traversal }; 
		}

		/// <summary>
		/// Build from a list of typed single traversals
		/// </summary>
		public fiNestedMemberTraversal(List<fiMemberTraversal> item) { 
			Item = item; 
		}

		/// <summary>
		/// Concatenate a nested traversal with a single typed traversal
		/// </summary>
		public fiNestedMemberTraversal(fiNestedMemberTraversal other, fiMemberTraversal<T> traversal) { 
			Item = other.Item.Concat(new List<fiMemberTraversal>() { traversal }).ToList(); 
		}
	}

	/// <summary>
	/// A traversal with an immediate return value available, for utility only
	/// </summary>
	public class fiNestedMemberTraversalImmediate<T> : fiNestedMemberTraversal<T> where T : class {
		/// <summary>
		/// The immediate typed value.  Presumed not serialized and not valid on clone.
		/// </summary>
		public T Immediate = null;

		/// <summary>
		/// Construct a null traversal (returns the parent)
		/// </summary>
		public fiNestedMemberTraversalImmediate() { }

		/// <summary>
		/// Build a traversal of the same type
		/// </summary>
		public fiNestedMemberTraversalImmediate(fiMemberTraversalImmediate<T> traversal) : base(traversal) { Immediate = traversal.Immediate; }

		/// <summary>
		/// Concatenate a specified traversal with one of this type
		/// </summary>
		public fiNestedMemberTraversalImmediate(T immediate, fiMemberTraversal<T> traversal) : base(traversal) { Immediate = immediate; }

		/// <summary>
		/// Build from a list of typed single traversals
		/// </summary>
		public fiNestedMemberTraversalImmediate(T immediate, List<fiMemberTraversal> item) : base(item) { Immediate = immediate; }

		/// <summary>
		/// Concatenate a nested traversal with a single typed immediate traversal
		/// </summary>
		public fiNestedMemberTraversalImmediate(fiNestedMemberTraversal other, fiMemberTraversalImmediate<T> traversal) : base(other, traversal) { Immediate = traversal.Immediate; }

		/// <summary>
		/// Concatenate a nested traversal with a single typed traversal, with a new immediate
		/// </summary>
		public fiNestedMemberTraversalImmediate(T immediate, fiNestedMemberTraversal other, fiMemberTraversal<T> traversal) : base(other, traversal) { Immediate = immediate; }

		/// <summary>
		/// Concatenate a specified traversal with one of this type, with a new immediate
		/// </summary>
		public fiNestedMemberTraversalImmediate(T immediate, fiNestedMemberTraversal<T> other) : base(other) { Immediate = immediate; }
	}

	/// <summary>
	/// Metadata for 
	/// </summary>
	public class fiMemberTraversalMetadata : FullInspector.IGraphMetadataItem {
		/// <summary>
		/// Type of the field owner
		/// </summary>
		public Type DeclaringType;

		/// <summary>
		/// Type of the field being edited
		/// </summary>
		public Type StorageType;

		/// <summary>
		/// Reflection info for the field
		/// </summary>
		public MemberInfo MemberInfo;

		/// <summary>
		/// Traversal of field being edited
		/// </summary>
		[NonSerialized] public fiNestedMemberTraversal NestedMemberTraversal;
	}
}