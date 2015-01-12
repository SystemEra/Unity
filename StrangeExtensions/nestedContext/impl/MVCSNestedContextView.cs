/*--------------------------+
NestedContext.cs
2014
Jacob Liechty

Copyright System Era Softworks 2014
+-------------------------------------------------------------------------*/

using strange.extensions.nestedcontext.api;
using strange.extensions.context.api;
using strange.extensions.context.impl;
using strange.extensions.mediation.api;
using strange.framework.impl;

namespace strange.extensions.nestedcontext.impl
{
	public class MVCSNestedContextView : NestedContextView
	{
		protected override void Start()
		{
			base.Start();
			(context as MVCSNestedContext).mediationBinder.Trigger(MediationEvent.AWAKE, this.GetComponent(typeof(IContextView)) as IContextView);
		}

	}
}

