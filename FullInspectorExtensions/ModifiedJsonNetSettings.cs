using Newtonsoft.ModifiedJson;

namespace FullInspector.Serializers.ModifiedJsonNet
{
	public static class JsonNetSettings
	{
		/// <summary>
		/// Add any custom JsonConverters here.
		/// </summary>
		public static JsonConverter[] CustomConverters = new JsonConverter[] {
        };

		/// <summary>
		/// Should [JsonObject(MemberSerialization.OptIn)] annotations (or a JsonConverter) be
		/// required for all objects that are serialized?
		/// </summary>
		public static bool RequireOptInAnnotation = true;
	}
}