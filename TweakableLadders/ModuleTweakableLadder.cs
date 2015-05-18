// TweakableLadders, a TweakableEverything module
//
// ModuleTweakableLadder.cs
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
using ToadicusTools;
using UnityEngine;

namespace TweakableEverything
{
	#if DEBUG
	public class ModuleTweakableLadder : DebugPartModule
	#else
	public class ModuleTweakableLadder : PartModule
	#endif
	{
		// Stores whether or not the wheel will start enabled.
		[KSPField(isPersistant = true, guiName = "Start", guiActive = false, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Extended", disabledText = "Retracted")]
		public bool startExtended;
		// Stores the last state of startEnabled so we can tell if it's changed.
		protected bool startExtendedState;

		protected RetractableLadder ladderModule;

		protected AnimationWrapper ladderAnimation;

		public ModuleTweakableLadder()
		{
			this.startExtended = false;
		}

		// Runs on PartModule startup.
		public override void OnStart(StartState state)
		{
			// Startup the PartModule stuff first.
			base.OnStart(state);

			// Set our state trackers to the opposite of our states, to force first-run updates.
			this.startExtendedState = !this.startExtended;

			// Fetch the solar ladder module from the part.
			if (this.part.tryGetFirstModuleOfType<RetractableLadder>(out this.ladderModule))
			{
				// Fetch the UnityEngine.Animation object from the solar ladder module.
				this.ladderAnimation = new AnimationWrapper(
					base.part.FindModelTransform(this.ladderModule.ladderAnimationRootName).animation,
					this.ladderModule.ladderRetractAnimationName,
					PlayDirection.Forward
				);

				// If we are in the editor and have an animation...
				if (HighLogic.LoadedSceneIsEditor && this.ladderAnimation != null)
				{
					// ...and disable Squad's tweakables, since they play out the animation
					this.ladderModule.Events["Extend"].guiActiveEditor = false;
					this.ladderModule.Events["Retract"].guiActiveEditor = false;
				}
			}
		}

		public void LateUpdate()
		{
			// If we're in the editor...
			if (HighLogic.LoadedSceneIsEditor && this.ladderModule != null)
			{
				// ...if startExtended has changed and we have an Animation...
				if (this.startExtendedState != this.startExtended && this.ladderAnimation != null)
				{
					// ...refresh startExtendedState
					this.startExtendedState = this.startExtended;

					// ...and if we are starting opened...
					if (this.startExtended)
					{
						// Yay debugging!
						Tools.PostDebugMessage(this, "Extending ladder.");

						// ...move the animation to the end with a "forward" play speed.
						this.ladderAnimation.SkipTo(PlayPosition.End);

						// ...flag the ladder as extended.
						this.ladderModule.StateName = "Extended";
					}
					// ...otherwise, we are starting closed...
					else
					{
						// Yay debugging!
						Tools.PostDebugMessage(this, "Retracting ladder.");

						// ...move the animation to the beginning with a "backward" play speed.
						this.ladderAnimation.SkipTo(PlayPosition.Beginning);

						// ...flag the ladder as retracted.
						this.ladderModule.StateName = "Retracted";
					}
				}
			}
		}
	}
}

