// CommonTools, a TweakableEverything module
//
// Tools.cs
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

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
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
		public static void PostDebugMessage(object Sender, string format, params object[] args)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(Sender.GetType().Name);
			sb.Append(": ");

			sb.AppendFormat(format, args);

			PostDebugMessage(sb.ToString());
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
