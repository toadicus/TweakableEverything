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
using ToadicusTools;
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

		// The animation module being wrapped
		private ModuleAnimateGeneric animationModule;
		private Animation animation;
		private AnimationState animState;

		// Declare enum values for parsing from string values
		private PlayPosition positionStart;
		private PlayDirection directionStart;

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
			this.startPosition = "Beginning";
			this.startDirection = "Backward";
		}

		// Start when KSP tells us to start.
		public override void OnStart(StartState state)
		{
			// Only do any work if we're in the editor and we have an animation module.
			if (state == StartState.Editor &&
			    this.part.tryGetFirstModuleOfType<ModuleAnimateGeneric>(out this.animationModule))
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
				if (this.animationModule == null ||
				    !Tools.TryParse(this.startPosition, out this.positionStart) ||
				    !Tools.TryParse(this.startDirection, out this.directionStart))
				{
					// ...disable the control and stop processing.
					this.Abort();
					return;
				}
				// ...otherwise...
				else
				{
					Animation[] animators = base.part.FindModelAnimators(this.animationModule.animationName);

					if (animators.Length > 0)
					{
						this.animation = animators[0];

						this.animState = this.animation[this.animationModule.animationName];
					}

					// ...build a new animation wrapper from the animation module and our parsed position and direction
					// data
					/*this.animationWrapper = new TweakableAnimationWrapper(
						this.animationModule,
						new GameScenes[] { GameScenes.EDITOR },
						WrapMode.ClampForever,
						positionStart,
						directionStart
					);*/

					// ...and start the animation
					// this.animationWrapper.Start();

					this.animationModule.Events["Toggle"].guiActiveEditor = false;
				}
			}

			this.LogDebug(
				"Started up.  animationModule={0}, startCompleted={1}, startPosition={2}, startDirection={3}",
				this.animationModule == null ? "null" : this.animationModule.ToString(),
				this.startCompleted,
				this.startPosition,
				this.startDirection
			);
		}

		// Runs at Unity's LateUpdate
		public void LateUpdate()
		{
			// Only do any work if we're in the editor and have an animation wrapper.
			if (
				HighLogic.LoadedSceneIsEditor && this.animationModule != null &&
				this.animation != null && this.animState != null
			)
			{
				if (this.animation.IsPlaying(this.animationModule.animationName))
				{
					this.animation.Stop(this.animationModule.animationName);
				}

				// If startCompleted has changed...
				if (this.startCompletedState != this.startCompleted)
				{
					this.LogDebug("startCompleted has changed to {0}; let's work.", this.startCompleted);

					switch (this.startCompleted)
					{
						case false:
							switch (this.directionStart)
							{
								case PlayDirection.Forward:
									this.animationModule.animTime = 1f;
									this.animState.normalizedTime = 1f;
									this.animState.speed = 1f;
									this.animation.Play(this.animationModule.animationName);
									break;
								case PlayDirection.Backward:
									this.animationModule.animTime = 0f;
									this.animState.normalizedTime = 0f;
									this.animState.speed = -1f;
									this.animation.Play(this.animationModule.animationName);
									break;
							}
							break;
						case true:
							switch (this.directionStart)
							{
								case PlayDirection.Forward:
									this.animationModule.animTime = 0f;
									this.animState.normalizedTime = 0f;
									this.animState.speed = -1f;
									this.animation.Play(this.animationModule.animationName);
									break;
								case PlayDirection.Backward:
									this.animationModule.animTime = 1f;
									this.animState.normalizedTime = 1f;
									this.animState.speed = 1f;
									this.animation.Play(this.animationModule.animationName);
									break;
							}
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
			this.animationModule = null;
			this.startField.guiActiveEditor = false;
			this.startUIControl.controlEnabled = false;

			this.LogDebug("Startup aborted");
		}

		public enum PlayPosition
		{
			Beginning = 0,
			End = 1
		}

		public enum PlayDirection
		{
			Forward = 1,
			Backward = -1
		}
	}
}

