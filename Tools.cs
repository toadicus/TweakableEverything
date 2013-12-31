// TweakableDockingNode © 2013 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using System.Linq;
using System.Collections.Generic;

namespace TweakableDockingNode
{
	public static class Tools
	{
		private static ScreenMessage debugmsg = new ScreenMessage("", 4f, ScreenMessageStyle.UPPER_RIGHT);

		[System.Diagnostics.Conditional("DEBUG")]
		public static void PostDebugMessage(string Msg)
		{
			if (HighLogic.LoadedScene > GameScenes.SPACECENTER)
			{
				debugmsg.message = Msg;
				ScreenMessages.PostScreenMessage(debugmsg, true);
			}

			KSPLog.print(Msg);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void DumpModules(this Part part)
		{
			string msg = string.Format("{0}: dumping modules:\n\t", part.GetType().Name);

			foreach (PartModule module in part.Modules)
			{
				msg += string.Format("{0}: {1}\n\t",
					part.Modules.IndexOf(module).ToString(),
					module.ToString()
				);
			}

			msg.TrimEnd(new char[] { '\n', '\t' });

			Tools.PostDebugMessage(msg);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void DumpAttachNodes(this Part part)
		{
			string msg = string.Format("{0}: dumping attach nodes:\n\t", part.GetType().Name);

			foreach (AttachNode node in part.attachNodes)
			{
				msg += part.attachNodes.IndexOf(node).ToString() + ": " + node.ToString() + "\n\t";
			}

			msg.TrimEnd(new char[] { '\n', '\t' });

			Tools.PostDebugMessage(msg);
		}

		public static string ToString(this ModuleAnimateGeneric mod)
		{
			string str = string.Format("{0} (animationName: {1})",
				((object)mod).ToString(),
				mod.animationName
			);

			return str;
		}
	}
}