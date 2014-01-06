// TweakableSolarPanels © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TweakableEverything
{
	public class ModuleTweakableSolarPanel : PartModule
	{
		protected ModuleDeployableSolarPanel solarPanelModule;

		// Tweakable property to determine whether the docking port should start opened or closed.
		[KSPField(guiName = "Start", isPersistant = true, guiActiveEditor = true)]
		[UI_Toggle(disabledText = "Retracted", enabledText = "Extended")]
		public bool StartOpened;
		// Save the state here so we can tell if StartOpened has changed.
		protected bool startOpenedState;

		protected bool firstUpdate;

		public ModuleTweakableSolarPanel()
		{
			this.StartOpened = false;
			this.firstUpdate = true;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.startOpenedState = !this.StartOpened;

			this.solarPanelModule = base.part.Modules.OfType<ModuleDeployableSolarPanel>().FirstOrDefault();

			if (HighLogic.LoadedSceneIsEditor)
			{
				this.solarPanelModule.OnStart(StartState.PreLaunch);
			}
		}

		public void LateUpdate()
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				if (this.startOpenedState != this.StartOpened)
				{
					this.startOpenedState = this.StartOpened;

					if (this.StartOpened && this.solarPanelModule.panelState == ModuleDeployableSolarPanel.panelStates.RETRACTED)
					{
						Tools.PostDebugMessage(this.GetType().Name + ": Extending panels.");
						this.solarPanelModule.Extend();
					}

					if (!this.StartOpened && this.solarPanelModule.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED)
					{
						Tools.PostDebugMessage(this.GetType().Name + ": Retracting panels.");
						this.solarPanelModule.Retract();
					}
				}

				try
				{
					this.solarPanelModule.updateFSM();
				}
				catch (NullReferenceException)
				{
				}
			}

			/*if (HighLogic.LoadedSceneIsFlight)
			{
				if (this.firstUpdate)
				{
					this.firstUpdate = false;

					if (this.StartOpened)
					{
						this.solarPanelModule.Extend();
					}
				}
			}*/
		}
	}
}

