using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakableEverything
{
	public class ModuleTweakableReactionWheel : PartModule
	{
		// Stores the reaction wheel module we're tweaking.
		protected ModuleReactionWheel reactionWheelModule;

		// Stores whether or not the wheel will start enabled.
		[KSPField(isPersistant = true, guiName = "Reaction Wheels", guiActive = false, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
		public bool startEnabled;
		// Stores the last state of startEnabled so we can tell if it's changed.
		protected bool startEnabledState;

		// Stores our tweaked value for roll torque.
		[KSPField(isPersistant = true, guiName = "Roll Torque (kN-m)", guiActiveEditor = true)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float RollTorque;

		// Stores our tweaked value for pitch torque.
		[KSPField(isPersistant = true, guiName = "Pitch Torque (kN-m)", guiActiveEditor = true)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float PitchTorque;

		// Stores our tweaked value for yaw torque.
		[KSPField(isPersistant = true, guiName = "Yaw Torque (kN-m)", guiActiveEditor = true)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float YawTorque;

		// Construct ALL the objects.
		public ModuleTweakableReactionWheel()
		{
			// Default to starting enabled, per Squad's behavior.
			this.startEnabled = true;

			// -1 will indicate an uninitialized value.
			this.RollTorque = -1;
			this.PitchTorque = -1;
			this.YawTorque = -1;
		}

		// Runs on start.
		public override void OnStart(StartState state)
		{
			// Start up the underlying PartModule stuff.
			base.OnStart(state);

			// Seed the startEnabledState to the opposite of startEnabled to force first-update processing.
			this.startEnabledState = !this.startEnabled;

			// Fetch the reaction wheel module.
			this.reactionWheelModule = base.part.Modules.OfType<ModuleReactionWheel>().FirstOrDefault();
			ModuleReactionWheel prefabModule = PartLoader.getPartInfoByName(this.part.partInfo.name).partPrefab.Modules
				.OfType<ModuleReactionWheel>()
				.First();

			Tools.InitializeTweakable<ModuleTweakableReactionWheel>(
				(UI_FloatRange)this.Fields["RollTorque"].uiControlCurrent(),
				ref this.RollTorque,
				ref this.reactionWheelModule.RollTorque,
				prefabModule.RollTorque
			);

			Tools.InitializeTweakable<ModuleTweakableReactionWheel>(
				(UI_FloatRange)this.Fields["PitchTorque"].uiControlCurrent(),
				ref this.PitchTorque,
				ref this.reactionWheelModule.PitchTorque,
				prefabModule.PitchTorque
			);

			Tools.InitializeTweakable<ModuleTweakableReactionWheel>(
				(UI_FloatRange)this.Fields["YawTorque"].uiControlCurrent(),
				ref this.YawTorque,
				ref this.reactionWheelModule.YawTorque,
				prefabModule.YawTorque
			);
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
						this.reactionWheelModule.State = ModuleReactionWheel.WheelState.Active;
					}
					// ...otherwise, we're starting disabled...
					else
					{
						// ...set the reaction wheel module to disabled
						this.reactionWheelModule.State = ModuleReactionWheel.WheelState.Disabled;
					}
				}
			}
		}
	}
}