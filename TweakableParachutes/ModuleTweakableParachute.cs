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

		protected float prefabDeploySpeed;
		protected float prefabSemiDeploySpeed;

		protected float lastDeployFactor;
		protected float lastSemiDeployFactor;

		protected ModuleParachute.deploymentStates lastChuteState;

		[KSPField(isPersistant = true, guiName = "Deploy Factor", guiFormat = "×##0", guiActiveEditor = true)]
		[UI_FloatRange(minValue = 1f, maxValue = 100f, stepIncrement = 5f)]
		public float deploymentFactor;

		[KSPField(isPersistant = true, guiName = "Semi-Deploy Factor", guiFormat = "×##0", guiActiveEditor = true)]
		[UI_FloatRange(minValue = 1f, maxValue = 100f, stepIncrement = 5f)]
		public float semiDeploymentFactor;

		[KSPField(isPersistant = false)]
		public float maxFactor;

		public override void OnAwake()
		{
			base.OnAwake();

			this.deploymentFactor = 20f;
			this.semiDeploymentFactor = 1f;
			this.maxFactor = 100f;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.chuteModule = base.part.getFirstModuleOfType<ModuleParachute>();
			ModuleParachute prefabChuteModule = base.part.partInfo.partPrefab.getFirstModuleOfType<ModuleParachute>();

			if (this.chuteModule == null)
			{
				return;
			}

			this.chuteModule.Events["Repack"].guiActive = false;
			this.chuteModule.Events["Repack"].guiActiveEditor = false;
			this.chuteModule.Events["Repack"].guiActiveUnfocused = false;

			this.prefabDeploySpeed = prefabChuteModule.deploymentSpeed;
			this.prefabSemiDeploySpeed = prefabChuteModule.semiDeploymentSpeed;

			this.lastChuteState = this.chuteModule.deploymentState;

			this.chuteModule.Fields["deploymentSpeed"].guiActiveEditor = true;
			this.chuteModule.Fields["deploymentSpeed"].guiName = "Deploy Spd";
			this.chuteModule.Fields["deploymentSpeed"].guiFormat = "G3";

			this.chuteModule.Fields["semiDeploymentSpeed"].guiActiveEditor = true;
			this.chuteModule.Fields["semiDeploymentSpeed"].guiName = "Semi-Deploy Spd";
			this.chuteModule.Fields["semiDeploymentSpeed"].guiFormat = "G3";

			var deployField = this.Fields["deploymentFactor"].uiControlCurrent() as UI_FloatRange;
			var semiDeployField = this.Fields["semiDeploymentFactor"].uiControlCurrent() as UI_FloatRange;

			float step = Mathf.Pow(10f, (int)Mathf.Log10(this.maxFactor / 2f)) / 2f;

			deployField.maxValue = this.maxFactor;
			deployField.minValue = 1f;
			deployField.stepIncrement = step;

			semiDeployField.maxValue = this.maxFactor;
			semiDeployField.minValue = 1f;
			deployField.stepIncrement = step;
		}

		public void LateUpdate()
		{
			if (this.chuteModule == null)
			{
				return;
			}

			if (this.deploymentFactor != this.lastDeployFactor)
			{
				this.deploymentFactor = Mathf.Max(1f, this.deploymentFactor);

				this.chuteModule.deploymentSpeed = this.prefabDeploySpeed / this.deploymentFactor;

				this.lastDeployFactor = this.deploymentFactor;
			}

			if (this.semiDeploymentFactor != this.lastSemiDeployFactor)
			{
				this.semiDeploymentFactor = Mathf.Max(1f, this.semiDeploymentFactor);

				this.chuteModule.semiDeploymentSpeed = this.prefabSemiDeploySpeed /	this.semiDeploymentFactor;

				this.lastSemiDeployFactor = this.semiDeploymentFactor;
			}

			if (this.lastChuteState != this.chuteModule.deploymentState)
			{
				switch (this.chuteModule.deploymentState)
				{
					case ModuleParachute.deploymentStates.CUT:
						this.Events["RepackWrapper"].active = true;
						break;
					default:
						break;
				}

				this.lastChuteState = this.chuteModule.deploymentState;
			}
		}

		[KSPEvent(guiName = "Repack Chute", guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 4)]
		public void RepackWrapper()
		{
			this.chuteModule.Repack();

			this.chuteModule.staged = false;

			this.Events["RepackWrapper"].active = false;
		}
	}
}
