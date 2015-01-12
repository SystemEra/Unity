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
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;

namespace FullInspector {
    public static class SerializationHelpersExt {
        /// <summary>
        /// Caches the results of serialization.
        /// </summary>
        public struct CachedSerializationResults
        {
            public List<UnityObject> Objects;
            public string Serialized;
        }

        /// <summary>
        /// Store serialization results as a cache by object reference, for objects which the user knows will not change
        /// </summary>
        private static Dictionary<object, CachedSerializationResults> m_serializationCache = new Dictionary<object, CachedSerializationResults>();

        /// <summary>
        /// Clones the given object allowing cached versions of the serialized object to be used
        /// </summary>
        /// <typeparam name="T">The type of object to clone.</typeparam>
        /// <typeparam name="TSerializer">The serializer to do the cloning with.</typeparam>
        /// <param name="obj">The object to clone.</param>
        /// <returns>A duplicate of the given object.</returns>
        public static T CloneCached<T, TSerializer>(T obj)
            where TSerializer : BaseSerializer
        {
            if (obj == null) return default(T);
            var serializer = fiSingletons.Get<TSerializer>();
            var serializationOperator = fiSingletons.Get<ListSerializationOperator>();

            string serialized;
            CachedSerializationResults serializationResults;
            if (m_serializationCache.TryGetValue(obj, out serializationResults))
            {
                serializationOperator.SerializedObjects = serializationResults.Objects;
                serialized = serializationResults.Serialized;
            }
            else
            {
                serializationOperator.SerializedObjects = new List<UnityObject>();
                serialized = serializer.Serialize(fsPortableReflection.AsMemberInfo(typeof(T)), obj, serializationOperator);
                m_serializationCache[obj] = new CachedSerializationResults() { Objects = serializationOperator.SerializedObjects, Serialized = serialized };
            }

            object deserialized = serializer.Deserialize(fsPortableReflection.AsMemberInfo(typeof(T)), serialized, serializationOperator);

            serializationOperator.SerializedObjects = null;

            return (T)deserialized;
        }
    }
}
