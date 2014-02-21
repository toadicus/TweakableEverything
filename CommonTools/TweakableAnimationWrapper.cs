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
			this.animation = animation;

			this.animationState = this.animation[animStateName];

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
			if (this.animation.isPlaying)
			{
				this.animation.Stop();
			}

			switch (position)
			{
				case PlayPosition.Beginning:
					this.normalizedTime = this.startTime;
					this.speed = (float)this.startDirection;
					break;
				case PlayPosition.End:
					this.normalizedTime = this.endTime;
					this.speed = -(float)this.startDirection;
					break;
				default:
					throw new NotImplementedException();
			}

			this.animation.Play(this.animationState.name);
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