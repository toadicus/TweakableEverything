using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakableDockingNode
{
	public abstract class TDNProtoUpdater : MonoBehaviour
	{
		protected bool runOnce = false;

		protected string[] AffectedParts = new string[]
		{
			"dockingPort1",
			"dockingPortLateral"
		};

		protected char configStringSplitChar = ',';

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

		public virtual void OnDestroy()
		{
			var config = KSP.IO.PluginConfiguration.CreateForType<TDNProtoUpdater>();

			config.load();
			string AffectedPartsString = string.Join(", ", this.AffectedParts);
			config.SetValue("AffectedParts", AffectedPartsString);
			config.save();
		}
	}

	[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class TDNProtoUpdater_SpaceCentre : TDNProtoUpdater
	{
		public void Update()
		{
			if (runOnce)
			{
				return;
			}
			runOnce = true;

			Tools.PostDebugMessage(this.GetType().Name + ": First Update.");

			IEnumerable<ProtoPartSnapshot> affectedParts = HighLogic.CurrentGame.flightState.protoVessels
				.SelectMany(pv => pv.protoPartSnapshots)
				.Where(pps => this.AffectedParts.Contains(pps.partName));

			this.UpdateProtoPartSnapshots(affectedParts);
		}
	}

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class TDNProtoUpdater_Flight : TDNProtoUpdater
	{
		public override void Awake()
		{
			if (FlightDriver.StartupBehaviour != FlightDriver.StartupBehaviours.RESUME_SAVED_CACHE)
			{
				return;
			}

			base.Awake();

			Tools.PostDebugMessage(this.GetType().Name + ": Awake started.  Flight StartupBehavior: " + FlightDriver.StartupBehaviour);

			IEnumerable<ProtoPartSnapshot> affectedParts = FlightDriver.FlightStateCache.flightState.protoVessels
				.SelectMany(pv => pv.protoPartSnapshots)
				.Where(pps => this.AffectedParts.Contains(pps.partName));

			this.UpdateProtoPartSnapshots(affectedParts);
		}
	}
}
