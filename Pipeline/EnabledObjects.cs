using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Object that may have fields/properties with [Enable] tags
public interface IEnableContainer
{
	IEnumerable<IEnabled> EnabledObjects { get; set; }
	BaseContext context { get; }
}

// Object that may be enabled
public interface IEnabled
{
	void Create(BaseContext context);
	void Enable();
	void Disable();
	void Destroy();
	bool Owned { get; set; }
}

public class BaseEnabled : IEnabled
{
	private List<IEnabled> m_enabledObjects = new List<IEnabled>();

	public virtual void Create(BaseContext context)
	{
		context.Inject(this);
		m_enabledObjects = EnableContainerImpl.GetActivationMemberCache(this.GetType())(this).ToList();

		foreach (var enabledObject in m_enabledObjects)
			enabledObject.Owned = true;

		foreach (var enabledObject in m_enabledObjects)
			enabledObject.Create(context);
	}

	public void Enable()
	{
		foreach (var enabledObject in m_enabledObjects)
			enabledObject.Enable();
	}

	public void Disable()
	{
		foreach (var enabledObject in m_enabledObjects)
			enabledObject.Disable();
	}

	public void Destroy()
	{
		foreach (var enabledObject in m_enabledObjects)
			enabledObject.Destroy();
	}
	public bool Owned { get; set; }
}

// This class will perform reflection to find [Enable] tags, and will iterate the found fields/properties to perform Start/Enable/Disable behavior on any IEnabled
public static class EnableContainerImpl
{
	private static Dictionary<Type, Func<object, IEnumerable<IEnabled>>> m_activationMemberCache = new Dictionary<Type, Func<object, IEnumerable<IEnabled>>>();

	public static Func<object, IEnumerable<IEnabled>> GetActivationMemberCache(Type type)
	{
		if (!m_activationMemberCache.ContainsKey(type))
		{
			// Find appropriate members
			var activationProperties = new List<PropertyInfo>();
			var activationFields = new List<FieldInfo>();
			foreach (var thisMember in type.FindMembers(MemberTypes.Property | MemberTypes.Field, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance, null, null))
			{
				var activationAttrs = thisMember.GetCustomAttributes(typeof(EnableAttribute), true);
				if (thisMember.MemberType == MemberTypes.Property)
				{
					if (activationAttrs.Any())
						activationProperties.Add(thisMember as PropertyInfo);
				}
				else
				{
					FieldInfo fieldInfo = thisMember as FieldInfo;
					if (activationAttrs.Any() || fieldInfo.FieldType == typeof(IEnabled) || 
						(fieldInfo.FieldType.IsGenericType && 
							(fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(IBinding<>) || fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(IEnableBinding<>))))
						activationFields.Add(fieldInfo);
				}
			}

			// Take a view and find all the non-null IEnabled objects in it
			m_activationMemberCache[type] = (object view) =>
			{
				return activationProperties.Select(activationProperty => activationProperty.GetValue(view, null) as IEnabled)
					.Concat(activationFields.Select(activationField => activationField.GetValue(view) as IEnabled))
					.Where(o => o != null && !o.Owned);
			};
		}

		return m_activationMemberCache[type];
	}

	public static void Create(IEnableContainer container)
	{
		if (Application.isPlaying)
		{
			if (!container.EnabledObjects.Any())
			{
				// Cache off a function that takes a container and returns an enumeration of fields
				var fieldFunc = GetActivationMemberCache(container.GetType());

				// Save off the list
				container.EnabledObjects = fieldFunc(container).ToList();
			}

			foreach (var enabledObject in container.EnabledObjects)
				enabledObject.Owned = true;

			foreach (var enabledObject in container.EnabledObjects)
				enabledObject.Create(container.context);
		}
	}

	// Enable/Disable should always be after start
	public static void Enable(IEnableContainer container)
	{
		foreach (var enabledObject in container.EnabledObjects)
		{
			enabledObject.Enable();
		}
	}

	public static void Disable(IEnableContainer container)
	{
		foreach (var enabledObject in container.EnabledObjects)
		{
			enabledObject.Disable();
		}
	}

	public static void Destroy(IEnableContainer container)
	{
		foreach (var enabledObject in container.EnabledObjects)
		{
			enabledObject.Destroy();
		}
	}
}
