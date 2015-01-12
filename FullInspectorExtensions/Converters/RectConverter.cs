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