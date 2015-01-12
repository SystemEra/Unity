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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

// Lots of reflection utility functions
public static class TypeUtil
{
	public static object InvokeDefaultConstructor(this Type type)
	{
		return type.GetConstructor(new Type[] { }).Invoke(new object[] { });
	}

	public static void Swap<T>(ref T a, ref T b)
	{
		T temp = a;
		a = b;
		b = temp;
	}

	public static void BreakpointMe()
	{
		UnityEngine.Debug.Log("Breakpoint here");
	}

	public static bool IsGenericSubclass(Type type, Type genericDefinition)
	{
		if (type.IsGenericTypeDefinition) return false;
		while (type != null)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition)
				return true;
			if (genericDefinition.IsInterface && type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == genericDefinition))
				return true;
			type = type.BaseType;
		}
		return false;
	}

	public static Type[] GetGenericSubclassArguments(Type type, Type genericDefinition)
	{
		if (type.IsGenericTypeDefinition) return new Type[] {};
		while (type != null)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition)
				return type.GetGenericArguments();
			if (genericDefinition.IsInterface)
			{
				var interfaceGenericType = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == genericDefinition);
				if (interfaceGenericType != null)
					return interfaceGenericType.GetGenericArguments();
			}
			type = type.BaseType;
		}
		return new Type[] {};
	}

	public static Type GetGenericSubclass(Type type, Type genericDefinition)
	{
		return genericDefinition.MakeGenericType(GetGenericSubclassArguments(type, genericDefinition));
	}

	public static object Default(Type t)
	{
		if (t.IsValueType)
			return Activator.CreateInstance(t);
		else if (t == typeof(string))
			return "";
		else if (t.IsAbstract || t.IsSubclassOf(typeof(UnityEngine.Object)))
			return null;
		else
		{
			var constructor = t.GetConstructor(new Type[] { });
			if (constructor != null) return constructor.Invoke(new object[] { });
			else return null;
		}
	}

	private class DefaultFunc<T>
	{
		private static T Invoke() { return Default<T>(); }
		public static Func<T> GetInvoke() { return Invoke; }
	}

	public static T Default<T>()
	{
		if (typeof(T).IsValueType || typeof(T).IsSubclassOf(typeof(UnityEngine.Object)))
			return default(T);
		else if (typeof(T) == typeof(string))
			return (T)(object)"";
		else if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Func<>))
		{
			Type defaultFuncType = typeof(DefaultFunc<>).MakeGenericType(new Type[] { typeof(T).GetGenericArguments()[0] });
			return (T)defaultFuncType.GetMethod("GetInvoke").Invoke(null, new object[] {});
		}
		else 
		{
			var constructor = typeof(T).GetConstructor(new Type[] {});
			if (constructor != null) return (T)constructor.Invoke(new object[] {});
			else return default(T);
		}
	}

	public class ConstructOfTypeDefault
	{
		Type T;
		public ConstructOfTypeDefault(Type t) { T = t; }
		
		public object Invoke(object[] o)
		{
			return TypeUtil.Default(T);
		}
	}

	public static string CleanName(string typeName)
	{
		if (typeName == null) return "";
		return SplitCamelCase(typeName, " ");
	}

	public static string TypeName(Type type, bool stripMenu = true, bool cleanName = true)
	{
		var typeNameAttrs = type.GetCustomAttributes(typeof(NameAttribute), true);
		if (typeNameAttrs.Length > 0)
		{
			string nameValue = (typeNameAttrs[0] as NameAttribute).Value;
			if (stripMenu)
				return nameValue.Split('/').Last();
			else
				return nameValue;
		}
		else
		{
			if (type.IsGenericType)
			{
				if (TypeUtil.IsGenericSubclass(type, typeof(IRef<>)))
					return TypeName(type.GetGenericArguments()[0], stripMenu, cleanName);
				if (TypeUtil.IsGenericSubclass(type, typeof(INamed<>)))
					return TypeName(type.GetGenericArguments()[0], stripMenu, cleanName);
				return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(x => TypeName(x, true, false)).ToArray()) + ">";
			}
			else if (cleanName)
				return CleanName(type.Name);
			else
				return type.Name;
		}
	}
	
	// Get readable field name
	public static string SplitCamelCase(string str, string delimiter)
	{
		string wildcard = "$1" + delimiter + "$2";
		return System.Text.RegularExpressions.Regex.Replace( System.Text.RegularExpressions.Regex.Replace( str, @"(\P{Ll})(\P{Ll}\p{Ll})", wildcard ), @"(\p{Ll})(\P{Ll})", wildcard );
	}
	
	public static string ByteArrayToString(byte[] ba)
	{
		System.Text.StringBuilder hex = new System.Text.StringBuilder(ba.Length * 2);
		foreach (byte b in ba)
			hex.AppendFormat("{0:x2}", b);
		return hex.ToString();
	}
}

// Useful enumerable extensions.
public static class EnumerableExt
{
	public static IEnumerable<T> Single<T>(T t) { return Enumerable.Repeat(t, 1); }
	public static IEnumerable<T> ConcatSingle<T>(this IEnumerable<T> e, T t) { return e.Concat(Single<T>(t)); }
}

public static class CloneExt
{
	public static T Clone<T>(this T other)
	{
		return FullInspector.SerializationHelpers.Clone<T, FullInspector.ModifiedJsonNetSerializer>(other);
	}
	
	public static T CloneCached<T>(this T other)
	{
		return FullInspector.SerializationHelpersExt.CloneCached<T, FullInspector.ModifiedJsonNetSerializer>(other);
	}

	public static void Clone<T>(this T other, T target)
	{
		var serializer = FullInspector.Internal.fiSingletons.Get<FullInspector.ModifiedJsonNetSerializer>();

		var serializationOperator = FullInspector.Internal.fiSingletons.Get<FullInspector.Internal.ListSerializationOperator>();
        serializationOperator.SerializedObjects = new List<UnityEngine.Object>();

		string serialized = serializer.Serialize(typeof(T), other, serializationOperator);
        serializer.Deserialize(target, serialized, serializationOperator);

        serializationOperator.SerializedObjects = null;
	}
}