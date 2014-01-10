// TweakableSolarPanels © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakableEverything
{
	public class ModuleTweakableSolarPanel : PartModule
	{
		protected bool firstUpdate;

		protected ModuleDeployableSolarPanel panelModule;

		protected Animation panelAnimation;

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
		protected bool sunTrackingState;

		protected float baseTrackingSpeed;

		protected Transform pivotTransform;
		protected Vector3 initialPivotVector;
		protected Quaternion initialPivotRotation;

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
			this.firstUpdate = true;

			this.StartOpened = false;
			this.sunTrackingEnabled = true;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.startOpenedState = !this.StartOpened;
			this.sunTrackingState = !this.sunTrackingEnabled;

			this.panelModule = base.part.Modules.OfType<ModuleDeployableSolarPanel>().FirstOrDefault();

			Tools.PostDebugMessage(this,
				"panelModule: " + this.panelModule,
				"storedAnimationTime: " + this.panelModule.storedAnimationTime,
				"storedAnimationSpeed: " + this.panelModule.storedAnimationSpeed,
				"stateString: " + this.panelModule.stateString,
				"currentRotation: " + this.panelModule.currentRotation,
				"originalRotation: " + this.panelModule.originalRotation
			);

			this.pivotTransform = base.part.FindModelTransform(this.panelModule.pivotName);
			this.initialPivotVector = this.pivotTransform.localPosition;
			this.initialPivotRotation = this.pivotTransform.localRotation;

			Tools.PostDebugMessage(this,
				"pivotTransform: " + this.pivotTransform,
				"initial pivotVector: " + this.initialPivotVector,
				"initial pivotRotation:" + this.initialPivotRotation
			);

			this.panelAnimation = this.panelModule.GetComponentInChildren<Animation>();

			Tools.PostDebugMessage(this,
				"panelAnimation: " + this.panelAnimation,
				"animationState: " + this.panelAnimation[this.panelModule.animationName]
			);

			if (HighLogic.LoadedSceneIsEditor && this.panelAnimation != null)
			{
				this.panelModule.currentRotation = this.panelModule.originalRotation;

				if (this.panelAnimation[this.panelModule.animationName])
				{
					this.panelAnimation.wrapMode = WrapMode.ClampForever;
					this.panelAnimation[this.panelAnimationName].enabled = true;
					this.panelAnimation[this.panelAnimationName].speed = 0f;
					this.panelAnimation[this.panelAnimationName].weight = 1f;

					Tools.PostDebugMessage(this,
						"panelAnimation set wrapMode, enabled, speed, and weight."
					);
				}
			}

			/* 
			 * Check whether this panel is a sun tracking panel or not.  Despite its name, ModuleDeployableSolarPanel
			 * is used for all solar panels, even those that don't deply.
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

		public void LateUpdate()
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				if (this.startOpenedState != this.StartOpened && this.panelAnimation != null)
				{
					this.startOpenedState = this.StartOpened;

					if (this.StartOpened)
					{
						Tools.PostDebugMessage(this, "Extending panel.");

						this.panelAnimation[this.panelAnimationName].speed = 1f;
						this.panelAnimation[this.panelAnimationName].normalizedTime = 1f;
						this.panelAnimation.Play(this.panelAnimationName);
						this.panelModule.panelState = ModuleDeployableSolarPanel.panelStates.EXTENDED;
					}
					else
					{
						Tools.PostDebugMessage(this, "Retracting panel.");

						this.panelAnimation[this.panelAnimationName].speed = -1f;
						this.panelAnimation[this.panelAnimationName].normalizedTime = 0f;
						this.panelAnimation.Play(this.panelAnimationName);
						this.panelModule.panelState = ModuleDeployableSolarPanel.panelStates.RETRACTED;
					}

					this.panelModule.storedAnimationTime = this.panelAnimation[this.panelAnimationName].normalizedTime;
					this.panelModule.storedAnimationSpeed = this.panelAnimation[this.panelAnimationName].speed;
					this.panelModule.stateString =
						Enum.GetName(typeof(ModuleDeployableSolarPanel.panelStates), this.panelModule.panelState);
				}
			}

			if (HighLogic.LoadedSceneIsFlight)
			{
				Tools.PostDebugMessage(this,
					"storedAnimationTime: " + this.panelModule.storedAnimationTime,
					"storedAnimationSpeed: " + this.panelModule.storedAnimationSpeed,
					"stateString: " + this.panelModule.stateString,
					"currentRotation: " + this.panelModule.currentRotation
				);
			}

			if (this.panelModule.sunTracking && this.sunTrackingEnabled != this.sunTrackingState)
			{
				this.sunTrackingState = this.sunTrackingEnabled;

				if (this.sunTrackingEnabled)
				{
					this.panelModule.trackingSpeed = this.baseTrackingSpeed;
				}
				else
				{
					this.panelModule.trackingSpeed = 0;
				}
			}
		}
	}
}

