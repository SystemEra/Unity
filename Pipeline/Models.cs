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

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IModel 
{
	string Serialize();
}

public class BaseViewModel : IModel
{
	public string Serialize()
	{
		FullInspector.ModifiedJsonNetSerializer.Formatting = Newtonsoft.ModifiedJson.Formatting.Indented;
		string data = FullInspector.SerializationHelpers.SerializeToContent<FullInspector.ModifiedJsonNetSerializer>(this.GetType(), this);
		FullInspector.ModifiedJsonNetSerializer.Formatting = Newtonsoft.ModifiedJson.Formatting.None;
		return data;
	}

	public static ModelType Deserialize<ModelType>(string data) where ModelType : BaseViewModel
	{
		return FullInspector.SerializationHelpers.DeserializeFromContent<FullInspector.ModifiedJsonNetSerializer>(typeof(ModelType), data) as ModelType;
	}
}

public class TransformModel : BaseViewModel
{
	public Vector3 Position;
	public Quaternion Rotation;
}
