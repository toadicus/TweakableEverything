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
	// Base class that facilitates the updating of prototype parts that were saved without TDN attach nodes.
	public abstract class TDNProtoUpdater : MonoBehaviour
	{
		// Some things should only happen once.
		protected bool runOnce = false;

		// Default array of names of parts with new AttachNodes from TDN.
		protected string[] AffectedParts = new string[]
		{
			"dockingPort1",
			"dockingPortLateral"
		};

		// We'll save the array as a single string.  This is the delimiter.
		protected char configStringSplitChar = ',';

		// When the plugin wakes up, load the rule list of affected parts from the XML file,
		// in case someone has changed it.
		public virtual void Awake()
		{
			var config = KSP.IO.PluginConfiguration.CreateForType<TDNProtoUpdater>();

			config.load();
			string AffectedPartsString = config.GetValue<string>("AffectedParts", string.Empty);
			if (AffectedPartsString != string.Empty)
			{
				this.AffectedParts = AffectedPartsString
					.Split(this.configStringSplitChar)
					.Select(s => s.Trim())
					.ToArray();
			}
		}

		// Check each of the affected parts snapshots to see if they are missing any AttachNodes.  If so, add them.
		protected virtual void UpdateProtoPartSnapshots(IEnumerable<ProtoPartSnapshot> affectedParts)
		{
			foreach (ProtoPartSnapshot affectedPart in affectedParts)
			{
				List<AttachNodeSnapshot> protoNodes = affectedPart.attachNodes;
				List<AttachNode> prefabNodes =
					PartLoader.getPartInfoByName(affectedPart.partName).partPrefab.attachNodes;

				IEnumerable<AttachNode> missingProtoNodes = prefabNodes
					.Where(pfN => !protoNodes.Select(prN => prN.id).Contains(pfN.id));

				Tools.PostDebugMessage(string.Format(
					"{0}: found affected part '{1}' in vessel '{2}'" +
					"\n\tprotoNodes: {3}" +
					"\n\tprefabNodes: {4}" +
					"\n\tmissingNodes: {5}",
					this.GetType().Name,
					affectedPart.partName,
					affectedPart.pVesselRef.vesselName,
					string.Join("; ", protoNodes.Select(n => n.id).ToArray()),
					string.Join("; ", prefabNodes.Select(n => n.id).ToArray()),
					string.Join("; ", missingProtoNodes.Select(n => n.id).ToArray())
				));

				if (missingProtoNodes.Count() > 0)
				{
					foreach (AttachNode missingProtoNode in missingProtoNodes)
					{
						Tools.PostDebugMessage(string.Format(
							"{0}: Adding new AttachNodeSnapshot '{1}'",
							this.GetType().Name,
							missingProtoNode.id
						));
						protoNodes.Add(
							new AttachNodeSnapshot(missingProtoNode.id + ", -1")
						);
					}

					KSPLog.print(string.Format(
						"{0}: after adding nodes to affected part '{1}' in vessel '{2}'" +
						"\n\tprotoNodes: {3}" +
						"\n\tprefabNodes: {4}",
						this.GetType().Name,
						affectedPart.partName,
						affectedPart.pVesselRef.vesselName,
						string.Join("; ", protoNodes.Select(n => n.id).ToArray()),
						string.Join("; ", prefabNodes.Select(n => n.id).ToArray())
					));
				}
			}
		}

		// When we're done, save the list of affected parts to the xml file.
		public virtual void OnDestroy()
		{
			var config = KSP.IO.PluginConfiguration.CreateForType<TDNProtoUpdater>();

			config.load();
			string AffectedPartsString = string.Join(", ", this.AffectedParts);
			config.SetValue("AffectedParts", AffectedPartsString);
			config.save();
		}
	}

	// SpaceCentre driver to update all affected parts in the current save.
	[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class TDNProtoUpdater_SpaceCentre : TDNProtoUpdater
	{
		// This runs once at the first update.  Any earlier and the current game doesn't have all the vessels yet.
		public void Update()
		{
			if (runOnce)
			{
				return;
			}
			runOnce = true;

			Tools.PostDebugMessage(this.GetType().Name + ": First Update.");

			// Fetch all the affected parts from the current game's list of prototype vessels.
			IEnumerable<ProtoPartSnapshot> affectedParts = HighLogic.CurrentGame.flightState.protoVessels
				.SelectMany(pv => pv.protoPartSnapshots)
				.Where(pps => this.AffectedParts.Contains(pps.partName));

			this.UpdateProtoPartSnapshots(affectedParts);
		}
	}

	// Flight driver to update all affected parts in the current flight cache.  This is for fixing when quickloading.
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class TDNProtoUpdater_Flight : TDNProtoUpdater
	{
		// Runs once when the plugin wakes up.  Any later and the vessel loader will throw an exception.
		public override void Awake()
		{
			// We only need to run this code if we're reloading from a saved cache, as in quickloading.
			if (FlightDriver.StartupBehaviour != FlightDriver.StartupBehaviours.RESUME_SAVED_CACHE)
			{
				return;
			}
				
			base.Awake();

			Tools.PostDebugMessage(this.GetType().Name + ": Awake started.  Flight StartupBehavior: " + FlightDriver.StartupBehaviour);

			// Fetch all the affected parts from the flight state cache.
			IEnumerable<ProtoPartSnapshot> affectedParts = FlightDriver.FlightStateCache.flightState.protoVessels
				.SelectMany(pv => pv.protoPartSnapshots)
				.Where(pps => this.AffectedParts.Contains(pps.partName));

			this.UpdateProtoPartSnapshots(affectedParts);
		}
	}
}
