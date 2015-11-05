// TweakableControlSurfaces, a TweakableEverything module
//
// ModuleTweakableControlSurface.cs
//
// Copyright © 2015, toadicus
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
using ToadicusTools.Extensions;
using UnityEngine;

namespace TweakableControlSurfaces
{
	public class ModuleTweakableControlSurface : PartModule
	{
		private ModuleControlSurface ctrlModule;

		private float baseCtrlRange;
		private float throttleCache;

		[KSPField(
			isPersistant = true,
			guiName = "Control Limiter",
			guiFormat = "P2",
			guiActive = false,
			guiActiveEditor = true
		)]
		[UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.05f)]
		public float ctrlThrottle;

		public override void OnAwake()
		{
			base.OnAwake();

			this.ctrlThrottle = 1f;
			this.baseCtrlRange = 15f;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if (this.part.tryGetFirstModuleOfType(out this.ctrlModule))
			{
				this.baseCtrlRange = this.ctrlModule.ctrlSurfaceRange;
			}

			this.throttleCache = this.ctrlThrottle + 1f;
		}

		public void FixedUpdate()
		{
			if (
				HighLogic.LoadedSceneIsFlight &&
				this.throttleCache != this.ctrlThrottle
			)
			{
				this.throttleCache = this.ctrlThrottle;

				this.ctrlModule.ctrlSurfaceRange = this.baseCtrlRange * this.ctrlThrottle;
			}
		}
	}
}

