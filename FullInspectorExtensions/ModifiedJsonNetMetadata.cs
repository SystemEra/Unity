using Newtonsoft.ModifiedJson;
using System;
using UnityEngine;
using FullInspector;
using System.Collections;

namespace FullInspector.Serializers.ModifiedJsonNet {
    public class ModifiedJsonNetMetadata : fiISerializerMetadata {
        public Guid SerializerGuid {
            get { return new Guid("19bd5fa6-8834-3363-aa86-56e8fbfc6623"); }
        }

        public Type SerializerType {
            get { return typeof(ModifiedJsonNetSerializer); }
        }

        public Type[] SerializationOptInAnnotationTypes {
            get {
                return new Type[] { 
                    typeof(JsonPropertyAttribute),
                    typeof(JsonConverterAttribute)
                };
            }
        }

        public Type[] SerializationOptOutAnnotationTypes {
            get {
                return new Type[] { 
                    typeof(JsonIgnoreAttribute)
                };
            }
        }
    }
}
