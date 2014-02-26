// TweakableEverything © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

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
				if (this.animationState != null)
				{
					return this.animationState.normalizedTime;
				}
				return 0f;
			}
			protected set
			{
				if (this.animationState != null)
				{
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

			this.module.Fields["status"].guiActiveEditor = true;
			this.module.Fields["animSwitch"].guiActiveEditor = true;
		}

		public void Start()
		{

			if (!this.validScenes.Contains(HighLogic.LoadedScene))
			{
				return;
			}

			this.animation.wrapMode = this.wrapMode;
			this.animationState.normalizedTime = this.startTime;
			this.animationState.speed = (float)this.startDirection;
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
				(this.module as ModuleAnimateGeneric).animSwitch = animSwitch;
			}

			this.animation.Play(this.animationState.name);

			Tools.PostDebugMessage(this, string.Format("Skipped to {0}", this.normalizedTime, this.animationState.normalizedTime));
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