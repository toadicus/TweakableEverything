// TweakableDockingNode © 2014 toadicus
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

		// Construct ALL the objects.
		public ModuleTweakableDecouple() : base()
		{
			// We'll use -1 to mean "uninitialized" for purposes of defaulting to the base module's value
			this.ejectionForce = -1;
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

			// Fetch the decoupler module from the part by module name.
			this.decoupleModule = base.part.Modules
				.OfType<PartModule>()
				.FirstOrDefault(m => m.moduleName == this.decouplerModuleName);

			partInfo = PartLoader.getPartInfoByName(base.part.partInfo.name);

			prefabModule = partInfo.partPrefab.Modules
				.OfType<PartModule>()
				.FirstOrDefault(m => m.moduleName == this.decouplerModuleName);

			// If our ejection force is uninitialized...
			if (this.ejectionForce == -1)
			{
				// Fetch it from the decoupler module
				this.ejectionForce = this.decoupleModule.Fields["ejectionForce"].GetValue<float>(this.decoupleModule);
			}

			// Set the bounds and increment on the tweakable
			((UI_FloatRange)this.Fields["ejectionForce"].uiControlEditor).minValue = 0;
			((UI_FloatRange)this.Fields["ejectionForce"].uiControlEditor).maxValue =
				prefabModule.Fields["ejectionForce"].GetValue<float>(prefabModule) * 2;
			((UI_FloatRange)this.Fields["ejectionForce"].uiControlEditor).stepIncrement =
				Mathf.Pow(10f,
					Mathf.RoundToInt(
						Mathf.Log10(prefabModule.Fields["ejectionForce"].GetValue<float>(prefabModule))
					) - 1
				);

			// Set the decoupler module's ejection force to ours.  In the editor, this is meaningless.  In flight,
			// this sets the ejectionForce from our persistent value when the part is started.
			this.decoupleModule.Fields["ejectionForce"].SetValue(this.ejectionForce, this.decoupleModule);
		}
	}
}
