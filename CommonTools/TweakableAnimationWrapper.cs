// CommonTools, a TweakableEverything module
//
// TweakableAnimationWrapper.cs
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
using System;
using UnityEngine;

namespace TweakableEverything
{
	public class TweakableAnimationWrapper
	{
		/*
		 * Fields
		 * */
		protected Animation animation;
		protected AnimationState animationState;

		protected WrapMode wrapMode;
		protected PlayPosition startPosition;
		protected PlayDirection startDirection;

		protected float startTime;
		protected float endTime;

		protected float animationWeight;

		protected GameScenes[] validScenes;

		/*
		 * Properties
		 * */
		public PartModule module
		{
			get;
			protected set;
		}

		public float normalizedTime
		{
			get
			{
				// If we have an animation state...
				if (this.animationState != null)
				{
					// ...and if we're wrapping a ModuleAnimateGeneric, return its Progress property, because it lies to us.
					if (this.module != null && this.module is ModuleAnimateGeneric)
					{
						return (this.module as ModuleAnimateGeneric).Progress;
					}
					// ...otherwise, return the animation's normalizedTime.
					return Mathf.Clamp(this.animationState.normalizedTime, 0f, 1f);
				}

				// Return 0 if we're not wrapping anything yet.
				return 0f;
			}
			protected set
			{
				if (this.animationState != null)
				{
					if (this.module != null && this.module is ModuleAnimateGeneric)
					{
						(this.module as ModuleAnimateGeneric).animTime = value;
						(this.module as ModuleAnimateGeneric).SetScalar(value);
					}

					this.animationState.normalizedTime = value;
					return;
				}
				Debug.LogWarning("Tried to set normalizedTime for uninitialized TweakableAnimationWrapper.");
			}
		}

		public float speed
		{
			get
			{
				if (this.animationState != null)
				{
					return this.animationState.speed;
				}
				return 0f;
			}
			protected set
			{
				if (this.animationState != null)
				{
					this.animationState.speed = value;
					return;
				}
				Debug.LogWarning("Tried to set normalizedTime for uninitialized TweakableAnimationWrapper.");
			}
		}

		/*
		 * Methods
		 * */
		private TweakableAnimationWrapper() {}

		public TweakableAnimationWrapper(
			Animation animation,
			string animStateName,
			GameScenes[] validScenes,
			WrapMode wrapMode,
			PlayPosition startPosition,
			PlayDirection startDirection,
			float animationWeight
		)
		{
			Tools.PostDebugMessage(
				this,
				"Constructing new TweakableAnimationWrapper with:",
				string.Format("animation: {0}", animation),
				string.Format("animStateName: {0}", animStateName),
				string.Format("validScenes: {0}", validScenes),
				string.Format("wrapMode: {0}", wrapMode),
				string.Format("startPosition: {0}", startPosition),
				string.Format("startDirection: {0}", startDirection),
				string.Format("animationWeight: {0}", animationWeight)
			);
			this.animation = animation;

			this.animationState = this.animation[animStateName];

			Tools.PostDebugMessage(this, string.Format("animationState: {0}", this.animationState));

			this.validScenes = validScenes;
			this.wrapMode = wrapMode;

			this.startPosition = startPosition;

			switch (this.startPosition)
			{
				case PlayPosition.Beginning:
					this.startTime = 0f;
					this.endTime = 1f;
					break;
				case PlayPosition.End:
					this.startTime = 1f;
					this.endTime = 0f;
					break;
				default:
					throw new System.NotImplementedException();
			}

			this.startDirection = startDirection;

			this.animationWeight = animationWeight;
		}

		public TweakableAnimationWrapper(
			ModuleAnimateGeneric animationModule,
			GameScenes[] validScenes,
			WrapMode wrapMode,
			PlayPosition startPosition,
			PlayDirection startDirection
		) : this
		(
			animationModule.part.FindModelAnimators(animationModule.animationName)[0],
			animationModule.animationName,
			validScenes,
			wrapMode,
			startPosition,
			startDirection,
			1f
		)
		{
			Tools.PostDebugMessage(
				this,
				"Constructing new TweakableAnimationWrapper from:",
				string.Format("animationModule: {0}", animationModule)
			);
			this.module = animationModule;
		}

		public void Start()
		{

			if (!this.validScenes.Contains(HighLogic.LoadedScene))
			{
				return;
			}

			this.animation.wrapMode = this.wrapMode;
			this.animationState.weight = this.animationWeight;
		}

		public void SkipTo(PlayPosition position)
		{
			bool animSwitch;

			if (this.animation.isPlaying)
			{
				this.animation.Stop();
			}

			switch (position)
			{
				case PlayPosition.Beginning:
					Tools.PostDebugMessage(this, string.Format("Skipping to {0}", this.startTime));
					this.normalizedTime = this.startTime;
					this.speed = (float)this.startDirection;
					animSwitch = true;
					break;
				case PlayPosition.End:
					Tools.PostDebugMessage(this, string.Format("Skipping to {0}", this.endTime));
					this.normalizedTime = this.endTime;
					this.speed = -(float)this.startDirection;
					animSwitch = false;
					break;
				default:
					throw new NotImplementedException();
			}

			if (this.module != null && this.module is ModuleAnimateGeneric)
			{
				ModuleAnimateGeneric mag = (this.module as ModuleAnimateGeneric);
				mag.animSwitch = animSwitch;
			}

			this.animation.Play(this.animationState.name);

			Tools.PostDebugMessage(this, string.Format(
				"Skipped to {0} ({1})",
				this.normalizedTime,
				this.animationState.normalizedTime
			));
		}

		public void Toggle()
		{
			Tools.PostDebugMessage(this,
				"Toggling.",
				string.Format("normalizedTime = {0}", this.normalizedTime),
				string.Format("startTime = {0}", this.startTime)
			);

			if (this.module is ModuleAnimateGeneric)
			{
				(this.module as ModuleAnimateGeneric).Toggle();
			}

			if (this.normalizedTime == startTime)
			{
				this.SkipTo(PlayPosition.End);
			}
			else
			{
				this.SkipTo(PlayPosition.Beginning);
			}
	}

		public void Play()
		{
			this.animation.Play(this.animationState.name);
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