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

using UnityEngine;
using strange.extensions.nestedcontext.api;
using strange.extensions.context.api;
using strange.extensions.context.impl;
using strange.extensions.mediation.api;
using strange.extensions.mediation.impl;

namespace strange.extensions.nestedcontext.impl
{
	public class NestedCapableView : FullInspector.BaseBehavior<FullInspector.ModifiedJsonNetSerializer>, INestedCapableView
	{
		/// Leave this value true most of the time. If for some reason you want
		/// a view to exist outside a context you can set it to false. The only
		/// difference is whether an error gets generated.
		private bool _requiresContext = true;
		public bool requiresContext
		{
			get
			{
				return _requiresContext;
			}
			set
			{
				_requiresContext = value;
			}
		}

		/// A flag for allowing the View to register with the Context
		/// In general you can ignore this. But some developers have asked for a way of disabling
		///  View registration with a checkbox from Unity, so here it is.
		/// If you want to expose this capability either
		/// (1) uncomment the commented-out line immediately below, or
		/// (2) subclass View and override the autoRegisterWithContext method using your own custom (public) field.
		//[SerializeField]
		protected bool registerWithContext = true;
		virtual public bool autoRegisterWithContext
		{
			get { return registerWithContext; }
			set { registerWithContext = value; }
		}

		public bool registeredWithContext { get; set; }

		[Inject] public INestedContext context { get; set; }
		public INestedContext overrideContext { get; set; }

		/// A MonoBehaviour Start handler
		/// If the View is not yet registered with the Context, it will 
		/// attempt to connect again at this moment.
		protected virtual void Start()
		{
			if (Application.isPlaying && autoRegisterWithContext && !registeredWithContext)
				bubbleToContext(this, true, true);
		}

		/// A MonoBehaviour OnDestroy handler
		/// The View will inform the Context that it is about to be
		/// destroyed.
		protected virtual void OnDestroy()
		{
			if (Application.isPlaying)
				bubbleToContext(this, false, false);
		}

		protected static IContext GetBubbledContext(MonoBehaviour view, bool requireContext, bool createParentContext)
		{
			const int LOOP_MAX = 100;
			int loopLimiter = 0;
			Transform trans = view.transform;
			while (trans != null && loopLimiter < LOOP_MAX)
			{
				loopLimiter++;
				if (trans.gameObject.GetComponent(typeof(IContextView)) != null)
				{
					NestedContextView contextView = trans.gameObject.GetComponent<NestedContextView>() as NestedContextView;
					if (contextView != null && contextView != view)
					{
						if (createParentContext && contextView.context == null)
						{
							contextView.CreateContext();
						}

						if (contextView.context != null)
						{
							return contextView.context;
						}
					}
				}
				trans = trans.parent;
			}

			if (requireContext)
			{
				//last ditch. If there's a Context anywhere, we'll use it!
				return Context.firstContext;
			}

			return null;
		}

		public static IContext GetParentContext(strange.extensions.nestedcontext.api.INestedCapableView view, MonoBehaviour viewBehavior)
		{
			return GetParentContext(view, viewBehavior, true, true);
		}

		public static IContext GetParentContext(strange.extensions.nestedcontext.api.INestedCapableView view, MonoBehaviour viewBehavior, bool toAdd, bool finalTry)
		{
			return view.overrideContext != null ? view.overrideContext : GetBubbledContext(viewBehavior, toAdd, finalTry);
		}

		/// Recurses through Transform.parent to find the GameObject to which ContextView is attached
		/// Has a loop limit of 100 levels.
		/// By default, raises an Exception if no Context is found.
		virtual protected void bubbleToContext(NestedCapableView view, bool toAdd, bool finalTry)
		{
			IContext bubbledContext = GetParentContext(view, view, requiresContext && finalTry, toAdd);

			if (bubbledContext != null)
			{
				if (toAdd)
				{
					registeredWithContext = true;
					bubbledContext.AddView(view);
				}
				else
				{
					bubbledContext.RemoveView(view);
				}
			}
			else if (requiresContext && finalTry)
			{
				string msg = "A view was added with no context. Views must be added into the hierarchy of their ContextView lest all hell break loose.";
				msg += "\nView: " + view.ToString();
				throw new MediationException(msg, MediationExceptionType.NO_CONTEXT);
			}
		}
	}
}