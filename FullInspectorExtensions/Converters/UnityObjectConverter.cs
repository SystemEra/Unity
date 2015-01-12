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

using FullInspector.Internal;
using FullSerializer.Internal;
using Newtonsoft.ModifiedJson;
using System;
using UnityObject = UnityEngine.Object;

namespace FullInspector.Serializers.ModifiedJsonNet
{
	public class UnityObjectConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(UnityObject).Resolve().IsAssignableFrom(objectType.Resolve());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}

			var serializationOperator = JsonNetOperatorHack.ActivateOperator;

			int componentId = serializer.Deserialize<int>(reader);
			return serializationOperator.RetrieveObjectReference(componentId);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var obj = (UnityObject)value;

			var serializationOperator = JsonNetOperatorHack.ActivateOperator;

			int id = serializationOperator.StoreObjectReference(obj);
			serializer.Serialize(writer, id);
		}
	}
}