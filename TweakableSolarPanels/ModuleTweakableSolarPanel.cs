﻿// TweakableSolarPanels © 2014 toadicus
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
			Animation solarPanelAnimation;

			base.OnStart(state);

			this.startOpenedState = !this.StartOpened;
			this.sunTrackingState = !this.sunTrackingEnabled;

			this.panelModule = base.part.Modules.OfType<ModuleDeployableSolarPanel>().FirstOrDefault();

			this.pivotTransform = base.part.FindModelTransform(this.panelModule.pivotName);
			this.initialPivotVector = this.pivotTransform.localPosition;
			this.initialPivotRotation = this.pivotTransform.localRotation;

			Tools.PostDebugMessage(this,
				"pivotTransform", this.pivotTransform,
				"initial pivotVector", this.initialPivotVector,
				"initial pivotRotation", this.initialPivotRotation);

			solarPanelAnimation = this.panelModule.GetComponentInChildren<Animation>();

			if (solarPanelAnimation != null)
			{
				if (solarPanelAnimation[this.panelModule.animationName])
				{
					this.panelAnimation[this.panelAnimationName].enabled = true;
					this.panelAnimation[this.panelAnimationName].speed = 0f;
					this.panelAnimation[this.panelAnimationName].weight = 1f;
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
				this.Fields["sunTracking"].guiActive = true;
				this.Fields["sunTracking"].guiActiveEditor = true;
			}
			else
			{
				// ...otherwise, make sure our tracking code and tweakable are inactive.
				this.sunTrackingEnabled = false;
				this.sunTrackingState = false;
				this.Fields["sunTracking"].guiActive = false;
				this.Fields["sunTracking"].guiActiveEditor = false;
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
						this.panelAnimation[this.panelAnimationName].normalizedTime = 1f;
						this.panelModule.panelState = ModuleDeployableSolarPanel.panelStates.EXTENDED;
					}
					else
					{
						this.panelAnimation[this.panelAnimationName].normalizedTime = 0f;
						this.panelModule.panelState = ModuleDeployableSolarPanel.panelStates.RETRACTED;
					}

					this.panelAnimation.Stop();

					this.panelModule.storedAnimationTime = this.panelAnimation[this.panelAnimationName].normalizedTime;
				}
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

