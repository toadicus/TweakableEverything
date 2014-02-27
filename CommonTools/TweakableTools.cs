// TweakableEverything © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using KSP.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TweakableEverything
{
	public static partial class Tools
	{
		public static void InitializeTweakable<T>(
			UI_FloatRange floatRange,
			ref float localField,
			ref float remoteField,
			float centerValue,
			float lowerMult,
			float upperMult,
			bool clobberEverywhere = false
		)
		{
			// If our field is uninitialized...
			if (localField == -1)
			{
				// ...fetch it from the remote field
				localField = centerValue;
			}

			Vector2 bounds;

			bounds = LoadBounds<T>();

			lowerMult = Mathf.Max(bounds.x, lowerMult, 0);
			upperMult = Mathf.Min(bounds.y, upperMult);

			// Set the bounds and increment for our tweakable range.
			floatRange.minValue = centerValue * lowerMult;
			floatRange.maxValue = centerValue * upperMult;
			floatRange.stepIncrement = Mathf.Pow(10f, Mathf.RoundToInt(Mathf.Log10(centerValue)) - 1);

			if (HighLogic.LoadedSceneIsFlight || clobberEverywhere)
			{
				// Clobber the remote field with ours.
				remoteField = localField;
			}
		}

		public static void InitializeTweakable<T>(
			UI_FloatRange floatRange,
			ref float localField,
			ref float remoteField,
			float centerValue,
			bool clobberEverywhere = false
		)
		{
			InitializeTweakable<T>(
				floatRange,
				ref localField,
				ref remoteField,
				centerValue,
				0f,
				2f,
				clobberEverywhere
			);
		}

		public static void InitializeTweakable<T>(
			UI_FloatRange floatRange,
			ref float localField,
			ref float remoteField,
			bool clobberEverywhere = false
		)
		{
			InitializeTweakable<T>(floatRange, ref localField, ref remoteField, remoteField, clobberEverywhere);
		}

		public static Vector2 LoadBounds<T>()
		{
			PluginConfiguration config = PluginConfiguration.CreateForType<T>();
			Vector2 bounds;

			config.load();

			bounds = config.GetValue("bounds", new Vector2(0f, 2f));

			config.save();

			return bounds;
		}
	}
}