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
