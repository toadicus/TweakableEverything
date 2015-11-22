// TweakableDockingNode, a TweakableEverything module
//
// ModuleTweakableDockingNode.cs
//
// Copyright © 2014, toadicus
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// 1. Redistributions of source code must retain the above copyright notice,
//    this list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation and/or other
//    materials provided with the distribution.
//
// 3. Neither the name of the copyright holder nor the names of its contributors may be used
//    to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using KSP;
using System;
using System.Collections.Generic;
using ToadicusTools.Extensions;
using UnityEngine;

namespace TweakableEverything
{
	#if DEBUG
	public class ModuleTweakableDockingNode : DebugPartModule
	#else
	public class ModuleTweakableDockingNode : PartModule
	#endif
	{
		/*
		 * Ctor
		 * Build ALL the objects.
		 * */
		public ModuleTweakableDockingNode() : base()
		{
			this.lastOpenState = false;
			this.AlwaysAllowStack = false;

			this.deployAnimationControllerName = string.Empty;
			this.TDNnodeName = string.Empty;

			this.acquireRange = -1;
			this.acquireForce = -1;
			this.acquireTorque = -1;
			this.undockEjectionForce = -1;
			this.minDistanceToReEngage = -1;

			this.maxRollAngle = 90f;
		}

		/*
		 * Fields
		 * */
		// Stores the ModuleDockingNode we're wrapping.
		protected ModuleDockingNode dockingNodeModule;

		// Field that references the animationName of the ModuleAnimateGeneric doing the animating.
		[KSPField(isPersistant = false)]
		public string deployAnimationControllerName;
		// Wrap the animation.
		protected ModuleAnimateGeneric deployAnimation;

		// String containing the name of the AttachNode that we will toggle.
		[KSPField(isPersistant = false)]
		public string TDNnodeName;
		// We will store our attachment node here.
		protected AttachNode attachNode;

		// Some parts need to leave stacking allowed all the time.
		[KSPField(isPersistant = false)]
		public bool AlwaysAllowStack;

		[KSPField(isPersistant = true)]
		public float minRollDotProduct;

		/// <summary>
		/// Maximum roll angle of separation for docking, in degrees.
		/// </summary>
		[KSPField(
			isPersistant = false,
			guiName = "Maximum Roll Angle",
			guiUnits = "°", guiFormat = "F0",
			guiActive = true, guiActiveEditor = true
		)]
		[UI_FloatRange(minValue = 0, maxValue = 90, stepIncrement = 5f, scene = UI_Scene.Editor)]
		public float maxRollAngle;

		public float lastMaxRollAngle;

		// Stores the open/closed state of the shield.
		protected bool lastOpenState;

		/* @subclass
		private bool yieldedDecouple;*/

		[KSPField(isPersistant = true, guiName = "Acquire Range", guiUnits = "m", guiFormat = "F2",
			guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float acquireRange;

		[KSPField(isPersistant = true, guiName = "Acquire Force", guiUnits = "kN", guiFormat = "F2",
			guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float acquireForce;

		[KSPField(isPersistant = true, guiName = "Acquire Torque", guiUnits = "kN-m", guiFormat = "F2",
			guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float acquireTorque;

		[KSPField(isPersistant = true, guiName = "Ejection Force", guiUnits = "kN", guiFormat = "F2",
			guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float undockEjectionForce;

		[KSPField(isPersistant = true, guiName = "Re-engage Distance", guiUnits = "m", guiFormat = "F2",
			guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float minDistanceToReEngage;

		[KSPField(isPersistant = true)]
		protected bool isDecoupled;

		// protected bool stagingEnabled;

		// Gets the base part's fuelCrossFeed value.
		public bool partCrossFeed
		{
			get
			{
				return base.part.fuelCrossFeed;
			}
			set
			{
				base.part.fuelCrossFeed = value;
			}
		}

		/*
		 * Properties
		 * */
		// Get the part attached on the docking end of things.
		protected Part attachedPart
		{
			get
			{
				if (this.attachNode == null)
				{
					return null;
				}

				return this.attachNode.attachedPart;
			}
		}

		// Get the open/closed state of the shield.
		[KSPField(isPersistant = false, guiActiveEditor = true)]
		protected bool IsOpen
		{
			get
			{
				if (this.deployAnimation == null)
				{
					this.LogDebug("deployAnimation is null; open status falling back to true.");
					return true;
				}
				else
				{
					return (this.deployAnimation.animTime >= 1f - float.Epsilon * 2f);
				}
			}
		}

		/*
		 * Methods
		 * */
		// Runs when each new part is started.
		public override void OnStart(StartState st)
		{
			this.dockingNodeModule = (ModuleDockingNode)base.part.Modules["ModuleDockingNode"];

			PartModule needle;

			for (int idx = 0; idx < base.part.Modules.Count; idx++)
			{
				needle = base.part.Modules[idx];

				if (needle is ModuleAnimateGeneric)
				{
					if (((ModuleAnimateGeneric)needle).animationName == this.deployAnimationControllerName)
					{
						this.deployAnimation = (ModuleAnimateGeneric)needle;
						break;
					}
				}
			}

			// If we've loaded a deployAnimationControllerName from the cfg...

			// Start the underlying ModuleDockingNode.
			base.OnStart(st);

			ModuleDockingNode prefabModule = PartLoader.getPartInfoByName(this.part.partInfo.name)
				.partPrefab.getFirstModuleOfType<ModuleDockingNode>();

			TweakableTools.InitializeTweakable<ModuleTweakableDockingNode>(
				this.Fields["acquireRange"].uiControlCurrent(),
				ref this.acquireRange,
				ref this.dockingNodeModule.acquireRange,
				prefabModule.acquireRange
			);

			TweakableTools.InitializeTweakable<ModuleTweakableDockingNode>(
				this.Fields["acquireForce"].uiControlCurrent(),
				ref this.acquireForce,
				ref this.dockingNodeModule.acquireForce,
				prefabModule.acquireForce
			);

			TweakableTools.InitializeTweakable<ModuleTweakableDockingNode>(
				this.Fields["acquireTorque"].uiControlCurrent(),
				ref this.acquireTorque,
				ref this.dockingNodeModule.acquireTorque,
				prefabModule.acquireForce
			);

			TweakableTools.InitializeTweakable<ModuleTweakableDockingNode>(
				this.Fields["undockEjectionForce"].uiControlCurrent(),
				ref this.undockEjectionForce,
				ref this.dockingNodeModule.undockEjectionForce,
				prefabModule.undockEjectionForce
			);

			TweakableTools.InitializeTweakable<ModuleTweakableDockingNode>(
				this.Fields["minDistanceToReEngage"].uiControlCurrent(),
				ref this.minDistanceToReEngage,
				ref this.dockingNodeModule.minDistanceToReEngage,
				prefabModule.minDistanceToReEngage
			);

			this.Fields["maxRollAngle"].uiControlFlight.controlEnabled = false;

			this.maxRollAngle = Mathf.Acos(this.minRollDotProduct) * 180f / Mathf.PI;
			this.dockingNodeModule.acquireMinRollDot = this.minRollDotProduct * this.minRollDotProduct;
			this.dockingNodeModule.captureMinRollDot = this.minRollDotProduct;

			this.lastMaxRollAngle = this.maxRollAngle;

			// If we have a tweakable AttachNode, use it.
			if (this.TDNnodeName != string.Empty)
			{
				this.attachNode = base.part.findAttachNode(this.TDNnodeName);
			}

			base.part.attachRules.allowStack = this.IsOpen | this.AlwaysAllowStack;

			this.dockingNodeModule.Events["EnableXFeed"].guiActive = false;
			this.dockingNodeModule.Events["DisableXFeed"].guiActive = false;

			this.dockingNodeModule.Events["EnableXFeed"].guiActiveEditor = false;
			this.dockingNodeModule.Events["DisableXFeed"].guiActiveEditor = false;

			this.dockingNodeModule.Events["EnableXFeed"].active = false;
			this.dockingNodeModule.Events["DisableXFeed"].active = false;

			/* @subclass
			ModuleStagingToggle stagingToggleModule;

			if (this.part.tryGetFirstModuleOfType<ModuleStagingToggle>(out stagingToggleModule))
			{
				stagingToggleModule.OnToggle += new ModuleStagingToggle.ToggleEventHandler(this.OnStagingToggle);
			}
			*/

			// Yay debugging!
			this.LogDebug(
				"{0}: Started with assembly version {4}." +
				"\n\tdeployAnimationModule={1}, attachNode={2}, TDNnodeName={3}, attachedPart={5}, fuelCrossFeed={6}",
				this.GetType().Name,
				this.deployAnimation,
				this.attachNode,
				this.TDNnodeName,
				this.GetType().Assembly.GetName().Version,
				this.attachedPart
			);
		}

		// Runs every LateUpdate, because that's how Unity rolls.
		// We're running at LateUpdate to avoid hiding Update, since ModuleDockingNode's Update is private and we
		// can't call it.
		public void FixedUpdate()
		{
			// If we're in the Editor...
			if (HighLogic.LoadedSceneIsEditor)
			{
				// ...and if we have a deployAnimationModule...
				if (this.deployAnimation != null)
				{
					// If the Opened state of the port has changed since last update and we have an attachNode...
					if (this.attachNode != null && this.IsOpen != this.lastOpenState)
					{
						// ...set the last state to the current state
						this.lastOpenState = this.IsOpen;

						// ...and switch allowStack.
						base.part.attachRules.allowStack = this.IsOpen | this.AlwaysAllowStack;

						// Yay debugging!
						this.LogDebug(
							"{0}: IsOpen changed to: {1}, part contains node: {2}, allowStack: {3}",
							this.GetType().Name,
							this.IsOpen,
							base.part.attachNodes.Contains(this.attachNode),
							base.part.attachRules.allowStack
						);
					}

					// ...if the port is closed and the attachNode icon is active...
					if (this.attachNode != null && this.attachNode.icon != null)
					{
						this.attachNode.icon.SetActive(this.IsOpen);
					}
				}

				if (this.maxRollAngle != this.lastMaxRollAngle)
				{
					this.minRollDotProduct = Mathf.Cos(this.maxRollAngle * Mathf.PI / 180f);
					this.dockingNodeModule.acquireMinRollDot = this.minRollDotProduct * this.minRollDotProduct;
					this.dockingNodeModule.captureMinRollDot = this.minRollDotProduct;
					this.lastMaxRollAngle = this.maxRollAngle;
				}
			}

			// If we are in flight...
			if (HighLogic.LoadedSceneIsFlight)
			{
				// ...and if we have a deploy animation module and are ready...
				if (
					this.deployAnimation != null &&
					this.dockingNodeModule.state == "Ready"
				)
				{
					// ...and if we have an attached part...
					if (this.attachedPart != null)
					{
						// ...disable the deploy animation.
						this.deployAnimation.Events["Toggle"].active = false;
					}
					// ...otherwise...
					else
					{
						// ...enable the deploy animation.
						this.deployAnimation.Events["Toggle"].active = true;
					}
				}
			}
		}

		/*
		 * Chopping out OnActive entirely pending reimplementation as a subclass.
		public override void OnActive()
		{
			yieldedDecouple = false;

			this.LogDebug("this.attachedPart={0}", this.attachedPart == null ? "null" : this.attachedPart.partInfo.title);

			ModuleTweakableDockingNode attachedNode;
			if (
				this.attachedPart != null &&
				PartIsStagingDockingPort(this.attachedPart, out attachedNode)
			)
			{
				this.LogDebug("Attached part is staging docking port");

				if (!attachedNode.yieldedDecouple)
				{
					this.LogDebug("Attached part has not yielded, yielding and returning.");
					this.yieldedDecouple = true;
					return;
				}
			}

			this.LogDebug("OnActive");

			base.OnActive();

			if (this.stagingEnabled && this.dockingNodeModule != null)
			{
				switch (this.dockingNodeModule.state.ToLower())
				{
					case "preattached":
						this.dockingNodeModule.Decouple();
						break;
					case "docked (dockee)":
					case "docked (docker)":
					case "docked (same vessel)":
						this.dockingNodeModule.Undock();
						break;
					default:
						break;
				}
			}
		}
		*/

		[KSPAction("Control from Here")]
		public void MakeReferenceTransformAction(KSPActionParam param)
		{
			if (this.dockingNodeModule.Events["MakeReferenceTransform"].active)
			{
				this.dockingNodeModule.MakeReferenceTransform();
			}
		}

		/*
		 * @subclass -- Chopping this out pending rewrite; should happen with Stock's logic.
		protected void OnStagingToggle(object sender, ModuleStagingToggle.BoolArg arg)
		{
			this.LogDebug("OnStagingToggle called.");
			this.stagingEnabled = arg.Value;
		}*/
	}
}
