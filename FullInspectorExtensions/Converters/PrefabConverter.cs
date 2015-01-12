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
using UnityEngine;

namespace FullInspector.Serializers.ModifiedJsonNet {
    public class PrefabConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return typeof(Prefab).Resolve().IsAssignableFrom(objectType.Resolve());
        }

		private Dictionary<string, GameObject> m_assetDictionary = null;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) 
		{
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }

			var serializationOperator = JsonNetOperatorHack.ActivateOperator;
			if (serializationOperator is FullInspector.NotSupportedSerializationOperator)
			{
				Tuple<string, string> assetTuple = serializer.Deserialize <Tuple<string, string>>(reader);

				if (m_assetDictionary == null)
				{
					var allPrefabs = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
					m_assetDictionary = new Dictionary<string, GameObject>();
					foreach (var prefab in allPrefabs)
					{
						if (prefab.GetComponent(typeof(IPrefabSerializableView)) != null && prefab.transform.parent == null)
						{
							string key = GetPrefabTypeString(prefab) + "-" + prefab.name;
							if (m_assetDictionary.ContainsKey(key))
								Debug.LogError("Indistinguishable prefabs with key " + key);
							else
								m_assetDictionary.Add(key, prefab);
						}
					}
				}

				GameObject result = null;
				try
				{
					result = m_assetDictionary[assetTuple.First + "-" + assetTuple.Second];
				}
				catch (KeyNotFoundException ) {	}
				
				return new Prefab(result);
			}
			else
				return new UnityObjectConverter().ReadJson(reader, objectType, existingValue, serializer);
            
        }

		private string GetPrefabTypeString(GameObject gameObject)
		{
			return string.Join(", ", gameObject.GetComponents(typeof(IPrefabSerializableView)).Select(c => c.GetType().ToString()).ToArray());
		}

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) 
		{
			var serializationOperator = JsonNetOperatorHack.ActivateOperator;

			if (serializationOperator is FullInspector.NotSupportedSerializationOperator)
			{
				string assetType = GetPrefabTypeString((value as Prefab).Value);
				string assetName = (value as Prefab).Value.name;
				serializer.Serialize(writer, new Tuple<string, string>(assetType, assetName));
			}
			else
				new UnityObjectConverter().WriteJson(writer, value, serializer);
        }
    }
}