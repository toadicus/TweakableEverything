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
using System.Reflection;
using ToadicusTools;
using UnityEngine;

namespace TweakableEverything
{
	public class ModuleStagingToggle : PartModule
	{
		private static Tools.DebugLogger log;

		private static FieldInfo stagingInstanceField;
		private static Staging stagingInstance;

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
			#if DEBUG
			if (log == null)
			{
				log = Tools.DebugLogger.New(this);
			}
			#endif

			log.Clear();

			log.AppendFormat("{0}: Waking up.", this);

			base.OnAwake();

			log.AppendFormat("\n\tOnAwake with defaultDisabled={0}", this.defaultDisabled);
			this.stagingEnabled = !this.defaultDisabled;
			this.updatePeriod = 0.0625f;
			this.timeSinceUpdate = 0f;

			this.forceUpdate = false;
			this.queuedStagingSort = false;

			if (stagingInstanceField == null)
			{
				FieldInfo[] staticStagingFields = typeof(Staging).GetFields(
					BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Default
				);

				FieldInfo field;
				for (int fIdx = 0; fIdx < staticStagingFields.Length; fIdx++)
				{
					field = staticStagingFields[fIdx];

					if (field.FieldType == typeof(Staging))
					{
						stagingInstanceField = field;

						this.Log("Got Staging instance field: {0}",
							stagingInstanceField == null ? "null" : stagingInstanceField.ToString());
						
						break;
					}
				}
			}

			log.AppendFormat("\n{0}: Awake; stagingEnabled={1}.\n", this, this.stagingEnabled);

			log.Print();
		}

		public override void OnStart(StartState state)
		{
			log.Clear();

			log.AppendFormat("{0}: Starting up.", this);

			log.AppendFormat("\n\tOnStart with stagingEnabled={0}"/* +
				"\npart.isInStagingList={1}"*/,
				this.stagingEnabled/*,
				this.part.isInStagingList()*/
			);

			log.AppendFormat("\n\tStarting with state {0}", state);
			base.OnStart(state);

			this.Fields["stagingEnabled"].guiActiveEditor = this.activeInEditor;
			this.Fields["stagingEnabled"].guiActive = this.activeInFlight;

			log.AppendFormat("\n\tguiActiveEditor: {0} guiActive: {1}",
				this.Fields["stagingEnabled"].guiActiveEditor, this.activeInFlight);

			if (this.stagingIcon != string.Empty && this.stagingIcon != null)
			{
				log.AppendFormat("\n\tstagingIcon={0}, setting icon.");
				DefaultIcons icon = (DefaultIcons)Enum.Parse(typeof(DefaultIcons), this.stagingIcon);

				this.part.stagingIcon = this.stagingIcon;
				this.part.stackIcon.SetIcon(icon);
				log.AppendFormat("\n\ticon set to {0} ({1})", icon, this.part.stackIcon);
			}
			else if (!this.part.hasStagingIcon)
			{
				log.AppendFormat("\n\tstagingIcon is null or empty, and the part does not have a stagingIcon");
				log.AppendFormat("\n\tbuilding a new DECOUPLER_VERT icon");
				this.part.stagingIcon = Enum.GetName(typeof(DefaultIcons), DefaultIcons.DECOUPLER_VERT);
				this.part.stackIcon.SetIcon(DefaultIcons.DECOUPLER_VERT);
			}
			#if DEBUG
			else
			{
				log.AppendFormat("\n\tThe part or some other module has already taken care of the stagingIcon.");
			}
			#endif

			if (this.part.inverseStage < 0)
			{
				log.AppendFormat("\n\tFound part with negative inverseStage={0}; zeroing.", this.part.inverseStage);
				this.part.inverseStage = 0;
			}

			if (stagingInstanceField != null && stagingInstance == null)
			{
				stagingInstance = stagingInstanceField.GetValue(null) as Staging;

				this.Log("Got Staging instance: {0}", stagingInstance == null ? "null" : stagingInstance.ToString());
			}

			log.AppendFormat("\n\tRegistering events");
			GameEvents.onPartAttach.Add(this.onPartAttach);
			GameEvents.onPartCouple.Add(this.onPartCouple);
			GameEvents.onUndock.Add(this.onUndock);
			GameEvents.onVesselChange.Add(this.onVesselChange);

			log.AppendFormat("\nStarted; stagingEnabled: {0}, part.stackIcon.iconImage: {1}\n",
				this.stagingEnabled, this.part.stackIcon.iconImage);

			log.Print();
		}

		public void LateUpdate()
		{
			if (
				this.timeSinceUpdate > this.updatePeriod &&
				stagingInstance != null &&
				stagingInstance.stages.Count > 0 &&
				!Staging.stackLocked
			)
			{
				log.Clear();

				log.AppendFormat("{0}: Time to update, stagingEnabled={1}, isInStagingList={2}",
					this, this.stagingEnabled, this.part.isInStagingList());

				if (stageSortQueued && this.queuedStagingSort)
				{
					log.AppendFormat("\n\tThis module queued a staging event last update; scheduling it now.");

					Staging.ScheduleSort();

					stageSortQueued = false;
					this.queuedStagingSort = false;
				}

				this.timeSinceUpdate = 0f;

				Part rootPart;
				switch (HighLogic.LoadedScene)
				{
					case GameScenes.EDITOR:
						rootPart = EditorLogic.RootPart;
						break;
					case GameScenes.FLIGHT:
						rootPart = FlightGlobals.ActiveVessel != null ? FlightGlobals.ActiveVessel.rootPart : null;
						break;
					default:
						rootPart = null;
						break;
				}

				// If our staging state has changed...
				if (rootPart != null &&
					this.part.hasAncestorPart(rootPart) &&
					(this.forceUpdate || this.stagingEnabled != this.part.isInStagingList())
				)
				{
					log.AppendFormat("\n\tStaging state changed." +
					"\n\t\tstagingEnable: {0}" +
					"\n\t\tpart.stackIcon.iconImage: {1}" +
					"\n\t\tpart.isInStagingList: {2}",
						this.stagingEnabled,
						this.part.stackIcon.iconImage,
						this.part.isInStagingList()
					);

					// ...and switch the staging
					this.SwitchStaging(this.stagingEnabled);
	
					this.forceUpdate = false;
				}

				log.Append("\nLateUpdate done.\n");

				log.Print();
			}

			this.timeSinceUpdate += Time.smoothDeltaTime;
		}

		public void OnDestroy()
		{
			log.Clear();

			log.AppendFormat("{0}: Destroying", this);

			GameEvents.onPartAttach.Remove(this.onPartAttach);
			GameEvents.onPartCouple.Remove(this.onPartCouple);
			GameEvents.onUndock.Remove(this.onUndock);
			GameEvents.onVesselChange.Remove(this.onVesselChange);

			log.AppendFormat("...events deregistered.");

			log.Print();
		}
		#endregion

		#region Utility Methods
		protected void SwitchStaging(bool enabled)
		{
			if (this.part == null)
			{
				log.AppendFormat("\n\t...could not switch staging: part reference is null.");
				return;
			}

			// If we're switching to enabled...
			if (enabled)
			{
				this.part.inverseStage = Math.Max(this.part.inverseStage - this.part.stageOffset, 0);

				log.AppendFormat("\n\tSwitching staging to enabled, default new inverseStage={0}",
					this.part.inverseStage);

				// ..and if our part has fallen off the staging list...
				if (stagingInstance.stages.Count < this.part.inverseStage + 1)
				{
					// ...add a new stage at the end
					log.AppendFormat("\n\tTrying to add new stage at {0}", stagingInstance.stages.Count);

					try
					{
						Staging.AddStageAt(stagingInstance.stages.Count);
					}
					catch (ArgumentOutOfRangeException)
					{
						log.AppendFormat("\n\t...Handled ArgumentOutOfRangeException instead.");
					}

					// ...and move our part to it
					this.part.inverseStage = stagingInstance.stages.Count - 1;

					log.AppendFormat("\n\t\tinverseStage had fallen off the list, fixed to {0}",
						this.part.inverseStage);
				}

				// ...add our icon to the staging list
				log.Append("\n\tCreating our staging icon in the staging list.");
				this.part.stackIcon.CreateIcon();
			}
			// ...otherwise, we're switching to disabled, so...
			else
			{
				log.Append("\n\tSwitching staging to disabled");

				bool needsStageAssignment = false;
				if (this.part.isInStagingList() != enabled)
				{
					log.Append("\n\t\tThe part is in the staging list, so we need a new stage assignment");
					needsStageAssignment = true;
				}

				log.Append("\n\tRemoving our staging icon from the staging list");
				// ...remove the icon from the list
				this.part.stackIcon.RemoveIcon();

				if (needsStageAssignment)
				{
					log.Append("\n\t\tWe removed our icon from staging, so fetching a new inverseStage");
					this.part.inverseStage = this.GetDecoupledStage();
					log.AppendFormat("={0}", this.part.inverseStage);
				}
				#if DEBUG
				else
				{
					log.Append("\n\t\tOur icon was already not in staging, so not assigning a new inverseStage.");
				}
				#endif
			}

			this.part.inverseStage = Math.Max(Math.Min(this.part.defaultInverseStage, stagingInstance.stages.Count - 1), 0);

			// Sort the staging list
			if (!stageSortQueued)
			{
				log.Append("\n\tNo other module has queued a staging sort this update; queueing now.");

				stageSortQueued = true;
				this.queuedStagingSort = true;
			}

			if (this.OnToggle != null)
			{
				log.Append("\n\tWe have OnToggle subscribers; firing OnToggle event for them now.");
				this.OnToggle(this, this.stagingEnabled ? BoolArg.True : BoolArg.False);
			}

			log.Append("\n\tStaging switch done");
		}

		// Gets the inverse stage in which this decoupler's part will be removed from the craft
		protected int GetDecoupledStage()
		{
			int iStage = 0;

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
			public static readonly BoolArg True;
			public static readonly BoolArg False;

			static BoolArg()
			{
				True = new BoolArg(true);
				False = new BoolArg(false);
			}

			public bool Value { get; protected set; }

			private BoolArg() {}

			public BoolArg(bool value) : base()
			{
				this.Value = value;
			}
		}
	}
}
