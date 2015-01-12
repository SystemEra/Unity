/*--------------------------+
NestedContext.cs
2014
Jacob Liechty

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using UnityEngine;
using System.Collections;

using strange.extensions.context.api;
using strange.extensions.mediation.api;

namespace strange.extensions.nestedcontext.api
{
	public interface INestedCapableView : IView
	{
		INestedContext overrideContext { get; set; }
	}
}