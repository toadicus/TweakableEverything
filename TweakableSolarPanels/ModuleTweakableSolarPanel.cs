// TweakableSolarPanels, a TweakableEverything module
//
// ModuleTweakableSolarPanel.cs
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
using ToadicusTools;
using UnityEngine;

namespace TweakableEverything
{
	#if DEBUG
	public class ModuleTweakableSolarPanel : DebugPartModule
	#else
	public class ModuleTweakableSolarPanel : PartModule
	#endif
	{
		// Tweakable property to determine whether the solar panel should start opened or closed.
		[KSPField(guiName = "Start", isPersistant = true, guiActiveEditor = true)]
		[UI_Toggle(disabledText = "Retracted", enabledText = "Extended")]
		public bool StartOpened;
		// Save the state here so we can tell if StartOpened has changed.
		protected bool startOpenedState;

		// Tweakable property to determine whether the solar panel should track the sun or not.
		// Tweakable in flight.
		[KSPField(guiName = "Sun Tracking", isPersistant = true, guiActiveEditor = true, guiActive = true)]
		[UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
		public bool sunTrackingEnabled;
		// Save the state here so we can tell if sunTrackingEnabled has changed.
		protected bool sunTrackingState;

		// Stores the default sun tracking speed from the panel module.
		protected float baseTrackingSpeed;

		// Stores the solar panel module we're tweaking
		protected ModuleDeployableSolarPanel panelModule;
		// Stores the solar panel animation we're clobbering.
		protected Animation panelAnimation;

		// Gets the animationName field from the panel module.
		protected string panelAnimationName
		{
			get
			{
				return this.panelModule.animationName;
			}
		}

		// Construct ALL the objects.
		public ModuleTweakableSolarPanel()
		{
			// These defaults reflect stock behavior.
			this.StartOpened = false;
			this.sunTrackingEnabled = true;
		}

		// Runs on PartModule startup.
		public override void OnStart(StartState state)
		{
			// Startup the PartModule stuff first.
			base.OnStart(state);

			// Fetch the solar panel module from the part.
			if (this.part.tryGetFirstModuleOfType(out this.panelModule))
			{// Set our state trackers to the opposite of our states, to force first-run updates.
				this.startOpenedState = !this.StartOpened;
				this.sunTrackingState = !this.sunTrackingEnabled;

				// Yay debugging!
				Tools.PostDebugMessage(this,
					"panelModule: " + this.panelModule,
					"storedAnimationTime: " + this.panelModule.storedAnimationTime,
					"storedAnimationSpeed: " + this.panelModule.storedAnimationSpeed,
					"stateString: " + this.panelModule.stateString,
					"currentRotation: " + this.panelModule.currentRotation,
					"originalRotation: " + this.panelModule.originalRotation
				);

				// Fetch the UnityEngine.Animation object from the solar panel module.
				this.panelAnimation = this.panelModule.GetComponentInChildren<Animation>();

				// Yay debugging!
				Tools.PostDebugMessage(this,
					"panelAnimation: " + this.panelAnimation,
					"animationState: " + this.panelAnimation[this.panelModule.animationName]
				);

				// If we are in the editor and have an animation...
				if (HighLogic.LoadedSceneIsEditor && this.panelAnimation != null)
				{
					// ...pre-set the panel's currentRotation...
					this.panelModule.currentRotation = this.panelModule.originalRotation;

					// ...and if our animation has an AnimationState named in the panel module...
					if (this.panelAnimation[this.panelModule.animationName])
					{
						// ...Set up the AnimationState for later use.
						this.panelAnimation.wrapMode = WrapMode.ClampForever;
						this.panelAnimation[this.panelAnimationName].enabled = true;
						this.panelAnimation[this.panelAnimationName].speed = 0f;
						this.panelAnimation[this.panelAnimationName].weight = 1f;

						// Yay debuggin!
						Tools.PostDebugMessage(this,
							"panelAnimation set wrapMode, enabled, speed, and weight."
						);
					}
				}

				/* 
			 * Checks whether this panel is a sun tracking panel or not.  Despite its name, ModuleDeployableSolarPanel
			 * is used for all (most?) solar panels, even those that don't deploy or rotate.
			 * */
				// If the panel is sun tracking panel...
				if (this.panelModule.sunTracking)
				{
					// ...go fetch the tracking speed and make sure our tracking tweakable is active.
					this.baseTrackingSpeed = this.panelModule.trackingSpeed;
					this.Fields["sunTrackingEnabled"].guiActive = true;
					this.Fields["sunTrackingEnabled"].guiActiveEditor = true;
				}
				else
				{
					// ...otherwise, make sure our tracking code and tweakable are inactive.
					this.sunTrackingEnabled = false;
					this.sunTrackingState = false;
					this.Fields["sunTrackingEnabled"].guiActive = false;
					this.Fields["sunTrackingEnabled"].guiActiveEditor = false;
				}
			}
		}

		// Runs at LateUpdate.  Why?  Because.
		public void LateUpdate()
		{
			// Do nothing if the panel module is null
			if (this.panelModule == null)
			{
				return;
			}

			// If we're in the editor...
			if (HighLogic.LoadedSceneIsEditor && this.panelAnimation != null)
			{
				// ...if StartOpened has changed and we have an Animation...
				if (this.startOpenedState != this.StartOpened)
				{
					// ...refresh startOpenedState
					this.startOpenedState = this.StartOpened;

					// ...and if we are starting opened...
					if (this.StartOpened)
					{
						// Yay debugging!
						Tools.PostDebugMessage(this, "Extending panel.");

						// ...move the animation to the end with a "forward" play speed.
						this.panelAnimation[this.panelAnimationName].speed = 1f;
						this.panelAnimation[this.panelAnimationName].normalizedTime = 1f;

						// ...flag the panel as extended.
						this.panelModule.panelState = ModuleDeployableSolarPanel.panelStates.EXTENDED;
					}
					// ...otherwise, we are starting closed...
					else
					{
						// Yay debugging!
						Tools.PostDebugMessage(this, "Retracting panel.");

						// ...move the animation to the beginning with a "backward" play speed.
						this.panelAnimation[this.panelAnimationName].speed = -1f;
						this.panelAnimation[this.panelAnimationName].normalizedTime = 0f;

						// ...flag the panel as retracted.
						this.panelModule.panelState = ModuleDeployableSolarPanel.panelStates.RETRACTED;
					}

					// ...play the animation, because it's so very pretty.
					this.panelAnimation.Play(this.panelAnimationName);

					// ...update the persistence data for the solar panel accordingly.
					this.panelModule.storedAnimationTime = this.panelAnimation[this.panelAnimationName].normalizedTime;
					this.panelModule.storedAnimationSpeed = this.panelAnimation[this.panelAnimationName].speed;
					this.panelModule.stateString =
						Enum.GetName(typeof(ModuleDeployableSolarPanel.panelStates), this.panelModule.panelState);
				}
			}

			// If this panel is tracking-enabled and our sun tracking state has changed...
			if (this.panelModule.sunTracking && this.sunTrackingEnabled != this.sunTrackingState)
			{
				// ..refresh our sunTrackingState
				this.sunTrackingState = this.sunTrackingEnabled;

				// ...and if we're tracking the sun...
				if (this.sunTrackingEnabled)
				{
					// ...ensure the panel's tracking speed is set per it's original value
					this.panelModule.trackingSpeed = this.baseTrackingSpeed;
				}
				// ...otherwise, we're not tracking the sun...
				else
				{
					// ...so set the panel's tracking speed to zero
					this.panelModule.trackingSpeed = 0;
				}
			}
		}
	}
}

