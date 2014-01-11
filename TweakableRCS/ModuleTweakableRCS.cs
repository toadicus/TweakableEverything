// TweakableRCS © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakableRCS
{
	public class ModuleTweakableRCS : PartModule
	{
		protected ModuleRCS RCSModule;

		// Stores whether the RCS block should start enabled or not.
		[KSPField(isPersistant = true, guiName = "Thruster", guiActive = false, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
		public bool startEnabled;
		// Stores the last state of startEnabled so we can tell if it's changed.
		protected bool startEnabledState;

		// Stores our thrust limiter value for the RCS block.
		[KSPField(isPersistant = true, guiName = "Thrust Limiter", guiActiveEditor = true, guiActive = true)]
		[UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 5f)]
		public float thrustLimit;

		protected float baseThrusterPower;

		public ModuleTweakableRCS()
		{
			this.startEnabled = true;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.RCSModule = base.part.Modules.OfType<ModuleRCS>().FirstOrDefault();

			this.startEnabledState = !this.startEnabled;

			this.baseThrusterPower = this.RCSModule.thrusterPower;
		}

		// Runs late in the update cycle
		public void LateUpdate()
		{
			// If we're in the editor...
			if (HighLogic.LoadedSceneIsEditor)
			{
				// ...and if our startEnabled state has changed...
				if (this.startEnabled != this.startEnabledState)
				{
					// ...refresh startEnabledState
					this.startEnabledState = this.startEnabled;

					// ...and if we're starting enabled...
					if (this.startEnabled)
					{
						// ...set the reaction wheel module to active
						this.RCSModule.isEnabled = true;
					}
					// ...otherwise, we're starting disabled...
					else
					{
						// ...set the reaction wheel module to disabled
						this.RCSModule.isEnabled = false;
					}
				}
			}

			if (HighLogic.LoadedSceneIsFlight)
			{
				this.RCSModule.thrusterPower = this.baseThrusterPower * this.thrustLimit / 100f;

				foreach (FXGroup fx in this.RCSModule.thrusterFX)
				{
					fx.Power *= this.thrustLimit / 100f;
				}
			}
		}
	}
}

