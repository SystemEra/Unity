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
	public class Vector2Converter : JsonConverter
	{
		[JsonObject(MemberSerialization.OptIn)]
		private struct Vec2
		{
			[JsonProperty]
			public float x;

			[JsonProperty]
			public float y;
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(Vector2) == objectType || typeof(Vector2?) == objectType;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
			JsonSerializer serializer)
		{

			if (reader.TokenType == JsonToken.Null) return null;

			var vec = serializer.Deserialize<Vec2>(reader);
			return new Vector2(vec.x, vec.y);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector2 = (Vector2)value;
			var vec2 = new Vec2()
			{
				x = vector2.x,
				y = vector2.y
			};

			serializer.Serialize(writer, vec2);
		}
	}

	public class Vector3Converter : JsonConverter
	{
		[JsonObject(MemberSerialization.OptIn)]
		private struct Vec3
		{
			[JsonProperty]
			public float x;

			[JsonProperty]
			public float y;

			[JsonProperty]
			public float z;
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(Vector3) == objectType || typeof(Vector3?) == objectType;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
			JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null) return null;

			var vec = serializer.Deserialize<Vec3>(reader);
			return new Vector3(vec.x, vec.y, vec.z);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector3 = (Vector3)value;
			var vec3 = new Vec3()
			{
				x = vector3.x,
				y = vector3.y,
				z = vector3.z
			};

			serializer.Serialize(writer, vec3);
		}
	}

	public class Vector4Converter : JsonConverter
	{
		[JsonObject(MemberSerialization.OptIn)]
		private struct Vec4
		{
			[JsonProperty]
			public float x;

			[JsonProperty]
			public float y;

			[JsonProperty]
			public float z;

			[JsonProperty]
			public float w;
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(Vector4) == objectType || typeof(Vector4?) == objectType;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
			JsonSerializer serializer)
		{

			if (reader.TokenType == JsonToken.Null) return null;

			var vec = serializer.Deserialize<Vec4>(reader);
			return new Vector4(vec.x, vec.y, vec.z, vec.w);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var vector4 = (Vector4)value;
			var vec4 = new Vec4()
			{
				x = vector4.x,
				y = vector4.y,
				z = vector4.z,
				w = vector4.w
			};

			serializer.Serialize(writer, vec4);
		}
	}
}