// TweakableRCS, a TweakableEverything module
//
// ModuleTweakableRCS.cs
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
using ToadicusTools.Extensions;
using UnityEngine;

namespace TweakableRCS
{
	#if DEBUG
	public class ModuleTweakableRCS : DebugPartModule
	#else
	public class ModuleTweakableRCS : PartModule
	#endif
	{
		protected ModuleRCS RCSModule;

		[KSPField(isPersistant = true, guiName = "Pitch", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", scene = UI_Scene.Editor)]
		public bool enablePitch;
		[KSPField(isPersistant = true, guiName = "Roll", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", scene = UI_Scene.Editor)]
		public bool enableRoll;
		[KSPField(isPersistant = true, guiName = "Yaw", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", scene = UI_Scene.Editor)]
		public bool enableYaw;

		[KSPField(isPersistant = true, guiName = "X Translation", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", scene = UI_Scene.Editor)]
		public bool enableX;
		[KSPField(isPersistant = true, guiName = "Y Translation", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", scene = UI_Scene.Editor)]
		public bool enableY;
		[KSPField(isPersistant = true, guiName = "Z Translation", guiActive = true, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", scene = UI_Scene.Editor)]
		public bool enableZ;

		public ModuleTweakableRCS()
		{
			this.enablePitch = true;
			this.enableRoll = true;
			this.enableYaw = true;

			this.enableX = true;
			this.enableY = true;
			this.enableZ = true;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if (base.part.tryGetFirstModuleOfType<ModuleRCS>(out this.RCSModule))
			{
				this.RCSModule.enablePitch = this.enablePitch;
				this.RCSModule.enableRoll = this.enableRoll;
				this.RCSModule.enableYaw = this.enableYaw;

				this.RCSModule.enableX = this.enableX;
				this.RCSModule.enableY = this.enableY;
				this.RCSModule.enableZ = this.enableZ;
			}
		}
	}
}
