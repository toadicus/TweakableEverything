// TweakableEverything © 2014 toadicus
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
	public class ModuleTweakableJettison : PartModule
	{
		protected List<ModuleJettison> jettisonModules;

		protected Dictionary<string, Transform> jettisonTransforms;

		protected Dictionary<string, bool> isJettisonedTable;

		[KSPField(isPersistant = true, guiName = "Fairing", guiActive = false, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Disabled", disabledText = "Enabled")]
		public bool disableFairing;

		protected bool disableState;

		public ModuleTweakableJettison()
		{
			// Seed disable flag to false to emulate stock behavior
			this.disableFairing = false;

			this.jettisonModules = new List<ModuleJettison>();
			this.jettisonTransforms = new Dictionary<string, Transform>();
			this.isJettisonedTable = new Dictionary<string, bool>();
		}

		public override void OnStart(StartState state)
		{
			// Start up the base PartModule, just in case.
			base.OnStart(state);

			// Fetch all of the ModuleJettisons from the part, filling a list of modules and a dict of transforms
			foreach (PartModule module in base.part.Modules)
			{
				if (module is ModuleJettison)
				{
					ModuleJettison jettisonModule = module as ModuleJettison;
					this.jettisonModules.Add(jettisonModule);
					this.jettisonTransforms[jettisonModule.jettisonName] = jettisonModule.jettisonTransform;
					this.isJettisonedTable[jettisonModule.jettisonName] = jettisonModule.isJettisoned;
				}
			}

			Tools.PostDebugMessage(this, string.Format("Found {0} ModuleJettisons.", this.jettisonModules.Count()));

			// Seed the disableState for first-run behavior.
			this.disableState = !this.disableFairing;
		}

		public void LateUpdate()
		{
			// If the disableFairing toggle has changed...
			if (this.disableState != this.disableFairing)
			{
				Tools.PostDebugMessage(this, "Fairing state switched");

				// ...re-seed the disableState
				this.disableState = this.disableFairing;

				// ...loop through the jettison modules and...
				foreach (ModuleJettison jettisonModule in this.jettisonModules)
				{
					// ...if the jettison module has not already been jettisoned...
					if (!jettisonModule.isJettisoned)
					{
						Transform jettisonTransform;

						// ...fetch the corresponding transform
						jettisonTransform = this.jettisonTransforms[jettisonModule.jettisonName];

						// ...set the transform's activity state
						jettisonTransform.gameObject.SetActive(!this.disableFairing);
						// ...set the module's event visibility
						jettisonModule.Events["Jettison"].guiActive = !this.disableFairing;

						Tools.PostDebugMessage(this,
							string.Format("Set transform's gameObject to {0}", !this.disableFairing));

						// ...and if the fairing is disabled...
						if (this.disableFairing)
						{
							// ...null the jettison module's transform
							jettisonModule.jettisonTransform = null;
							Tools.PostDebugMessage(this, "transform set to null.");
						}
						// ...otherwise, the fairing is enabled...
						else
						{
							// ...return the jettison module's transform
							jettisonModule.jettisonTransform = jettisonTransform;
							Tools.PostDebugMessage(this, "transform reset.");
						}

						jettisonModule.isJettisoned = !this.disableFairing |
							this.isJettisonedTable[jettisonModule.jettisonName];
					}
				}
			}
		}
	}
}

