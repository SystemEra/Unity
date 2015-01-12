/*--------------------------+
INestedContextView.cs
2014
Jacob Liechty

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using System;
using strange.framework.api;
using strange.extensions.context.api;

namespace strange.extensions.nestedcontext.api
{
	public interface INestedContextView : IContextView
	{
		// Parent context
		IContext parentContext { get; }
	}
}

