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


using Newtonsoft.ModifiedJson;
using System;
using UnityEngine;

namespace FullInspector.Serializers.ModifiedJsonNet {
    /// <summary>
    /// Converts UnityEngine.Color types
    /// </summary>
    public class ColorConverter : JsonConverter {
        [JsonObject(MemberSerialization.OptIn)]
        private struct WritableColor {
            [JsonProperty]
            public float r;
            [JsonProperty]
            public float g;
            [JsonProperty]
            public float b;
            [JsonProperty]
            public float a;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Color) || objectType == typeof(Color?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {

            WritableColor writable = serializer.Deserialize<WritableColor>(reader);
            return new Color(writable.r, writable.g, writable.b, writable.a);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var color = (Color)value;
            WritableColor writable = new WritableColor() {
                r = color.r,
                g = color.g,
                b = color.b,
                a = color.a
            };

            serializer.Serialize(writer, writable);
        }
    }
}