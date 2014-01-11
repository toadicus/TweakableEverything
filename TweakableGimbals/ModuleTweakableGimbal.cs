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

		// Stores our tweaked value for roll torque.
		[KSPField(isPersistant = true, guiName = "Gimbal Range", guiActiveEditor = true)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float gimbalRange;

		public ModuleTweakableGimbal()
		{
//			this.startLocked = false;

			this.gimbalRange = -1;
		}

		// Runs on PartModule startup.
		public override void OnStart(StartState state)
		{
			// Startup the PartModule stuff first.
			base.OnStart(state);

			// Set our state trackers to the opposite of our states, to force first-run updates.
//			this.startLockedState = !this.startLocked;

			// Fetch the solar gimbal module from the part.
			this.gimbalModule = base.part.Modules.OfType<ModuleGimbal>().FirstOrDefault();

			Tools.InitializeTweakable(
				(UI_FloatRange)this.Fields["gimbalRange"].uiControlEditor,
				ref this.gimbalRange,
				ref this.gimbalModule.gimbalRange
			);
		}
	}
}

