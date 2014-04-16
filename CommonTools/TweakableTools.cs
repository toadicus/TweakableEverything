// CommonTools, a TweakableEverything module
//
// TweakableTools.cs
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
			Vector2 bounds;

			bounds = LoadBounds<T>();

			// If our field is uninitialized...
			if (localField == -1)
			{
				// ...fetch it from the remote field
				localField = centerValue;
			}

			lowerMult = Mathf.Max(lowerMult, bounds.x, 0);
			upperMult = Mathf.Max(lowerMult, Mathf.Min(upperMult, bounds.y));

			// Set the bounds and increment for our tweakable range.
			if (centerValue < 0)
			{
				floatRange.maxValue = centerValue * lowerMult;
				floatRange.minValue = centerValue * upperMult;
			}
			else
			{
				floatRange.minValue = centerValue * lowerMult;
				floatRange.maxValue = centerValue * upperMult;
			}

			floatRange.stepIncrement = Mathf.Pow(10f, Mathf.RoundToInt(Mathf.Log10(Mathf.Abs(centerValue))) - 1);

			localField = Mathf.Clamp(localField, floatRange.minValue, floatRange.maxValue);

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

			bounds = config.GetValue("bounds", new Vector2(float.NegativeInfinity, float.PositiveInfinity));

			config.save();

			return bounds;
		}
	}
}