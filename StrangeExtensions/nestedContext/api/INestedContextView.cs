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

