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

 * The Strange IOC Framework is hosted at https://github.com/strangeioc/strangeioc. 
 * Code contained here may contain modified code which is subject to the license 
 * at this link.
 */

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

