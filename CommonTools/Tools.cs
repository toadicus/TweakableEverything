// TweakableEverything © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TweakableEverything
{
	public static partial class Tools
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
		public static void PostDebugMessage(object Sender, params object[] args)
		{
			string Msg;

			Msg = string.Format(
				"{0}:\n\t{1}",
				Sender.GetType().Name,
				string.Join("\n\t", args.Select(a => (string)a).ToArray())
			);

			PostDebugMessage(Msg);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void DebugFieldsActivate(this PartModule partModule)
		{
			foreach (BaseField field in partModule.Fields)
			{
				field.guiActive = field.guiActiveEditor = true;
			}
		}

		public static bool Contains(this GameScenes[] haystack, GameScenes needle)
		{
			foreach (GameScenes item in haystack)
			{
				if (item == needle)
				{
					return true;
				}
			}

			return false;
		}

		public static bool TryParse<enumType>(string value, out enumType result)
			where enumType : struct, IConvertible, IComparable, IFormattable
		{
			try
			{
				if (!typeof(enumType).IsEnum)
				{
					throw new ArgumentException("result must be of an enum type");
				}

				result = (enumType)Enum.Parse(typeof(enumType), value);
				return true;
			}
			catch (Exception e)
			{
				Debug.LogWarning(string.Format("[{0}] failed to parse value '{1}': {2}",
					typeof(enumType).Name,
					value,
					e.Message
				));

				result = (enumType)Enum.GetValues(typeof(enumType)).GetValue(0);
				return false;
			}
		}

		public static double GetValue(this ConfigNode node, string name, double defaultValue)
		{
			if (node.HasValue(name))
			{
				double result;
				if (double.TryParse(node.GetValue(name), out result))
				{
					return result;
				}
			}
			return defaultValue;
		}

		public static float GetValue(this ConfigNode node, string name, float defaultValue)
		{
			if (node.HasValue(name))
			{
				float result;
				if (float.TryParse(node.GetValue(name), out result))
				{
					return result;
				}
			}
			return defaultValue;
		}

		public static int GetValue(this ConfigNode node, string name, int defaultValue)
		{
			if (node.HasValue(name))
			{
				int result;
				if (int.TryParse(node.GetValue(name), out 	result))
				{
					return result;
				}
			}
			return defaultValue;
		}

		public static UI_Control uiControlCurrent(this BaseField field)
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				return field.uiControlFlight;
			}
			else if (HighLogic.LoadedSceneIsEditor)
			{
				return field.uiControlEditor;
			}
			else
			{
				return null;
			}
		}
	}
}
