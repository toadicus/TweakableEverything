// Tweakablegimbals © 2014 toadicus
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
	public class ModuleTweakableGimbal : PartModule
	{
		protected ModuleGimbal gimbalModule;

		// Stores our tweaked value for gimbal range.
		[KSPField(isPersistant = true, guiName = "Gimbal Range", guiActiveEditor = true)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = .1f)]
		public float gimbalRange;

		// Stores our tweaked value for control reversal.
		[KSPField(isPersistant = true, guiName = "Control", guiActiveEditor = true, guiActive = true)]
		[UI_Toggle(enabledText = "Reversed", disabledText = "Normal")]
		public bool reverseGimbalControl;
		// Stores the previous state of reverseGimbalControl.
		protected bool reverseControlState;

		public ModuleTweakableGimbal()
		{
			this.gimbalRange = -1;
			this.reverseGimbalControl = false;
		}

		// Runs on PartModule startup.
		public override void OnStart(StartState state)
		{
			// Startup the PartModule stuff first.
			base.OnStart(state);

			// Set our state trackers to the opposite of our states, to force first-run updates.
//			this.startLockedState = !this.startLocked;

			// Fetch the gimbal module from the part.
			this.gimbalModule = base.part.Modules.OfType<ModuleGimbal>().FirstOrDefault();

			// Initialize the gimbal range tweakable and value.
			Tools.InitializeTweakable(
				(UI_FloatRange)this.Fields["gimbalRange"].uiControlEditor,
				ref this.gimbalRange,
				ref this.gimbalModule.gimbalRange,
				PartLoader.getPartInfoByName(base.part.partInfo.name).partPrefab.Modules
					.OfType<ModuleGimbal>()
					.FirstOrDefault()
					.gimbalRange
			);

			// If we're in flight mode...
			if (HighLogic.LoadedSceneIsFlight)
			{
				// ...and if our control state and gimbal range don't match...
				if (
					(this.reverseGimbalControl && this.gimbalRange >= 0) ||
					(!this.reverseGimbalControl && this.gimbalRange < 0)
				)
				{
					// ...toggle the reverse state.
					this.ToggleGimbalFlip();

					// ...and seed our last state.
					this.reverseControlState = this.reverseGimbalControl;
				}
			}
		}

		public void LateUpdate()
		{
			// If we're in flight mode...
			if (HighLogic.LoadedSceneIsFlight)
			{
				// ...and our gimbal control has changed...
				if (this.reverseControlState != this.reverseGimbalControl)
				{
					// ...set the last state
					this.reverseControlState = this.reverseGimbalControl;
					// ...and toggle the reverse state.
					this.ToggleGimbalFlip();
				}
			}
		}

		protected void ToggleGimbalFlip()
		{
			Tools.PostDebugMessage(this.GetType().Name, "Reversing gimbal control.");

			// Literally just negate the gimbal range to change control state.
			this.gimbalModule.gimbalRange = -this.gimbalModule.gimbalRange;
			// Seed this in the persistence file.
			this.gimbalRange = this.gimbalModule.gimbalRange;
		}

		// Puts this in an action group.
		[KSPAction("Toggle Gimbal Control Flip")]
		public void ActionGimbalFlip()
		{
			// Emulate a tweakble toggle.
			this.reverseGimbalControl = !this.reverseGimbalControl;
		}
	}
}

