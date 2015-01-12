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