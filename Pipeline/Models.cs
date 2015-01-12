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
