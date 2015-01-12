using FullInspector.Internal;
using FullSerializer.Internal;
using Newtonsoft.ModifiedJson;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;

namespace FullInspector.Serializers.ModifiedJsonNet {
    /// <summary>
    /// Converts all types that derive from UnityObject.
    /// </summary>
    public class AssetConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return typeof(Asset).Resolve().IsAssignableFrom(objectType.Resolve());
        }

		private Dictionary<string, Asset> m_assetDictionary = null;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) 
		{
            // null may have been serialized automatically by Json.NET, so we need to recover handle
            // the null case
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