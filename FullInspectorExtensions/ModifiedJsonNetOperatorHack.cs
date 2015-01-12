﻿/*
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

namespace FullInspector.Serializers.ModifiedJsonNet
{
	/// <summary>
	/// Normally we would use the JsonNetSerializer context object to fetch the active ISerializationOperator
	/// instance, but WinRT breaks the StreamingContext object. We use this hack to fix the WinRT build.
	/// </summary>
	public static class JsonNetOperatorHack
	{
		public static ISerializationOperator ActivateOperator;
	}
}