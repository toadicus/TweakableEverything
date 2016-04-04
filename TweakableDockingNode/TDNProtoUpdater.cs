// TweakableDockingNode, a TweakableEverything module
//
// TDNProtoUpdater.cs
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
using System.Collections.Generic;
using UnityEngine;
using ToadicusTools.Extensions;

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
			this.Log("Waking up.");
			var config = KSP.IO.PluginConfiguration.CreateForType<TDNProtoUpdater>(null);

			config.load();
			string AffectedPartsString = config.GetValue<string>("AffectedParts", string.Empty);
			if (AffectedPartsString != string.Empty)
			{
				string[] partStrings = AffectedPartsString.Split(this.configStringSplitChar);

				for (int idx = 0; idx < partStrings.Length; idx++)
				{
					partStrings[idx] = partStrings[idx].Trim();
				}
			}
			this.Log("Awake.");
		}

		// Check each of the affected parts snapshots to see if they are missing any AttachNodes.  If so, add them.
		protected virtual void UpdateProtoPartSnapshots(IList<ProtoPartSnapshot> affectedParts)
		{
			ProtoPartSnapshot affectedPart;
			AttachNode prefabNode;
			AttachNodeSnapshot protoNode;
			List<AttachNodeSnapshot> protoNodes;
			List<AttachNode> prefabNodes;

			bool nodeIsMissing;

			System.Text.StringBuilder joinedProtoNodes = new System.Text.StringBuilder();
			System.Text.StringBuilder joinedPrefabNodes = new System.Text.StringBuilder();

			for (int pIdx = 0; pIdx < affectedParts.Count; pIdx++)
			{
				affectedPart = affectedParts[pIdx];

				protoNodes = affectedPart.attachNodes;
				prefabNodes = PartLoader.getPartInfoByName(affectedPart.partName).partPrefab.attachNodes;

				#if DEBUG
				joinedProtoNodes.Length = 0;
				for (int prNIdx = 0; prNIdx < protoNodes.Count; prNIdx++)
				{
					if (joinedProtoNodes.Length > 0)
					{
						joinedProtoNodes.Append(", ");
					}

					joinedProtoNodes.Append(protoNodes[prNIdx].id);
				}

				joinedPrefabNodes.Length = 0;
				for (int pfNIdx = 0; pfNIdx < prefabNodes.Count; pfNIdx++)
				{
					if (joinedPrefabNodes.Length > 0)
					{
						joinedPrefabNodes.Append(", ");
					}

					joinedPrefabNodes.Append(prefabNodes[pfNIdx].id);
				}

				KSPLog.print(string.Format(
					"{0}: before adding nodes to affected part '{1}' in vessel '{2}'" +
					"\n\tprotoNodes: {3}" +
					"\n\tprefabNodes: {4}",
					this.GetType().Name,
					affectedPart.partName,
					affectedPart.pVesselRef.vesselName,
					joinedProtoNodes.ToString(),
					joinedPrefabNodes.ToString()
				));
				#endif

				for (int pfIdx = 0; pfIdx < prefabNodes.Count; pfIdx++)
				{
					prefabNode = prefabNodes[pfIdx];

					nodeIsMissing = true;

					for (int pnIdx = 0; pnIdx < protoNodes.Count; pnIdx++)
					{
						protoNode = protoNodes[pnIdx];

						if (prefabNode.id == protoNode.id)
						{
							this.LogDebug(
								"{0}: Skipping prefab node '{1}', already in protoNodes",
								this.GetType().Name,
								prefabNode.id
							);

							nodeIsMissing = false;
							break;
						}
					}

					if (nodeIsMissing)
					{
						this.LogDebug(
							"{0}: Adding new AttachNodeSnapshot '{1}'",
							this.GetType().Name,
							prefabNode.id
						);
						protoNodes.Add(new AttachNodeSnapshot(prefabNode.id + ", -1"));
					}
				}

				joinedProtoNodes.Length = 0;
				for (int prNIdx = 0; prNIdx < protoNodes.Count; prNIdx++)
				{
					if (joinedProtoNodes.Length > 0)
					{
						joinedProtoNodes.Append(", ");
					}

					joinedProtoNodes.Append(protoNodes[prNIdx].id);
				}

				joinedPrefabNodes.Length = 0;
				for (int pfNIdx = 0; pfNIdx < prefabNodes.Count; pfNIdx++)
				{
					if (joinedPrefabNodes.Length > 0)
					{
						joinedPrefabNodes.Append(", ");
					}

					joinedPrefabNodes.Append(prefabNodes[pfNIdx].id);
				}

				KSPLog.print(string.Format(
					"{0}: after adding nodes to affected part '{1}' in vessel '{2}'" +
					"\n\tprotoNodes: {3}" +
					"\n\tprefabNodes: {4}",
					this.GetType().Name,
					affectedPart.partName,
					affectedPart.pVesselRef.vesselName,
					joinedProtoNodes.ToString(),
					joinedPrefabNodes.ToString()
				));
			}
		}

		// When we're done, save the list of affected parts to the xml file.
		public virtual void OnDestroy()
		{
			var config = KSP.IO.PluginConfiguration.CreateForType<TDNProtoUpdater>(null);

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
			if (
				runOnce ||
				HighLogic.CurrentGame == null ||
				HighLogic.CurrentGame.flightState == null ||
				HighLogic.CurrentGame.flightState.protoVessels == null
			)
			{
				return;
			}

			// Tools.PostDebugMessage(this.GetType().Name + ": First Update.");

			// Fetch all the affected parts from the current game's list of prototype vessels.
			List<ProtoPartSnapshot> affectedParts = new List<ProtoPartSnapshot>();

			ProtoVessel pv;
			ProtoPartSnapshot pps;

			for (int pvIdx = 0; pvIdx < HighLogic.CurrentGame.flightState.protoVessels.Count; pvIdx++)
			{
				pv = HighLogic.CurrentGame.flightState.protoVessels[pvIdx];


				for (int ppIdx = 0; ppIdx < pv.protoPartSnapshots.Count; ppIdx++)
				{
					pps = pv.protoPartSnapshots[ppIdx];

					for (int apIdx = 0; apIdx < this.AffectedParts.Length; apIdx++)
					{
						if (this.AffectedParts[apIdx] == pps.partName)
						{
							affectedParts.Add(pps);
						}
					}
				}
			}

			this.UpdateProtoPartSnapshots(affectedParts);

			runOnce = true;
		}
	}

	// Flight driver to update all affected parts in the current flight cache.  This is for fixing when quickloading.
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class TDNProtoUpdater_Flight : TDNProtoUpdater
	{
		public void Update()
		{
			// Don't run if Flight isn't ready yet.
			if (
				FlightDriver.FlightStateCache == null ||
				FlightDriver.FlightStateCache.flightState == null ||
				FlightDriver.FlightStateCache.flightState.protoVessels == null
			)
			{
				return;
			}

			// We only need to run this code if we're reloading from a saved cache, as in quickloading.
			if (FlightDriver.StartupBehaviour != FlightDriver.StartupBehaviours.RESUME_SAVED_CACHE)
			{
				return;
			}

			// Fetch all the affected parts from the flight state cache.
			/*IEnumerable<ProtoPartSnapshot> affectedParts = FlightDriver.FlightStateCache.flightState.protoVessels
				.SelectMany(pv => pv.protoPartSnapshots)
				.Where(pps => this.AffectedParts.Contains(pps.partName));*/

			List<ProtoPartSnapshot> affectedParts = new List<ProtoPartSnapshot>();

			ProtoVessel pv;
			ProtoPartSnapshot pps;

			for (int pvIdx = 0; pvIdx < FlightDriver.FlightStateCache.flightState.protoVessels.Count; pvIdx++)
			{
				pv = FlightDriver.FlightStateCache.flightState.protoVessels[pvIdx];


				for (int ppIdx = 0; ppIdx < pv.protoPartSnapshots.Count; ppIdx++)
				{
					pps = pv.protoPartSnapshots[ppIdx];

					for (int apIdx = 0; apIdx < this.AffectedParts.Length; apIdx++)
					{
						if (this.AffectedParts[apIdx] == pps.partName)
						{
							affectedParts.Add(pps);
						}
					}
				}
			}

			this.UpdateProtoPartSnapshots(affectedParts);

			runOnce = true;
		}
	}
}
