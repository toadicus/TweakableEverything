// TweakableIntakes, a TweakableEverything module
//
// ModuleTweakableResourceIntake.cs
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

namespace TweakableIntakes
{
	#if DEBUG
	public class ModuleTweakableResourceIntake : DebugPartModule
	#else
	public class ModuleTweakableResourceIntake : PartModule
	#endif
	{
		protected ModuleResourceIntake intakeModule;

		public override void OnAwake()
		{
			this.Actions["ActivateAction"].active = false;
			this.Actions["DeactivateAction"].active = false;

			PartModule module;
			for (int idx = 0; idx < this.part.Modules.Count; idx++)
			{
				module = this.part.Modules[idx];

				if (module is ModuleResourceIntake)
				{
					this.intakeModule = module as ModuleResourceIntake;

					this.Actions["ActivateAction"].active = true;
					this.Actions["DeactivateAction"].active = true;
				}
			}
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if (HighLogic.LoadedSceneIsEditor)
			{
				this.intakeModule.Events["Activate"].active = !this.intakeModule.intakeEnabled;
				this.intakeModule.Events["Deactivate"].active = this.intakeModule.intakeEnabled;

				this.intakeModule.Events["Activate"].guiActiveEditor = true;
				this.intakeModule.Events["Deactivate"].guiActiveEditor = true;
			}
		}

		[KSPAction("Open Intake")]
		public void ActivateAction(KSPActionParam param)
		{
			if (this.intakeModule != null)
			{
				this.intakeModule.Activate();
			}
		}

		[KSPAction("Close Intake")]
		public void DeactivateAction(KSPActionParam param)
		{
			if (this.intakeModule != null)
			{
				this.intakeModule.Deactivate();
			}
		}
	}
}

