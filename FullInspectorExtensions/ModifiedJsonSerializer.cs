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

namespace FullInspector {

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

        /// <summary>
        /// The JsonConverters that we need to use for serialization to happen correctly.
        /// </summary>
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

        /// <summary>
        /// Every converter that will be used during (de)serialization.
        /// </summary>
        private static JsonConverter[] AllConverters;

		public static Formatting Formatting = Formatting.None;

        static ModifiedJsonNetSerializer() {
            // create the list of all of the JsonConverters that we will be using
            List<JsonConverter> allConverters = new List<JsonConverter>();
            allConverters.AddRange(RequiredConverters);
			allConverters.AddRange(JsonNetSettings.CustomConverters);
            AllConverters = allConverters.ToArray();

            // the settings we use for serialization
            Settings = new JsonSerializerSettings() {
                Converters = AllConverters,

                // ensure that we recreate containers and don't just append to them if they are
                // already allocated (we want to replace whatever Unity deserialized into the
                // list)
                ObjectCreationHandling = ObjectCreationHandling.Replace,

                // handle inheritance correctly
                TypeNameHandling = TypeNameHandling.Auto,

                // don't be so strict about types
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,

                // we want to serialize loops, otherwise self-referential Components won't work
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,

				PreserveReferencesHandling = PreserveReferencesHandling.Objects,
				ContractResolver = new JsonContractResolver(),

                Error = HandleError
            };
        }

        /// <summary>
        /// Formats an ErrorContext for pretty printing.
        /// </summary>
        private static string FormatError(ErrorContext error) {
            return "Attempting to recover from serialization error\n" +
                error.Error.Message + "\n" +
                "Member: " + error.Member + "\n" +
                "OriginalObject: " + error.OriginalObject + "\n" +
                "Exception: " + error.Error;
        }

        /// <summary>
        /// Json.NET callback for when an error occurs. We simply print out the error and continue
        /// with the deserialization process.
        /// </summary>
        private static void HandleError(object sender, ErrorEventArgs e) {
            if (fiSettings.EmitWarnings) {
                Debug.LogWarning(FormatError(e.ErrorContext));
            }
            e.ErrorContext.Handled = true;
        }

        /// <summary>
        /// The serialization settings that are used
        /// </summary>
        private static JsonSerializerSettings Settings;

        public override string Serialize(MemberInfo storageType, object value,
            ISerializationOperator serializationOperator) {

            if (value == null) {
                return "null";
            }

            try {
				JsonNetOperatorHack.ActivateOperator = serializationOperator;
				return JsonConvert.SerializeObject(value, GetStorageType(storageType) , Formatting, Settings);
            }
            finally {
				JsonNetOperatorHack.ActivateOperator = null;
            }
        }

		private string Migrate(string serializedState)
		{
			//return migrated = serializedState.Replace("OldValue", "NewValue");
			return serializedState;
		}

        public override object Deserialize(MemberInfo storageType, string serializedState,
            ISerializationOperator serializationOperator) {

            try {
				JsonNetOperatorHack.ActivateOperator = serializationOperator;
                return JsonConvert.DeserializeObject(Migrate(serializedState), GetStorageType(storageType), Settings);
            }
            finally {
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
