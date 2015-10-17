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
using System.Reflection;
using ToadicusTools.Extensions;
using UnityEngine;

namespace TweakableEverything
{
	#if DEBUG
	public class ModuleTweakableSolarPanel : DebugPartModule
	#else
	public class ModuleTweakableDeployablePanel : PartModule
	#endif
	{
		private FieldInfo animationNameField;
		private FieldInfo originalRotationField;
		private FieldInfo currentRotationField;
		private FieldInfo sunTrackingField;
		private FieldInfo trackingSpeedField;
		private FieldInfo storedAnimationTimeField;
		private FieldInfo panelStateField;
		private FieldInfo statusField;
		private FieldInfo stateStringField;

		[KSPField(isPersistant = false)]
		public string moduleType;

		// Tweakable property to determine whether the solar panel should start opened or closed.
		[KSPField(guiName = "Start", isPersistant = true, guiActiveEditor = true, guiActive = false)]
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
		protected PartModule panelModule;
		// Stores the solar panel animation we're clobbering.
		protected ToadicusTools.AnimationWrapper panelAnimation;

		private object PanelStateExtended
		{
			get
			{
				switch (this.panelModule.GetType().Name)
				{
					case "ModuleDeployableSolarPanel":
						return ModuleDeployableSolarPanel.panelStates.EXTENDED;
					case "ModuleDeployableRadiator":
						return ModuleDeployableRadiator.panelStates.EXTENDED;
					default:
						throw new NotImplementedException();
				}
			}
		}

		private object PanelStateRetracted
		{
			get
			{
				switch (this.panelModule.GetType().Name)
				{
					case "ModuleDeployableSolarPanel":
						return ModuleDeployableSolarPanel.panelStates.RETRACTED;
					case "ModuleDeployableRadiator":
						return ModuleDeployableRadiator.panelStates.RETRACTED;
					default:
						throw new NotImplementedException();
				}
			}
		}

		// Construct ALL the objects.
		public ModuleTweakableDeployablePanel()
		{
			// These defaults reflect stock behavior.
			this.StartOpened = false;
			this.sunTrackingEnabled = true;
			this.moduleType = "ModuleDeployableSolarPanel";
		}

		// Runs on PartModule startup.
		public override void OnStart(StartState state)
		{
			// Startup the PartModule stuff first.
			base.OnStart(state);

			// Fetch the solar panel module from the part.
			if (this.part.tryGetFirstModuleByName(this.moduleType, out this.panelModule))
			{// Set our state trackers to the opposite of our states, to force first-run updates.
				this.startOpenedState = !this.StartOpened;
				this.sunTrackingState = !this.sunTrackingEnabled;
/*
				// Yay debugging!
				this.LogDebug(
					"panelModule: " + this.panelModule,
					"storedAnimationTime: " + this.panelModule.storedAnimationTime,
					"storedAnimationSpeed: " + this.panelModule.storedAnimationSpeed,
					"stateString: " + this.panelModule.stateString,
					"currentRotation: " + this.panelModule.currentRotation,
					"originalRotation: " + this.panelModule.originalRotation
				);
*/
				// Fetch the UnityEngine.Animation object from the solar panel module.
				Animation anim = this.panelModule.GetComponentInChildren<Animation>();

				// If the animation is null, bailout.
				if (anim == null)
				{
					this.LogDebug("No animation objects found in panel module; bailing out.");
					return;
				}

				if (animationNameField == null)
				{
					animationNameField = this.panelModule.GetType().GetField("animationName");
				}

				// Build an ToadicusTools.
				this.panelAnimation = new ToadicusTools.AnimationWrapper(anim, (string)animationNameField.GetValue(this.panelModule), ToadicusTools.PlayDirection.Forward);

				// Yay debugging!
				this.LogDebug("panelAnimation: " + this.panelAnimation);

				// If we are in the editor and have an animation...
				if (HighLogic.LoadedSceneIsEditor && this.panelAnimation != null)
				{
					// ...pre-set the panel's currentRotation...

					if (originalRotationField == null)
					{
						originalRotationField = this.panelModule.GetType().GetField("originalRotation");
					}

					if (currentRotationField == null)
					{
						currentRotationField = this.panelModule.GetType().GetField("currentRotation");
					}

					currentRotationField.SetValue(this.panelModule, originalRotationField.GetValue(this.panelModule));
				}

				/* 
				 * Checks whether this panel is a sun tracking panel or not.  Despite its name, ModuleDeployableSolarPanel
				 * is used for all (most?) solar panels, even those that don't deploy or rotate.
				 * */
				// If the panel is sun tracking panel...
				if (sunTrackingField == null)
				{
					sunTrackingField = this.panelModule.GetType().GetField("sunTracking");
				}

				bool moduleIsSunTracking = (bool)sunTrackingField.GetValue(this.panelModule);

				if (moduleIsSunTracking)
				{
					if (trackingSpeedField == null)
					{
						trackingSpeedField = this.panelModule.GetType().GetField("trackingSpeed");
					}

					// ...go fetch the tracking speed and make sure our tracking tweakable is active.
					this.baseTrackingSpeed = (float)trackingSpeedField.GetValue(this.panelModule);
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
				this.LogDebug("panel module is null, bailing out.");
				return;
			}

			// If we're in the editor...
			if (HighLogic.LoadedSceneIsEditor && this.panelAnimation != null)
			{
				// ...if StartOpened has changed and we have an Animation...
				if (this.startOpenedState != this.StartOpened)
				{
					this.LogDebug("Start-opened state changed, skipping animation.");

					// ...refresh startOpenedState
					this.startOpenedState = this.StartOpened;

					if (storedAnimationTimeField == null)
					{
						storedAnimationTimeField = this.panelModule.GetType().GetField("storedAnimationTime");
					}

					if (panelStateField == null)
					{
						panelStateField = this.panelModule.GetType().GetField("panelState");
					}

					if (statusField == null)
					{
						statusField = this.panelModule.GetType().GetField("status");
					}

					// ...and if we are starting opened...
					if (this.StartOpened)
					{
						// Yay debugging!
						this.LogDebug("Extending panel.");

						// ...move the animation to the end with a "forward" play speed.
						this.panelAnimation.SkipTo(ToadicusTools.PlayPosition.End);

						storedAnimationTimeField.SetValue(this.panelModule, 1f);

						// ...flag the panel as extended.
						panelStateField.SetValue(this.panelModule, this.PanelStateExtended);
						statusField.SetValue(this.panelModule, "Extended");
					}
					// ...otherwise, we are starting closed...
					else
					{
						// Yay debugging!
						this.LogDebug("Retracting panel.");

						// ...move the animation to the beginning with a "backward" play speed.
						this.panelAnimation.SkipTo(ToadicusTools.PlayPosition.Beginning);
						storedAnimationTimeField.SetValue(this.panelModule, 0f);

						// ...flag the panel as retracted.
						panelStateField.SetValue(this.panelModule, this.PanelStateRetracted);
						statusField.SetValue(this.panelModule, "Retracted");
					}

					// ...play the animation, because it's so very pretty.

					// ...update the persistence data for the solar panel accordingly.

					if (stateStringField == null)
					{
						stateStringField = this.panelModule.GetType().GetField("stateString");
					}

					stateStringField.SetValue(
						this.panelModule,
						Enum.GetName(
							typeof(ModuleDeployableSolarPanel.panelStates),
							panelStateField.GetValue(this.panelModule)
						)
					);
				}
			}

			// If this panel is tracking-enabled and our sun tracking state has changed...
			if (((bool)sunTrackingField.GetValue(this.panelModule)) && this.sunTrackingEnabled != this.sunTrackingState)
			{
				this.LogDebug("Sun tracking toggled; updating.");

				// ..refresh our sunTrackingState
				this.sunTrackingState = this.sunTrackingEnabled;

				// ...and if we're tracking the sun...
				if (this.sunTrackingEnabled)
				{
					this.LogDebug("Setting panel module's sunTrackingSpeed to {0}", this.baseTrackingSpeed);
					// ...ensure the panel's tracking speed is set per it's original value
					trackingSpeedField.SetValue(this.panelModule, this.baseTrackingSpeed);
				}
				// ...otherwise, we're not tracking the sun...
				else
				{
					this.LogDebug("Setting panel module's sunTrackingSpeed to 0");
					// ...so set the panel's tracking speed to zero
					trackingSpeedField.SetValue(this.panelModule, 0);
				}
			}
		}
	}
}
