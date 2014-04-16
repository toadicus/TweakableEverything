// TweakableDecouplers, a TweakableEverything module
//
// ModuleTweakableDecouple.cs
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
using System.Linq;
using UnityEngine;

namespace TweakableEverything
{
	public class ModuleTweakableDecouple : PartModule
	{
		// Stores the name of the decoupler module, since sometimes it is different.
		[KSPField(isPersistant = false)]
		public string decouplerModuleName;

		// Stores the decoupler module
		protected PartModule decoupleModule;

		// Stores the tweaked ejectionForce for clobbering the value in the real decouplerModule.
		[KSPField(isPersistant = true, guiName = "Ejection Force (kN)", guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float ejectionForce;

		// Stores the configurable multiplier for the lower bound on the FloatRange
		[KSPField(isPersistant = false)]
		public float lowerMult;
		// Stores the configurable multiplier for the upper bound on the FloatRange
		[KSPField(isPersistant = false)]
		public float upperMult;

		// Store the tweaked staging enabled toggle for clobbering the value in the real decouplerModule.
		[KSPField(isPersistant = true, guiName = "Staging", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
		public bool stagingEnabled;
		// Stores its last state so we can only run when things change.
		protected bool stagingState;

		// Construct ALL the objects.
		public ModuleTweakableDecouple() : base()
		{
			// We'll use -1 to mean "uninitialized" for purposes of defaulting to the base module's value
			this.ejectionForce = -1;

			// Set the default multipler bounds.
			this.lowerMult = 0f;
			this.upperMult = 2f;

			// Default stagingEnabled to true for consistency with stock behavior.
			this.stagingEnabled = true;

			// Default to ModuleDecouple in case we get an older .cfg file.
			this.decouplerModuleName = "ModuleDecouple";
		}

		// Runs on start.  Seriously.
		public override void OnStart(StartState state)
		{
			AvailablePart partInfo;
			PartModule prefabModule;

			// Start up any underlying PartModule stuff
			base.OnStart(state);

			// Fetch the generic decoupler module from the part by module name.
			this.decoupleModule = base.part.Modules
				.OfType<PartModule>()
				.FirstOrDefault(m => m.moduleName == this.decouplerModuleName);

			// Fetch the prefab for harvesting the actual stock value.  This is done to prevent copies in the editor
			// from inheriting a tweaked value as their "center".
			partInfo = PartLoader.getPartInfoByName(base.part.partInfo.name);
			// Fetch the prefab module for the above purpose.
			prefabModule = partInfo.partPrefab.Modules
				.OfType<PartModule>()
				.FirstOrDefault(m => m.moduleName == this.decouplerModuleName);

			// Fetch the ejectionForce field from our generic decoupler module.
			float remoteEjectionForce =
				this.decoupleModule.Fields["ejectionForce"].GetValue<float>(this.decoupleModule);

			// Build initialize the FloatRange with upper and lower bounds from the cfg file, center value from the
			// prefab, and current value from persistence
			Tools.InitializeTweakable<ModuleTweakableDecouple>(
				(UI_FloatRange)this.Fields["ejectionForce"].uiControlCurrent(),
				ref this.ejectionForce,
				ref remoteEjectionForce,
				prefabModule.Fields["ejectionForce"].GetValue<float>(prefabModule),
				this.lowerMult,
				this.upperMult
			);

			// Set the decoupler module's ejection force to ours.  In the editor, this is meaningless.  In flight,
			// this sets the ejectionForce from our persistent value when the part is started.
			this.decoupleModule.Fields["ejectionForce"].SetValue(remoteEjectionForce, this.decoupleModule);

			// Seed the stagingEnabled state so we make sure to run on the first update.
			this.stagingState = !this.stagingEnabled;
		}

		public void LateUpdate()
		{
			// If the decoupler has already fired...
			if (this.decoupleModule.Fields["isDecoupled"].GetValue<bool>(this.decoupleModule))
			{
				// ...disable the tweakable
				this.Fields["stagingEnabled"].guiActive = this.Fields["stagingEnabled"].guiActiveEditor = false;

				// ...and do nothing else
				return;
			}

			// If our staging state has changed...
			if (this.stagingState != this.stagingEnabled)
			{
				// ...seed the last state
				this.stagingState = this.stagingEnabled;

				// ...and switch the staging
				this.SwitchStaging(this.stagingEnabled);
			}
		}

		// Switches the staging
		protected void SwitchStaging(bool enabled)
		{
			// If we're switching to enabled...
			if (enabled)
			{
				// ..and if our part has fallen off the staging list...
				if (Staging.StageCount < this.part.inverseStage + 1)
				{
					// ...add a new stage at the end
					Staging.AddStageAt(Staging.StageCount);
					// ...and move our part to it
					this.part.inverseStage = Staging.StageCount - 1;
				}

				// ...add our icon to the staging list
				Tools.PostDebugMessage(this, "Assigning inverseStage " + this.part.inverseStage, "Stage Count: " + Staging.StageCount);
				this.part.stackIcon.CreateIcon();
			}
			// ...otherwise, we're switching to disabled, so...
			else
			{
				// ...remove the icon from the list
				this.part.stackIcon.RemoveIcon();
			}

			// Sort the staging list
			Staging.ScheduleSort();

			// Clobber the "staged" field in the decoupler module
			this.decoupleModule.Fields["staged"].SetValue(enabled, this.decoupleModule);
		}
	}
}
