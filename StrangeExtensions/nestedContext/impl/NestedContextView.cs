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
	public class NestedContextView : FullInspector.BaseBehavior<FullInspector.ModifiedJsonNetSerializer>, INestedContextView, INestedCapableView
	{
		public IContext context{get;set;}
		
		// IView implementation
		public bool requiresContext {get;set;}
		public bool registeredWithContext {get;set;}
		public bool autoRegisterWithContext{ get; set; }

		// The parent context in the nested hierarchy		
		public IContext parentContext { get; set; }

		// Any overriden context not based on hierarchy
		public INestedContext overrideContext { get; set; }
		public virtual Context CreateContext() { return new Context(this); }

		protected virtual void Start()
		{
			if (context == null && UnityEngine.Application.isPlaying)
			{
				CreateContext();
			}
		}

	}
}

