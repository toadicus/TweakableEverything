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
	/// <summary>
	/// PartModule to facilitate the real-time toggling of Staging behavior for a subject Part.
	/// </summary>
	public class ModuleStagingToggle : PartModule
	{
		// A log for debugging.  Yay?
		private static Tools.DebugLogger log;

		// The FieldInfo behind the more-useful stagingInstance, see below
		private static FieldInfo stagingInstanceField;

		// We want to look at the staging list directly, so we reflectively get the Staging instance.
		private static Staging stagingInstance;

		// False for all ModuleStagingToggles after any OnStart, until Staging has a positive stage count.
		private static bool waitingForStaging;

		// True when any ModuleStagingToggle has queued a sort, so we only queue one.
		private static bool stageSortQueued = false;

		#region Interface Elements
		/// <summary>
		/// Store the tweaked staging enabled toggle for clobbering the value in the real decouplerModule.
		/// </summary> 
		[KSPField(isPersistant = true, guiName = "Staging", guiActive = false, guiActiveEditor = false)]
		public bool stagingEnabled;

		/// <summary>
		/// If true, this module will present toggle events in the editor
		/// </summary>
		[KSPField(isPersistant = false)]
		public bool activeInEditor;

		/// <summary>
		/// If true, this module will present toggle events in flight
		/// </summary>
		[KSPField(isPersistant = false)]
		public bool activeInFlight;

		/// <summary>
		/// If true, parts bearing this module will disable (or not enable) Staging behavior by default when placed
		/// </summary>
		[KSPField(isPersistant = false)]
		public bool defaultDisabled;

		/// <summary>
		/// A string naming the staging icon to use for this part
		/// </summary>
		[KSPField(isPersistant = false)]
		public string stagingIcon;

		/// <summary>
		/// Only one module should ever be running per part.
		/// </summary>
		public bool partPrimary;

		/// <summary>
		/// Only true when Enable/Disable Staging event has been clicked for this module and is being processed.
		/// </summary>
		public bool eventPrimary;

		/// <summary>
		/// Occurs when staging is toggled, and during startup
		/// </summary>
		public event ToggleEventHandler OnToggle;

		/// <summary>
		/// Toggle event handler.
		/// </summary>
		public delegate void ToggleEventHandler(object sender, ModuleStagingToggle.BoolArg args);
		#endregion

		// When true, forces a call to EnableAtStage or Disable in the next LateUpdate
		private bool forceUpdate;

		// True only after OnStart, to defer queuing a Staging sort
		private bool justStarted;

		// True when this module has queued a Staging sort, so we know when to actually run the sort
		private bool queuedStagingSort;

		#region LifeCycle Methods
		/// <summary>
		/// Runs during Unity's Awake cycle.
		/// </summary>
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

		/// <summary>
		/// Runs during Unity's Start cycle.
		/// </summary>
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

		/// <summary>
		/// Runs during Unity's LateUpdate cycle.
		/// </summary>
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
						this.DisableInStage(this.part.inverseStage);
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
				log.AppendFormat("\nCaught exception: {0}", x.ToString());
			}
			finally
			{
				if (stageSortQueued && this.queuedStagingSort)
				{
					log.AppendFormat("\nstagingSortQueued and this.queuedStagingSort still true after LateUpdate");
					log.AppendFormat("\n\tthis.partPrimary={0}", this.partPrimary);
					log.AppendFormat("\n\tstagingInstance={0}",
						stagingInstance == null ? "null" : stagingInstance.ToString());
					log.AppendFormat("\n\twaitingForStaging={0}", waitingForStaging);
					if (stagingInstance != null && stagingInstance.stages != null)
					{
						log.AppendFormat("\n\t\tstagingInstance.stages.Count={0}", stagingInstance.stages.Count);
					}
					log.AppendFormat("\n\tStaging.stackLocked={0}", Staging.stackLocked);

					printLog = true;
				}

				if (printLog)
				{
					log.Print(false);
				}
			}
			#endif
		}

		/// <summary>
		/// Runs when Unity destroys this object
		/// </summary>
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
		/// <summary>
		/// KSPEvent handler to enable staging on this part.
		/// </summary>
		[KSPEvent(guiName = "Enable Staging")]
		public void EnableEvent()
		{
			if (this.part == null)
			{
				this.LogError("Cannot enable staging: part reference is null");
				return;
			}

			this.eventPrimary = true;

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
				this.LogDebug("In editor; checking if we need to enable symmetry partners");

				if (this.part.symmetryCounterparts != null)
				{
					this.LogDebug("Enabling {0} symmetry partners", this.part.symmetryCounterparts.Count);

					Part symCounterPart;
					for (int sIdx = 0; sIdx < this.part.symmetryCounterparts.Count; sIdx++)
					{
						symCounterPart = this.part.symmetryCounterparts[sIdx];

						ModuleStagingToggle symStagingToggle;
						if (symCounterPart != null && symCounterPart.tryGetFirstModuleOfType(out symStagingToggle))
						{
							if (!symStagingToggle.eventPrimary)
							{
								symStagingToggle.EnableAtStage(newInverseStage);
							}
						}
					}
				}
			}

			if (this.part.inverseStage == stagingInstance.stages.Count || this.part.childStageOffset > 0)
			{
				this.LogDebug("inverseStage equals stage count or we have a childStageOffset, adding new stage");
				Staging.AddStageAt(this.part.inverseStage);
			}

			this.LogDebug("Queueing sort");
			this.QueueStagingSort();

			this.eventPrimary = false;

			this.LogDebug("Enabled");
		}

		/// <summary>
		/// KSPEvent handler to disable staging on this part.
		/// </summary>
		[KSPEvent(guiName = "Disable Staging")]
		public void DisableEvent()
		{
			if (this.part == null)
			{
				this.LogError("Cannot disable staging: part reference is null");
				return;
			}

			this.eventPrimary = true;

			this.LogDebug("Disable Staging clicked");

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
							if (!symStagingToggle.eventPrimary)
							{
								symStagingToggle.DisableInStage(0);
							}
						}
					}
				}
			}

			// Use the sekrit password to avoid changing our inverseStage here.
			this.DisableInStage(int.MinValue);

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

			this.LogDebug("Got root part: {0}", rootPart);

			if (rootPart != null && this.part.hasAncestorPart(rootPart))
			{
				this.LogDebug("We have root as ancenstor, let's try to delete our stage." +
					"\n\tthis.part.inverseStage={0}, stagingInstance.stages.Count={1}",
					this.part.inverseStage, stagingInstance.stages.Count
				);

				if (this.part.inverseStage < stagingInstance.stages.Count)
				{
					StageGroup ourGroup = stagingInstance.stages[this.part.inverseStage];

					this.LogDebug("ourGroup={0}, ourGroup.icons.Count={1}", ourGroup, ourGroup.icons.Count);

					if (ourGroup != null && ourGroup.icons.Count == 0)
					{
						this.LogDebug("Deleting stage");

						Staging.DeleteStage(ourGroup);
					}
				}
			}

			// Since we didn't set it in DisableInStage, set it here.
			this.part.inverseStage = 0;

			this.QueueStagingSort();

			this.eventPrimary = false;
		}
		#endregion

		#region Utility Methods
		/// <summary>
		/// Utility method for enabling staging on this part at the designated inverseStage
		/// </summary>
		/// <param name="newInverseStage">The inverse stage (top down) at which to place this part</param>
		public void EnableAtStage(int newInverseStage)
		{
			this.LogDebug("Enabling");

			this.Events["EnableEvent"].active = false;
			this.Events["DisableEvent"].active = true;

			this.stagingEnabled = true;

			// int.MinValue is our sekrit key to not assign the stage
			if (int.MinValue != newInverseStage)
			{
				this.part.inverseStage = newInverseStage;
			}

			this.part.stackIcon.CreateIcon();

			this.InvokeToggle();
		}

		/// <summary>
		/// Utility method for disabling staging on this part. 
		/// </summary>
		public void DisableInStage(int newInverseStage)
		{
			this.LogDebug("Disabling");

			this.Events["EnableEvent"].active = true;
			this.Events["DisableEvent"].active = false;

			this.part.stackIcon.RemoveIcon();

			this.stagingEnabled = false;

			// int.MinValue is our sekrit key to not assign the stage
			if (int.MinValue != newInverseStage)
			{
				this.part.inverseStage = newInverseStage;
			}

			this.InvokeToggle();
		}

		private void InvokeToggle()
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
		private void QueueStagingSort()
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
		private int GetDecoupledStage()
		{
			Part _;
			return this.GetDecoupledStage(out _);
		}

		private int GetDecoupledStage(out Part parentDecouplerPart)
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
		private void onPartAttach(GameEvents.HostTargetAction<Part, Part> data)
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

		private void onUndock(EventReport data)
		{
			if (data.origin != null)
			{
				this.LogDebug("{0}: Running onUndock.", this.part.craftID);
				this.onPartEvent(data.origin);
			}
		}

		private void onPartCouple(GameEvents.FromToAction<Part, Part> data)
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

		private void onVesselChange(Vessel data)
		{
			if (this.part.vessel != null && data.id == this.part.vessel.id)
			{
				this.LogDebug("{0}: Running onVesselChange.", this.part.craftID);
				this.forceUpdate = true;
			}
		}

		private void onPartEvent(Part data)
		{
			if (data.vessel != null && data.vessel.id == this.part.vessel.id)
			{
				this.forceUpdate = true;
			}
		}
		#endregion

		/// <summary>
		/// Bool argument for Events
		/// </summary>
		public class BoolArg : EventArgs
		{
			/// <param name="arg">Argument</param>
			public static explicit operator bool(BoolArg arg)
			{
				return arg.Value;
			}

			/// <summary>
			/// A <c>BoolArg</c> representing the <see cref="bool"/> value true
			/// </summary>
			public static readonly BoolArg True;

			/// <summary>
			/// A <c>BoolArg</c> representing the <see cref="bool"/> value false
			/// </summary>
			public static readonly BoolArg False;

			static BoolArg()
			{
				True = new BoolArg(true);
				False = new BoolArg(false);
			}

			/// <summary>
			/// Gets or sets the boolean value of this object
			/// </summary>
			public bool Value { get; private set; }

			private BoolArg() {}

			/// <summary>
			/// Initializes a new instance of the <see cref="TweakableEverything.ModuleStagingToggle.BoolArg"/> class.
			/// </summary>
			/// <param name="value">Boolean value of the new object</param>
			public BoolArg(bool value) : base()
			{
				this.Value = value;
			}
		}
	}
}
