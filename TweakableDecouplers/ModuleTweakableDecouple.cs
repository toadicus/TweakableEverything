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
using KSPAPIExtensions;
using System;
using System.Collections.Generic;
using ToadicusTools.Extensions;
using UnityEngine;

namespace TweakableEverything
{
	/*
	 * @TODO: Remove this whole module in favor of a simple MM patch on top of Squad's percentage slider, or 
	 * */
	#if DEBUG
	public class ModuleTweakableDecouple : DebugPartModule
	#else
	public class ModuleTweakableDecouple : PartModule
	#endif
	{
		// Stores the name of the decoupler module, since sometimes it is different.
		[KSPField(isPersistant = false)]
		public string decouplerModuleName;

		// Stores the decoupler module
		protected PartModule decoupleModule;

		// Stores the tweaked ejectionForce for clobbering the value in the real decouplerModule.
		[KSPField(isPersistant = true, guiName = "Ejection Force", guiUnits = "N", guiFormat = "S2+3",
			guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float ejectionForce;

		// Stores the configurable multiplier for the lower bound on the FloatRange
		[KSPField(isPersistant = false)]
		public float lowerMult;
		// Stores the configurable multiplier for the upper bound on the FloatRange
		[KSPField(isPersistant = false)]
		public float upperMult;

		/* @stockified
		[KSPField(isPersistant = true)]
		public bool staged;*/

		// Construct ALL the objects.
		public ModuleTweakableDecouple() : base()
		{
			// We'll use -1 to mean "uninitialized" for purposes of defaulting to the base module's value
			this.ejectionForce = -1;

			/* @stockified
			this.staged = true;
			*/

			// Set the default multipler bounds.
			this.lowerMult = 0f;
			this.upperMult = 2f;

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
			if (base.part.tryGetFirstModuleByName(this.decouplerModuleName, out this.decoupleModule))
			{
				// Fetch the prefab for harvesting the actual stock value.  This is done to prevent copies in the editor
				// from inheriting a tweaked value as their "center".
				partInfo = PartLoader.getPartInfoByName(base.part.partInfo.name);

				// Fetch the prefab module for the above purpose.
				if (partInfo.partPrefab.tryGetFirstModuleByName(this.decouplerModuleName, out prefabModule))
				{// Fetch the ejectionForce field from our generic decoupler module.
					float remoteEjectionForce =
						this.decoupleModule.Fields["ejectionForce"].GetValue<float>(this.decoupleModule);

					// Build initialize the FloatRange with upper and lower bounds from the cfg file, center value from the
					// prefab, and current value from persistence
					TweakableTools.InitializeTweakable<ModuleTweakableDecouple>(
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

					/* @stockified
					this.decoupleModule.Fields["staged"].SetValue(this.staged, this.decoupleModule);
					*/

					this.decoupleModule.Fields["ejectionForcePercent"].guiActive = false;
					this.decoupleModule.Fields["ejectionForcePercent"].guiActiveEditor = false;
					this.decoupleModule.Fields["ejectionForcePercent"].uiControlCurrent().controlEnabled = false;
				}

				/* @stockified
				ModuleStagingToggle stagingToggleModule;

				if (this.part.tryGetFirstModuleOfType<ModuleStagingToggle>(out stagingToggleModule))
				{
					stagingToggleModule.OnToggle += new ModuleStagingToggle.ToggleEventHandler(this.OnStagingToggle);
				}*/
			}
		}

		/* @stockified
		public void LateUpdate()
		{
			try
			{
				if (this.decoupleModule == null)
					return;

				// If the decoupler has already fired...
				if (this.decoupleModule.Fields["isDecoupled"].GetValue<bool>(this.decoupleModule))
				{
					// ...disable the tweakable
					this.Fields["stagingEnabled"].guiActive = this.Fields["stagingEnabled"].guiActiveEditor = false;

					// ...and do nothing else
					return;
				}
			}
			catch (NullReferenceException) { }
		}

		// Switches the staging
		protected void OnStagingToggle(object sender, ModuleStagingToggle.BoolArg arg)
		{
			if (this.decoupleModule == null)
				return;

			this.LogDebug("OnStagingToggle called.");

			// Clobber the "staged" field in the decoupler module
			this.decoupleModule.Fields["staged"].SetValue(arg.Value, this.decoupleModule);
			this.staged = arg.Value;
		}*/
	}
}
