// TweakableParachutes, a TweakableEverything module
//
// ModuleTweakableParachute.cs
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
using System.Linq;
using ToadicusTools;
using UnityEngine;

namespace TweakableEverything
{
	#if DEBUG
	public class ModuleTweakableParachute : DebugPartModule
	#else
	public class ModuleTweakableParachute : PartModule
	#endif
	{
		protected ModuleParachute chuteModule;

		[KSPField(isPersistant = true, guiName = "Deploy Speed", guiActiveEditor = true)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = .1f)]
		public float deploymentSpeed;

		[KSPField(isPersistant = true, guiName = "Semi-Deploy Speed", guiActiveEditor = true)]
		[UI_FloatRange(minValue = float.MinValue, maxValue = float.MaxValue, stepIncrement = .1f)]
		public float semiDeploymentSpeed;

		[KSPField(isPersistant = false)]
		public float lowerMult;

		[KSPField(isPersistant = false)]
		public float upperMult;

		[KSPField(isPersistant = false)]
		public float stepMult;

		public ModuleTweakableParachute()
		{
			this.deploymentSpeed = -1f;
			this.semiDeploymentSpeed = -1f;
			this.lowerMult = 0f;
			this.upperMult = 2f;
			this.stepMult = 1f;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.chuteModule = base.part.getFirstModuleOfType<ModuleParachute>();

			if (this.chuteModule == null)
			{
				return;
			}

			Tools.InitializeTweakable<ModuleTweakableParachute>(
				(UI_FloatRange)this.Fields["deploymentSpeed"].uiControlCurrent(),
				ref this.deploymentSpeed,
				ref this.chuteModule.deploymentSpeed,
				PartLoader.getPartInfoByName(base.part.partInfo.name).partPrefab.Modules
					.OfType<ModuleParachute>()
					.FirstOrDefault()
					.deploymentSpeed,
				this.lowerMult,
				this.upperMult,
				this.stepMult
			);

			Tools.InitializeTweakable<ModuleTweakableParachute>(
				(UI_FloatRange)this.Fields["semiDeploymentSpeed"].uiControlCurrent(),
				ref this.semiDeploymentSpeed,
				ref this.chuteModule.semiDeploymentSpeed,
				PartLoader.getPartInfoByName(base.part.partInfo.name).partPrefab.Modules
					.OfType<ModuleParachute>()
					.FirstOrDefault()
					.semiDeploymentSpeed,
				this.lowerMult,
				this.upperMult,
				this.stepMult
			);
		}

		public void LateUpdate()
		{
			if (this.chuteModule == null)
			{
				return;
			}
				
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (this.chuteModule.deploymentSpeed != this.deploymentSpeed)
				{
					this.chuteModule.deploymentSpeed = this.deploymentSpeed;
				}

				if (this.chuteModule.semiDeploymentSpeed != this.semiDeploymentSpeed)
				{
					this.chuteModule.semiDeploymentSpeed = this.semiDeploymentSpeed;
				}
			}
		}
	}
}
