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

public class Asset : FullInspector.BaseScriptableObject { }

public interface IPrefabSerializableView { }

// Allow the serializer to attempt to uniquely reference a gameobject by assuming it is a prefab
public class Prefab
{
	public GameObject Value;
	public Prefab(GameObject value) { Value = value; }
}

public struct Tuple<T, U> : IEquatable<Tuple<T,U>>
{
	public T First;
	public U Second;
	
	public Tuple(T first, U second)
	{
		this.First = first;
		this.Second = second;
	}

	public override int GetHashCode()
	{
		return (First == null ? 0 : First.GetHashCode()) ^ (Second == null ? 0 : Second.GetHashCode());
	}
	
	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		return Equals((Tuple<T, U>)obj);
	}
	
	public bool Equals(Tuple<T, U> other)
	{
		return other.First.Equals(First) && other.Second.Equals(Second);
	}

	public override string ToString ()
	{
		return string.Format ("{0} / {1}", First, Second);
	}
}

public struct Tuple<T, U, V> : IEquatable<Tuple<T,U,V>>
{
	public T First;
	public U Second;
	public V Third;
	
	public Tuple(T first, U second, V third)
	{
		this.First = first;
		this.Second = second;
		this.Third = third;
	}
	
	public override int GetHashCode()
	{
		return First.GetHashCode() ^ Second.GetHashCode() ^ Third.GetHashCode();
	}
	
	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		return Equals((Tuple<T, U, V>)obj);
	}
	
	public bool Equals(Tuple<T, U, V> other)
	{
		return other.First.Equals(First) && other.Second.Equals(Second) && other.Third.Equals(Third);
	}
	
	public override string ToString ()
	{
		return string.Format ("{0} / {1} / {2}", First, Second, Third);
	}
}

public static class Tuple
{
	public static Tuple<T, U> New<T, U>(T first, U second)
	{
		return new Tuple<T, U>(first, second);
	}

	public static Tuple<T, U, V> New<T, U, V>(T first, U second, V third)
	{
		return new Tuple<T, U, V>(first, second, third);
	}
}
