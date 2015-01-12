using System;
using UnityEngine;
using System.Collections;

public class HideFieldAttribute : System.Attribute {}
public class RequiredAttribute : System.Attribute { public bool InheritName = false; public RequiredAttribute(bool inheritName = false) { InheritName = inheritName; }}
public class NameAttribute: System.Attribute { public string Value = ""; public NameAttribute(string name) { Value = name; } }
public class ExpandAttribute : System.Attribute {}
public class CollapseAttribute : System.Attribute {}
public class HorizontalLayoutAttribute : System.Attribute {}