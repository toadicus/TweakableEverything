// TweakableDockingNode © 2014 toadicus
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

		public static void InitializeTweakable(
			UI_FloatRange floatRange,
			ref float localField,
			ref float remoteField,
			float centerValue,
			bool clobberEverywhere = false
		)
		{
			// If our field is uninitialized...
			if (localField == -1)
			{
				// ...fetch it from the remote field
				localField = centerValue;
			}

			// Set the bounds and increment for our tweakable range.
			floatRange.minValue = 0;
			floatRange.maxValue = centerValue * 2f;
			floatRange.stepIncrement = Mathf.Pow(10f, Mathf.RoundToInt(Mathf.Log10(centerValue)) - 1);

			if (HighLogic.LoadedSceneIsFlight || clobberEverywhere)
			{
				// Clobber the remote field with ours.
				remoteField = localField;
			}
		}

		public static void InitializeTweakable(
			UI_FloatRange floatRange,
			ref float localField,
			ref float remoteField,
			bool clobberEverywhere = false
		)
		{
			InitializeTweakable(floatRange, ref localField, ref remoteField, remoteField, clobberEverywhere);
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
	}
}
