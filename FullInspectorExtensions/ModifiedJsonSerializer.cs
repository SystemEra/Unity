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

using FullInspector.Serializers.ModifiedJsonNet;
using Newtonsoft.ModifiedJson;
using Newtonsoft.ModifiedJson.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

namespace FullInspector
{

	public class ModifiedJsonNetSerializer : BaseSerializer
	{
		public class JsonContractResolver : DefaultContractResolver
		{
			protected override JsonConverter ResolveContractConverter(Type objectType)
			{
				return ModifiedJsonNetSerializer.AllConverters.FirstOrDefault(c => c.CanConvert(objectType));
			}
			protected override List<MemberInfo> GetSerializableMembers(Type objectType)
			{
				return base.GetSerializableMembers(objectType).Where(member =>
				{
					bool isUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(member.DeclaringType) && !typeof(ISerializedObject).IsAssignableFrom(member.DeclaringType);
					if (member.GetCustomAttributes(typeof(NonSerializedAttribute), true).Any() || member.GetCustomAttributes(typeof(NotSerializedAttribute), true).Any())
					{
						return false;
					}
					return isUnityObject || member.MemberType == MemberTypes.Field;
				}).ToList();
			}
		}

		private static JsonConverter[] RequiredConverters = new JsonConverter[] {
			new AssetConverter(),
			new PrefabConverter(),
            new UnityObjectConverter(),
			new RectConverter(),
            new Vector2Converter(),
            new Vector3Converter(),
            new Vector4Converter(),
            new ColorConverter(),
        };

		private static JsonConverter[] AllConverters;

		public static Formatting Formatting = Formatting.None;

		static ModifiedJsonNetSerializer()
		{
			List<JsonConverter> allConverters = new List<JsonConverter>();
			allConverters.AddRange(RequiredConverters);
			allConverters.AddRange(JsonNetSettings.CustomConverters);
			AllConverters = allConverters.ToArray();

			Settings = new JsonSerializerSettings()
			{
				Converters = AllConverters,
				ObjectCreationHandling = ObjectCreationHandling.Replace,
				TypeNameHandling = TypeNameHandling.Auto,
				TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,

				PreserveReferencesHandling = PreserveReferencesHandling.Objects,
				ContractResolver = new JsonContractResolver(),

				Error = HandleError
			};
		}

		private static void HandleError(object sender, ErrorEventArgs e)
		{
			e.ErrorContext.Handled = true;
		}

		private static JsonSerializerSettings Settings;

		public override string Serialize(MemberInfo storageType, object value,
			ISerializationOperator serializationOperator)
		{

			if (value == null)
			{
				return "null";
			}

			try
			{
				JsonNetOperatorHack.ActivateOperator = serializationOperator;
				return JsonConvert.SerializeObject(value, GetStorageType(storageType), Formatting, Settings);
			}
			finally
			{
				JsonNetOperatorHack.ActivateOperator = null;
			}
		}

		private string Migrate(string serializedState)
		{
			//return migrated = serializedState.Replace("OldValue", "NewValue");
			return serializedState;
		}

		public override object Deserialize(MemberInfo storageType, string serializedState,
			ISerializationOperator serializationOperator)
		{

			try
			{
				JsonNetOperatorHack.ActivateOperator = serializationOperator;
				return JsonConvert.DeserializeObject(Migrate(serializedState), GetStorageType(storageType), Settings);
			}
			finally
			{
				JsonNetOperatorHack.ActivateOperator = null;
			}
		}

		public void Deserialize(object target, string serializedState, ISerializationOperator serializationOperator)
		{
			try
			{
				JsonNetOperatorHack.ActivateOperator = serializationOperator;
				JsonConvert.PopulateObject(Migrate(serializedState), target, Settings);
			}
			finally
			{
				JsonNetOperatorHack.ActivateOperator = null;
			}
		}
	}
}
