// TweakableAnimateGeneric, a TweakableEverything module
//
// ModuleTweakableAnimateGeneric.cs
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

namespace TweakableEverything
{
	#if DEBUG
	public class ModuleTweakableAnimateGeneric : DebugPartModule
	#else
	public class ModuleTweakableAnimateGeneric : PartModule
	#endif
	{
		// Stores whether or not the animation will start completed.
		[KSPField(isPersistant = true, guiName = "Start", guiActive = false, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Open", disabledText = "Closed")]
		public bool startCompleted;
		// Stores the last state of startCompleted so we can tell if it's changed.
		private bool startCompletedState;

		// Animation start position (from Beginning or End).
		[KSPField(isPersistant = false)]
		public string startPosition;

		// Animation start direction (from Backwards or Forwards).
		[KSPField(isPersistant = false)]
		public string startDirection;

		// Name for the "start completed" field.
		[KSPField(isPersistant = false)]
		public string fieldGuiName;

		// Text to display after fieldGuiName when startCompleted = true
		[KSPField(isPersistant = false)]
		public string fieldEnabledText;

		// Text to display after fieldGuiName when startCompleted = false
		[KSPField(isPersistant = false)]
		public string fieldDisabledText;

		// The animation wrapper
		private ToadicusTools.AnimationWrapper animationWrapper;

		// Declare enum values for parsing from string values
		private ToadicusTools.PlayDirection direction;

		// The animation wrapper used for easy interface
		// private TweakableAnimationWrapper animationWrapper;

		// Gets the BaseField object for startCompleted
		private BaseField startField
		{
			get
			{
				return (BaseField)this.Fields["startCompleted"];
			}
		}

		// Gets the UI_Toggle object for the current UI control underlying startCompleted.
		private UI_Toggle startUIControl
		{
			get
			{
				return (UI_Toggle)this.startField.uiControlCurrent();
			}
		}

		// Construct ALL the things.
		public ModuleTweakableAnimateGeneric()
		{
			this.startCompleted = false;
			this.startDirection = "Backward";
		}

		// Start when KSP tells us to start.
		public override void OnStart(StartState state)
		{
			ModuleAnimateGeneric magToWrap;

			// Only do any work if we're in the editor and we have an animation module.
			if (state == StartState.Editor &&
				this.part.tryGetFirstModuleOfType<ModuleAnimateGeneric>(out magToWrap))
			{
				// Start the base PartModule, just in case.
				base.OnStart(state);

				// If fielGuiName is neither null nor empty...
				if (!string.IsNullOrEmpty(this.fieldGuiName))
				{
					// ...assign it to the startCompleted gui name
					this.startField.guiName = this.fieldGuiName;
				}

				// If fieldEnabledText is neither null nor empty... 
				if (!string.IsNullOrEmpty(this.fieldEnabledText))
				{
					// ...assign it to the startCompleted control enabled text
					this.startUIControl.enabledText = this.fieldEnabledText;
				}

				// If fieldDisabledText is neither null nor empty... 
				if (!string.IsNullOrEmpty(this.fieldDisabledText))
				{
					// ...assign it to the startCompleted control disabled text
					this.startUIControl.disabledText = this.fieldDisabledText;
				}

				// Seed startCompletedState to ensure we run the startCompleted check on the first update
				this.startCompletedState = !this.startCompleted;

				// If we didn't get a module, or we can't parse enums from startPosition or startDirection...
				if (
					magToWrap == null ||
					!ToadicusTools.EnumTools.TryParse(this.startDirection, out this.direction)
				)
				{
					// ...disable the control and stop processing.
					this.Abort();
					return;
				}
				// ...otherwise...
				else
				{
					this.animationWrapper = new ToadicusTools.AnimationWrapper(magToWrap, direction);

					this.animationWrapper.module.Events["Toggle"].guiActiveEditor = false;
				}
			}

			this.LogDebug(
				"Started up.  animationModule={0}, startCompleted={1}, startPosition={2}, startDirection={3}",
				this.animationWrapper == null ? "null" : this.animationWrapper.ToString(),
				this.startCompleted,
				this.startPosition,
				this.startDirection
			);
		}

		// Runs at Unity's LateUpdate
		public void FixedUpdate()
		{
			// Only do any work if we're in the editor and have an animation wrapper.
			if (HighLogic.LoadedSceneIsEditor && this.animationWrapper != null)
			{
				if (this.animationWrapper.IsPlaying())
				{
					this.animationWrapper.animation.Stop();
				}

				// If startCompleted has changed...
				if (this.startCompletedState != this.startCompleted)
				{
					this.LogDebug("startCompleted has changed to {0}; let's work.", this.startCompleted);

					switch (this.startCompleted)
					{
						case false:
							this.animationWrapper.SkipTo(ToadicusTools.PlayPosition.End);
							break;
						case true:
							this.animationWrapper.SkipTo(ToadicusTools.PlayPosition.Beginning);
							break;
					}

					// ...reset startCompletedState to avoid re-running
					this.LogDebug("startCompletedState reseeded");
					this.startCompletedState = this.startCompleted;
				}
			}
		}

		// If we need to abort, disable the UI elements.
		public void Abort()
		{
			this.animationWrapper = null;
			this.startField.guiActiveEditor = false;
			this.startUIControl.controlEnabled = false;

			this.LogError("Startup aborted!");
		}
	}
}

