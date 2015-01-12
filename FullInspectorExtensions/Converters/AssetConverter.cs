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
using System.Linq;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;

namespace FullInspector.Serializers.ModifiedJsonNet {
    public class AssetConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return typeof(Asset).Resolve().IsAssignableFrom(objectType.Resolve());
        }

		private Dictionary<string, Asset> m_assetDictionary = null;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) 
		{
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }

			var serializationOperator = FullInspector.Serializers.ModifiedJsonNet.JsonNetOperatorHack.ActivateOperator;
			if (serializationOperator is FullInspector.NotSupportedSerializationOperator)
			{
				Tuple<string, string> assetTuple = serializer.Deserialize <Tuple<string, string>>(reader);

				if (m_assetDictionary == null)
					m_assetDictionary = UnityEngine.Resources.FindObjectsOfTypeAll<Asset>().ToDictionary(a => a.GetType().ToString() + "-" + a.name, a => a);

				return m_assetDictionary[assetTuple.First + "-" + assetTuple.Second];
			}
			else
				return new UnityObjectConverter().ReadJson(reader, objectType, existingValue, serializer);
            
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) 
		{
			var serializationOperator = FullInspector.Serializers.ModifiedJsonNet.JsonNetOperatorHack.ActivateOperator;

			if (serializationOperator is FullInspector.NotSupportedSerializationOperator)
			{
				string assetType = value.GetType().ToString();
				string assetName = (value as Asset).name;
				serializer.Serialize(writer, new Tuple<string, string>(assetType, assetName));
			}
			else
				new UnityObjectConverter().WriteJson(writer, value, serializer);
        }
    }
}