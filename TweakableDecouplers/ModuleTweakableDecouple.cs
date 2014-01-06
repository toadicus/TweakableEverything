// TweakableDockingNode © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModuleTweakableDecoupler
{
	public class ModuleTweakableDecouple : PartModule
	{
		[KSPField(isPersistant = true, guiName = "Ejection Force (kN)", guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float ejectionForce;

		protected ModuleDecouple decoupleModule;

		public ModuleTweakableDecouple() : base()
		{
			this.ejectionForce = -1;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.decoupleModule = base.part.Modules.OfType<ModuleDecouple>().First();

			if (this.ejectionForce == -1)
			{
				this.ejectionForce = this.decoupleModule.ejectionForce;
			}

			((UI_FloatRange)this.Fields["ejectionForce"].uiControlEditor).minValue = 0;
			((UI_FloatRange)this.Fields["ejectionForce"].uiControlEditor).maxValue =
				this.decoupleModule.ejectionForce * 2;
			((UI_FloatRange)this.Fields["ejectionForce"].uiControlEditor).stepIncrement =
				Mathf.Pow(10f, Mathf.RoundToInt(Mathf.Log10(this.decoupleModule.ejectionForce)) - 1);

			this.decoupleModule.ejectionForce = this.ejectionForce;
		}
	}
}
