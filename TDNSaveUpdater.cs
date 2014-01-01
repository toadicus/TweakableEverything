using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakableDockingNode
{
	[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class TDNSaveUpdater : MonoBehaviour
	{
		protected string GameSaveFolder;
		protected string SaveFileFullName;
		protected string persistentFileName;
		protected string quicksaveFileName;

		protected bool runOnce;

		protected Dictionary<string, int> AffectedParts;

		protected void Awake()
		{
			this.GameSaveFolder = string.Empty;
			this.persistentFileName = "persistent.sfs";
			this.quicksaveFileName = "quicksave.sfs";
			this.runOnce = false;

			this.AffectedParts = new Dictionary<string, int>
			{
				{"dockingPort1", 2},
				{"dockingPortLateral", 3}
			};
		}

		protected void Update()
		{
			ConfigNode RootConfigNode;

			if (this.runOnce)
			{
				return;
			}
			this.runOnce = true;

			bool changesMade = false;

			this.GameSaveFolder = HighLogic.SaveFolder;

			this.SaveFileFullName = string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), new string[] {KSPUtil.ApplicationRootPath, "saves", this.GameSaveFolder, this.persistentFileName});
			Tools.PostDebugMessage(string.Format("{0}: Loading file {1}", this.GetType().Name, this.SaveFileFullName));

			RootConfigNode = ConfigNode.Load(this.SaveFileFullName);

			ConfigNode GameConfigNode = RootConfigNode.GetNode("GAME");

			ConfigNode FlightStateNode = GameConfigNode.GetNode("FLIGHTSTATE");

			ConfigNode[] VesselNodes = FlightStateNode.GetNodes("VESSEL");

			foreach (ConfigNode VesselNode in VesselNodes)
			{
				ConfigNode[] PartNodes = VesselNode.GetNodes("PART");

				foreach (ConfigNode PartNode in PartNodes)
				{
					if (PartNode.HasValue("name"))
					{
						string partName = PartNode.GetValue("name");
						if (this.AffectedParts.ContainsKey(partName))
						{
							int prefabAttNCount = PartLoader.getPartInfoByName(partName).partPrefab.attachNodes.Count;

							Tools.PostDebugMessage(string.Format(
								"{0}: Found affected part '{1}'",
								this.GetType().Name,
								partName
							));

							string[] attNValues = PartNode.GetValues("attN");

							Tools.PostDebugMessage(string.Format(
								"{0}: attN values found: {1}, prefabattachNodes found: {2}",
								this.GetType().Name,
								attNValues.Length,
								prefabAttNCount
							));

							if (Array.FindIndex(attNValues, v => v.Contains("tdn")) == -1 && attNValues.Length < prefabAttNCount)
							{
								PartNode.AddValue("attN", "tdn,-1");
								changesMade = true;
							}
							else if (attNValues.Length != prefabAttNCount)
							{
								KSPLog.print(string.Format(
									"{0}: Found affected part '{1}', but its persistence status is not as expected." +
									"\n\tprefab attachNodes found: {2}" +
									"\n\tattNValues: {3}",
									this.GetType().Name,
									partName,
									prefabAttNCount,
									string.Join("; ", attNValues)
								));
							}
						}
					}
				}
			}

			if (changesMade)
			{
				RootConfigNode.Save(this.SaveFileFullName);
			}
		}
	}
}

