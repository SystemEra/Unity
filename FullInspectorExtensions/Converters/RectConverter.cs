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

namespace FullInspector.Serializers.ModifiedJsonNet
{
	public class RectConverter : JsonConverter
	{
		[JsonObject(MemberSerialization.OptIn)]
		private struct Rec
		{
			[JsonProperty]
			public float x;

			[JsonProperty]
			public float y;

			[JsonProperty]
			public float w;

			[JsonProperty]
			public float h;
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(Rect) == objectType || typeof(Rect?) == objectType;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
			JsonSerializer serializer)
		{

			if (reader.TokenType == JsonToken.Null) return null;

			var rect = serializer.Deserialize<Rec>(reader);
			return new Rect(rect.x, rect.y, rect.w, rect.h);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var rect = (Rect)value;
			var rec = new Rec()
			{
				x = rect.x,
				y = rect.y,
				w = rect.width,
				h = rect.height,
			};

			serializer.Serialize(writer, rec);
		}
	}
}