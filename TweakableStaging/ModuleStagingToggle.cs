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
#define DEBUG
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

		private static bool waitingForStaging;

		private static bool stageSortQueued = false;

		#region Interface Elements
		// Store the tweaked staging enabled toggle for clobbering the value in the real decouplerModule.
		[KSPField(isPersistant = true, guiName = "Staging", guiActive = false, guiActiveEditor = false)]
		public bool stagingEnabled;

		[KSPField(isPersistant = false)]
		public bool activeInEditor;

		[KSPField(isPersistant = false)]
		public bool activeInFlight;
		[KSPField(isPersistant = false)]
		public bool defaultDisabled;

		[KSPField(isPersistant = false)]
		public string stagingIcon;

		// Only one module should ever be running per part.
		public bool partPrimary;

		public event ToggleEventHandler OnToggle;

		public delegate void ToggleEventHandler(object sender, ModuleStagingToggle.BoolArg args);
		#endregion

		// Stores the last toggle state so we can only run when things change.
		protected bool forceUpdate;
		protected bool justStarted;
		protected bool queuedStagingSort;

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

						this.LogDebug("Got Staging instance field: {0}",
							stagingInstanceField == null ? "null" : stagingInstanceField.ToString());
						
						break;
					}
				}
			}

			log.AppendFormat("\n{0}: Awake; stagingEnabled={1}.\n", this, this.stagingEnabled);

			bool otherModuleIsPrimary = false;

			PartModule module;
			for (int mIdx = 0; mIdx < this.part.Modules.Count; mIdx++)
			{
				module = this.part.Modules[mIdx];

				if (module is ModuleStagingToggle && module != this)
				{
					ModuleStagingToggle stagingModule = module as ModuleStagingToggle;

					log.AppendFormat("\nForeign module at index {0} is primary", mIdx);

					if (stagingModule.partPrimary)
					{
						otherModuleIsPrimary = true;
						break;
					}
				}
			}

			this.partPrimary = !otherModuleIsPrimary;

			log.AppendFormat("This module is {0}primary", this.partPrimary ? "" : "not ");

			log.Print(false);
		}

		public override void OnStart(StartState state)
		{
			if (!this.partPrimary)
			{
				this.LogWarning(
					"Non-primary ModuleStagingToggle in part {0} with multiple ModuleStagingToggles not starting up.",
					this.part.partInfo.title
				);

				this.Events["EnableEvent"].active = false;
				this.Events["DisableEvent"].active = false;

				return;
			}

			log.Clear();

			log.AppendFormat("{0}: Starting up.", this);

			waitingForStaging = true;

			log.AppendFormat("\n\tOnStart with stagingEnabled={0}"/* +
				"\npart.isInStagingList={1}"*/,
				this.stagingEnabled/*,
				this.part.isInStagingList()*/
			);

			log.AppendFormat("\n\tStarting with state {0}", state);
			base.OnStart(state);

			this.Events["EnableEvent"].active = !this.stagingEnabled;
			this.Events["DisableEvent"].active = this.stagingEnabled;

			this.Events["EnableEvent"].guiActiveEditor = this.activeInEditor;
			this.Events["DisableEvent"].guiActiveEditor = this.activeInEditor;

			this.Events["EnableEvent"].guiActive = this.activeInFlight;
			this.Events["DisableEvent"].guiActive = this.activeInFlight;

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

				this.LogDebug("Got Staging instance: {0}",
					stagingInstance == null ? "null" : stagingInstance.ToString());
			}

			if (this.stagingEnabled && this.part.symmetryCounterparts != null)
			{
				for (int pIdx = 0; pIdx < this.part.symmetryCounterparts.Count; pIdx++)
				{
					Part symPartner = this.part.symmetryCounterparts[pIdx];

					if (symPartner == null)
					{
						continue;
					}

					for (int mIdx = 0; mIdx < symPartner.Modules.Count; mIdx++)
					{
						PartModule module = symPartner.Modules[mIdx];

						if (module == null || !(module is ModuleStagingToggle))
						{
							continue;
						}

						ModuleStagingToggle symModule = module as ModuleStagingToggle;

						if (symModule.stagingEnabled == this.defaultDisabled)
						{
							this.stagingEnabled = symModule.stagingEnabled;

							log.AppendFormat("\n\tAssigning stagingEnabled={0} because a symmetry partner" +
								" with non-default options was found on startup", this.stagingEnabled);
						}
					}
				}
			}

			this.forceUpdate = true;
			this.justStarted = true;

			log.AppendFormat("\n\tRegistering events");
			GameEvents.onPartAttach.Add(this.onPartAttach);
			GameEvents.onPartCouple.Add(this.onPartCouple);
			GameEvents.onUndock.Add(this.onUndock);
			GameEvents.onVesselChange.Add(this.onVesselChange);

			log.AppendFormat("\nStarted; stagingEnabled: {0}, part.stackIcon.iconImage: {1}\n",
				this.stagingEnabled, this.part.stackIcon.iconImage);

			log.Print(false);
		}

		public void LateUpdate()
		{
			waitingForStaging &= stagingInstance.stages.Count < 1;

			#if DEBUG
			bool printLog = false;
			try {
			#endif
			
			log.Clear();
			
			if (
				this.partPrimary &&
				stagingInstance != null &&
				!waitingForStaging &&
				!Staging.stackLocked
			)
			{
				log.AppendFormat("{0}: Time to update, stagingEnabled={1}, isInStagingList={2}",
					this, this.stagingEnabled, this.part.isInStagingList());

				if (this.forceUpdate)
				{
					this.forceUpdate = false;

					log.AppendFormat("\n\tUpdate forced...");
					if (this.stagingEnabled)
					{
						this.EnableAtStage(this.part.inverseStage);
						log.AppendFormat("enabled at stage {0}", this.part.inverseStage);
					}
					else if (!this.stagingEnabled)
					{
						this.Disable();
						log.AppendFormat("disabled");
					}

					#if DEBUG
					printLog = true;
					#endif
				}

				if (stageSortQueued && this.queuedStagingSort)
				{
					// Un-queue the sort whether we can do it or not
					stageSortQueued = false;
					this.queuedStagingSort = false;

					if (stagingInstance.stages.Count > 0)
					{
						log.AppendFormat("\n\tThis module queued a staging event last update; sorting now.");

						Staging.SortNow();
					}
					#if DEBUG
					else
					{
						log.AppendFormat(
							"\n\tThis module queued a staging event last update, but now the stage list is empty." +
							"\n\tun-queuing sort"
						);
					}

					printLog = true;
					#endif
				}

				if (this.justStarted)
				{
					this.QueueStagingSort();
					this.justStarted = false;
				}

				log.Append("\nLateUpdate done.\n");
			}
				#if DEBUG
			} catch (Exception x)
			{
				log.AppendFormat("Caught exception: {0}", x.ToString());
			}
			finally
			{
				if (printLog)
				{
					log.Print(false);
				}
			}
			#endif
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

			log.Print(false);
		}
		#endregion

		#region KSPEvents
		[KSPEvent(guiName = "Enable Staging")]
		public void EnableEvent()
		{
			if (this.part == null)
			{
				this.LogError("Cannot enable staging: part reference is null");
				return;
			}

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

			int newInverseStage = 0;

			if (rootPart != null && this.part.hasAncestorPart(rootPart))
			{
				this.LogDebug("Part is on vessel; figuring new stage.");

				Part parentDecouplerPart;

				newInverseStage = this.GetDecoupledStage(out parentDecouplerPart);

				this.LogDebug("parentDecouplerPart={0}, new inverseStage={1}",
					parentDecouplerPart == null ? "null" : parentDecouplerPart.partInfo.title,
					newInverseStage
				);

				if (parentDecouplerPart != null && parentDecouplerPart.childStageOffset > 0)
				{
					newInverseStage += parentDecouplerPart.childStageOffset;

					this.LogDebug("parentDecouplerPart.childStageOffset={0}, new inverseStage={1}",
						parentDecouplerPart.childStageOffset, newInverseStage);
				}

				newInverseStage += this.part.stageOffset;

				this.LogDebug("stageOffset={0}, new inverseStage={1}",
					this.part.stageOffset, newInverseStage);

				if (this.part.stageBefore)
				{
					newInverseStage++;

					this.LogDebug("stageBefore={0}, new inverseStage={1}",
						this.part.stageBefore, newInverseStage);
				}

				if (this.part.manualStageOffset > -1)
				{
					newInverseStage = this.part.manualStageOffset;

					this.LogDebug("manualStageOffset={0}, new inverseStage={1}",
						this.part.manualStageOffset, newInverseStage);
				}

				newInverseStage = Mathf.Clamp(newInverseStage, 0, stagingInstance.stages.Count);

				this.LogDebug("inverseStage={0}", newInverseStage);
			}

			this.EnableAtStage(newInverseStage);

			if (HighLogic.LoadedSceneIsEditor)
			{
				if (this.part.symmetryCounterparts != null)
				{
					Part symCounterPart;
					for (int sIdx = 0; sIdx < this.part.symmetryCounterparts.Count; sIdx++)
					{
						symCounterPart = this.part.symmetryCounterparts[sIdx];

						ModuleStagingToggle symStagingToggle;
						if (symCounterPart != null && symCounterPart.tryGetFirstModuleOfType(out symStagingToggle))
						{
							symStagingToggle.EnableAtStage(newInverseStage);
						}
					}
				}
			}

			if (this.part.inverseStage == stagingInstance.stages.Count || this.part.childStageOffset > 0)
			{
				Staging.AddStageAt(this.part.inverseStage);
			}

			this.QueueStagingSort();
		}

		[KSPEvent(guiName = "Disable Staging")]
		public void DisableEvent()
		{
			if (this.part == null)
			{
				this.LogError("Cannot disable staging: part reference is null");
				return;
			}

			this.Disable();

			if (HighLogic.LoadedSceneIsEditor)
			{
				if (this.part.symmetryCounterparts != null)
				{
					Part symCounterPart;
					for (int sIdx = 0; sIdx < this.part.symmetryCounterparts.Count; sIdx++)
					{
						symCounterPart = this.part.symmetryCounterparts[sIdx];

						ModuleStagingToggle symStagingToggle;
						if (symCounterPart != null && symCounterPart.tryGetFirstModuleOfType(out symStagingToggle))
						{
							symStagingToggle.Disable();
						}
					}
				}
			}

			this.QueueStagingSort();
		}
		#endregion

		#region Utility Methods
		public void EnableAtStage(int newInverseStage)
		{
			this.LogDebug("Enabling");

			this.Events["EnableEvent"].active = false;
			this.Events["DisableEvent"].active = true;

			this.stagingEnabled = true;
			this.part.inverseStage = newInverseStage;

			this.part.stackIcon.CreateIcon();

			this.InvokeToggle();
		}

		public void Disable()
		{
			this.Events["EnableEvent"].active = true;
			this.Events["DisableEvent"].active = false;

			this.part.stackIcon.RemoveIcon();

			this.InvokeToggle();

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

			if (rootPart != null && this.part.hasAncestorPart(rootPart))
			{
				if (this.part.inverseStage < stagingInstance.stages.Count)
				{
					StageGroup ourGroup = stagingInstance.stages[this.part.inverseStage];

					if (ourGroup != null && ourGroup.icons.Count == 0)
					{
						Staging.DeleteStage(ourGroup);
					}
				}

				this.part.inverseStage = 0;
			}

			this.stagingEnabled = false;
		}

		protected void InvokeToggle()
		{
			if (this.OnToggle != null)
			{
				log.Append("\n\tWe have OnToggle subscribers; firing OnToggle event for them now.");
				this.OnToggle(this, this.stagingEnabled ? BoolArg.True : BoolArg.False);
			}

			log.Append("\n\tStaging switch done");
		}

		/// <summary>
		/// Queues a Staging sort, if none is queued already.
		/// </summary>
		#if DEBUG
		[KSPEvent(guiName="Queue Staging Sort", guiActiveEditor=true, guiActive=true, active=true)]
		public void QueueStagingSort()
		#else
		protected void QueueStagingSort()
		#endif
		{
			if (!stageSortQueued)
			{
				stageSortQueued = true;
				this.queuedStagingSort = true;
				this.LogDebug("Staging sort queued.");
			}
		}

		// Gets the inverse stage in which this decoupler's part will be removed from the craft
		protected int GetDecoupledStage()
		{
			Part _;
			return this.GetDecoupledStage(out _);
		}

		protected int GetDecoupledStage(out Part parentDecouplerPart)
		{
			int iStage = 0;

			parentDecouplerPart = null;

			this.LogDebug("this.part.parent={0}", this.part.parent == null ? "null" : this.part.parent.partInfo.title);

			Part ancestorPart = this.part;
			while (ancestorPart.parent != null)
			{
				ancestorPart = ancestorPart.parent;
				this.LogDebug("Checking if ancestorPart {0} is decoupler", ancestorPart);
				if (ancestorPart.isDecoupler())
				{
					this.LogDebug("ancestorPart {0} is decoupler, checking if staging is disabled", ancestorPart);

					ModuleStagingToggle tweakableStagingModule;

					if (ancestorPart.tryGetFirstModuleOfType<ModuleStagingToggle>(out tweakableStagingModule))
					{
						if (!tweakableStagingModule.stagingEnabled)
						{
							this.LogDebug("ancestorPart {0} staging is disabled, skipping", ancestorPart);
							continue;
						}
					}

					this.LogDebug("ancestorPart {0} staging is enabled, recording", ancestorPart);
					iStage = ancestorPart.inverseStage;
					parentDecouplerPart = ancestorPart;
					break;
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
