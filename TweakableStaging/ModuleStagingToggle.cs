// TweakableStaging, a TweakableEverything module
//
// ModuleStagingToggle.cs
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
	#if DEBUG
	public class ModuleStagingToggle : DebugPartModule
	#else
	public class ModuleStagingToggle : PartModule
	#endif
	{
		public static bool stageSortQueued = false;

		#region Interface Elements
		// Store the tweaked staging enabled toggle for clobbering the value in the real decouplerModule.
		[KSPField(isPersistant = true, guiName = "Staging", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
		public bool stagingEnabled;

		[KSPField(isPersistant = false)]
		public bool activeInEditor;

		[KSPField(isPersistant = false)]
		public bool activeInFlight;
		[KSPField(isPersistant = false)]
		public bool defaultDisabled;

		[KSPField(isPersistant = false)]
		public string stagingIcon;

		public event ToggleEventHandler OnToggle;

		public delegate void ToggleEventHandler(object sender, ModuleStagingToggle.BoolArg args);
		#endregion

		// Stores the last toggle state so we can only run when things change.
		protected bool forceUpdate;
		protected bool queuedStagingSort;

		protected float updatePeriod;
		protected float timeSinceUpdate;

		#region LifeCycle Methods
		public override void OnAwake()
		{
			base.OnAwake();

			this.LogDebug("OnAwake with defaultDisabled={0}", this.defaultDisabled);
			this.stagingEnabled = !this.defaultDisabled;
			this.updatePeriod = 0.125f;
			this.timeSinceUpdate = 0f;

			this.forceUpdate = false;
			this.queuedStagingSort = false;
		}

		public override void OnStart(StartState state)
		{
			this.LogDebug("OnStart with stagingEnabled={0}" +
				"\npart.isInStagingList={1}",
				this.stagingEnabled,
				this.part.isInStagingList()
			);

			Tools.PostDebugMessage(this, "Starting with state {0}", state);
			base.OnStart(state);

			this.Fields["stagingEnabled"].guiActiveEditor = this.activeInEditor;
			this.Fields["stagingEnabled"].guiActive = this.activeInFlight;

			Tools.PostDebugMessage(this, "guiActiveEditor: {0} guiActive: {1}",
				this.Fields["stagingEnabled"].guiActiveEditor, this.activeInFlight);

			if (this.stagingIcon != string.Empty && this.stagingIcon != null)
			{
				DefaultIcons icon = (DefaultIcons)Enum.Parse(typeof(DefaultIcons), this.stagingIcon);

				this.part.stagingIcon = this.stagingIcon;
				this.part.stackIcon.SetIcon(icon);
			}
			else if (!this.part.hasStagingIcon)
			{
				this.part.stagingIcon = Enum.GetName(typeof(DefaultIcons), DefaultIcons.DECOUPLER_VERT);
				this.part.stackIcon.SetIcon(DefaultIcons.DECOUPLER_VERT);
			}

			if (this.part.stackIcon != null)
			{
				if (this.stagingEnabled)
				{
					this.part.stackIcon.CreateIcon();
				}
				else
				{
					this.part.stackIcon.RemoveIcon();
				}
			}

			GameEvents.onPartAttach.Add(this.onPartAttach);
			GameEvents.onPartCouple.Add(this.onPartCouple);
			GameEvents.onUndock.Add(this.onUndock);
			GameEvents.onVesselChange.Add(this.onVesselChange);

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
			if (this.timeSinceUpdate > this.updatePeriod)
			{
				if (this.queuedStagingSort)
				{
					stageSortQueued = false;
					this.queuedStagingSort = false;
				}

				this.LogDebug("Time to update, stagingEnabled={0}", this.stagingEnabled);

				this.timeSinceUpdate = 0f;

				// If our staging state has changed...
				if (Staging.StageCount > 0 && (this.forceUpdate || this.stagingEnabled != this.part.isInStagingList()))
				{
					Tools.PostDebugMessage(this, "Staging state changed." +
					"\n\tstagingEnable: {0}" +
					"\n\tpart.stackIcon.iconImage: {1}" +
					"\n\tpart.isInStagingList: {2}",
						this.stagingEnabled,
						this.part.stackIcon.iconImage,
						this.part.isInStagingList()
					);

					// ...and switch the staging
					this.SwitchStaging(this.stagingEnabled);
	

					this.forceUpdate = false;
				}
			}

			this.timeSinceUpdate += Time.deltaTime;
		}

		public void OnDestroy()
		{
			GameEvents.onPartAttach.Remove(this.onPartAttach);
			GameEvents.onPartCouple.Remove(this.onPartCouple);
			GameEvents.onUndock.Remove(this.onUndock);
			GameEvents.onVesselChange.Remove(this.onVesselChange);
		}
		#endregion

		#region Utility Methods
		protected void SwitchStaging(bool enabled)
		{
			if (this.part == null)
			{
				Tools.PostDebugMessage(this, "could not switch staging: part reference is null.");
				return;
			}

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
				this.LogDebug("Assigning inverseStage " + this.part.inverseStage, "Stage Count: " + Staging.StageCount);
				this.part.stackIcon.CreateIcon();
			}
			// ...otherwise, we're switching to disabled, so...
			else
			{
				bool needsStageAssignment = false;
				if (this.part.isInStagingList() != this.stagingEnabled)
				{
					needsStageAssignment = true;
				}

				// ...remove the icon from the list
				this.part.stackIcon.RemoveIcon();

				if (needsStageAssignment)
				{
					this.part.inverseStage = this.GetDecoupledStage();
					this.LogDebug("Removed from list, assigned inverseStage={0}", this.part.inverseStage);
				}
				#if DEBUG
				else
				{
					this.LogDebug("Removed from list, no stage assigned.", this.part.inverseStage);
				}
				#endif
			}

			// Sort the staging list
			if (!stageSortQueued)
			{
				this.LogDebug("Scheduling staging sort.");

				Staging.ScheduleSort();

				stageSortQueued = true;
				this.queuedStagingSort = true;
			}

			if (this.OnToggle != null)
			{
				this.OnToggle(this, new BoolArg(this.stagingEnabled));
			}
			else
			{
				Tools.PostDebugMessage(this, "cannot raise OnToggle: no subscribers to call.");
			}
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
						break;
					}
				}
			}

			return iStage;
		}
		#endregion

		#region Event Handlers
		protected void onPartAttach(GameEvents.HostTargetAction<Part, Part> data)
		{
			this.LogDebug("Caught onPartAttach with host {0} and target {1}", data.host, data.target);

			// Do nothing if our part or the part being attached are null.
			if (data.target == null || this.part == null)
			{
				return;
			}

			if (this.part.hasAncestorPart(data.target))
			{
				this.part.inverseStage = this.GetDecoupledStage();
			}
		}

		protected void onUndock(EventReport data)
		{
			if (data.origin != null)
			{
				this.LogDebug("{0}: Running onUndock.", this.part.craftID);
				this.onPartEvent(data.origin);
			}
		}

		protected void onPartCouple(GameEvents.FromToAction<Part, Part> data)
		{
			if (data.from != null)
			{
				this.LogDebug("{0}: Running onPartCouple.", data.from.craftID);
				this.onPartEvent(data.from);
			}

			if (data.to != null)
			{
				this.LogDebug("{0}: Running onPartCouple.", data.to.craftID);
				this.onPartEvent(data.to);
			}
		}

		protected void onVesselChange(Vessel data)
		{
			if (this.part.vessel != null && data.id == this.part.vessel.id)
			{
				this.LogDebug("{0}: Running onVesselChange.", this.part.craftID);
				this.forceUpdate = true;
			}
		}

		protected void onPartEvent(Part data)
		{
			if (data.vessel != null && data.vessel.id == this.part.vessel.id)
			{
				this.forceUpdate = true;
			}
		}
		#endregion

		public class BoolArg : EventArgs
		{
			public bool Value { get; protected set; }

			private BoolArg() {}

			public BoolArg(bool value) : base()
			{
				this.Value = value;
			}
		}
	}
}
