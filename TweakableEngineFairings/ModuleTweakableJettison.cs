// TweakableEverything © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using System;
using System.Linq;
using UnityEngine;

namespace TweakableEverything
{
	public class ModuleTweakableJettison : PartModule
	{
		protected ModuleJettison jettisonModule;

		protected Transform jettisonTransform;

		[KSPField(isPersistant = true, guiName = "Fairing", guiActive = false, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Disabled", disabledText = "Enabled")]
		public bool disableFairing;

		protected bool disableState;

		public ModuleTweakableJettison()
		{
			// Seed disable flag to false to emulate stock behavior
			this.disableFairing = false;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.jettisonModule = base.part.Modules.OfType<ModuleJettison>().FirstOrDefault();

			this.jettisonTransform = this.jettisonModule.jettisonTransform;

			this.disableState = !this.disableFairing;
		}

		public void LateUpdate()
		{
			if (this.jettisonModule != null && (this.disableState != this.disableFairing))
			{
				Tools.PostDebugMessage(this, "Fairing state switched");

				this.disableState = this.disableFairing;

				this.jettisonTransform.gameObject.SetActive(!this.disableFairing);
				this.jettisonModule.Events["Jettison"].guiActive = !this.disableFairing;

				Tools.PostDebugMessage(this, string.Format("Set transform's gameObject to {0}", !this.disableFairing));

				if (this.disableFairing)
				{
					this.jettisonModule.jettisonTransform = null;
					Tools.PostDebugMessage(this, "transform set to null.");
				}
				else
				{
					this.jettisonModule.jettisonTransform = this.jettisonTransform;
					Tools.PostDebugMessage(this, "transform reset.");
				}
			}
		}
	}
}

