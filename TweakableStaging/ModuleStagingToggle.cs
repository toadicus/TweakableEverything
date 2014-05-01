// TweakableStaging, a TweakableEverything module
//
// EmptyCSharpFile.cs
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
using ToadicusTools;
using UnityEngine;

namespace TweakableEverything
{
	public class ModuleStagingToggle : PartModule
	{
		// Store the tweaked staging enabled toggle for clobbering the value in the real decouplerModule.
		[KSPField(isPersistant = true, guiName = "Staging", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
		public bool stagingEnabled;
		// Stores its last state so we can only run when things change.
		protected bool stagingState;

		[KSPField(isPersistant = false)]
		public bool activeInEditor;

		[KSPField(isPersistant = false)]
		public bool activeInFlight;

		[KSPField(isPersistant = false)]
		public bool defaultDisabled;

		public override void OnAwake()
		{
			base.OnAwake();

			// Default stagingEnabled to true for consistency with stock behavior.
			this.stagingEnabled = !this.defaultDisabled;

			Tools.PostDebugMessage(this, "OnAwake.  stagingEnabled: {0}, defaultDisabled: {1}",
				this.stagingEnabled, this.defaultDisabled
			);
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.Fields["stagingEnabled"].guiActiveEditor = this.activeInEditor;
			this.Fields["stagingEnabled"].guiActive = this.activeInFlight;

			// Seed the stagingEnabled state so we make sure to run on the first update.
			this.stagingState = !this.stagingEnabled;

			if (!this.part.hasStagingIcon)
			{
				this.part.stagingIcon = Enum.GetName(typeof(DefaultIcons), DefaultIcons.DECOUPLER_VERT);
				this.part.stackIcon.SetIcon(DefaultIcons.DECOUPLER_VERT);
			}

			if (this.stagingEnabled)
			{
				this.part.stackIcon.CreateIcon();
			}
			else
			{
				this.part.stackIcon.RemoveIcon();
			}

			GameEvents.onPartAttach.Add(this.onPartAttach);
			GameEvents.onPartCouple.Add(this.onFromToEvent);
			GameEvents.onUndock.Add(this.onGenericEvent);
			GameEvents.onVesselChange.Add(this.onGenericEvent);

			Tools.PostDebugMessage(this,
				"Started." +
				"\n\tstagingEnabled: {0}" +
				"part.stackIcon.iconImage: {1}",
				this.stagingEnabled,
				this.part.stackIcon.iconImage
			);
		}

		public void LateUpdate()
		{
			// If our staging state has changed...
			if (this.stagingState != this.stagingEnabled)
			{
				Tools.PostDebugMessage(this, "Staging state changed." +
					"\n\tstagingEnable: {0}" +
					"\n\tpart.stackIcon.iconImage: {1}",
					this.stagingEnabled,
					this.part.stackIcon.iconImage
				);

				// ...seed the last state
				this.stagingState = this.stagingEnabled;

				// ...and switch the staging
				this.SwitchStaging(this.stagingEnabled);
			}
		}

		public void Destroy()
		{
			GameEvents.onPartAttach.Remove(this.onPartAttach);
		}

		protected void SwitchStaging(bool enabled)
		{
			// If we're switching to enabled...
			if (enabled)
			{
				this.part.inverseStage = Math.Max(this.part.inverseStage, 0);

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

				this.part.inverseStage = this.GetDecoupledStage();
			}

			// Sort the staging list
			Staging.ScheduleSort();

			this.OnToggle(this, new BoolArg(this.stagingEnabled));
		}

		// Gets the inverse stage in which this decoupler's part will be removed from the craft, or -1 if not
		protected int GetDecoupledStage()
		{
			int iStage = -1;

			if (this.stagingEnabled)
			{
				iStage = this.part.inverseStage;
			}
			else
			{
				Part ancestorPart = this.part;
				while (ancestorPart.parent != null)
				{
					ancestorPart = ancestorPart.parent;

					if (ancestorPart.isDecoupler())
					{
						ModuleStagingToggle tweakableStagingModule;

						if (ancestorPart.tryGetFirstModuleOfType<ModuleStagingToggle>(out tweakableStagingModule))
						{
							if (!tweakableStagingModule.stagingEnabled)
							{
								continue;
							}
						}

						iStage = ancestorPart.inverseStage;
					}
				}
			}

			return iStage;
		}

		protected void onPartAttach(GameEvents.HostTargetAction<Part, Part> data)
		{
			Tools.PostDebugMessage(this, "Caught onPartAttach with host {0} and target {1}", data.host, data.target);

			if (this.part.hasAncestorPart(data.target))
			{
				this.part.inverseStage = this.GetDecoupledStage();
			}
		}

		protected void onGenericEvent(object _)
		{
			this.stagingState = !this.stagingEnabled;
		}

		protected void onFromToEvent(GameEvents.FromToAction<Part, Part> _)
		{
			this.onGenericEvent(null);
		}

		public event ToggleEventHandler OnToggle;

		public delegate void ToggleEventHandler(object sender, ModuleStagingToggle.BoolArg args);

		public class BoolArg : EventArgs
		{
			public bool Value { get; protected set; }

			private BoolArg() {}

			public BoolArg(bool value)
			{
				this.Value = value;
			}
		}
	}
}
