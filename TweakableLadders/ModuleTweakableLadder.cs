// TweakableLadders © 2014 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakableEverything
{
	public class ModuleTweakableLadder : PartModule
	{
		// Stores whether or not the wheel will start enabled.
		[KSPField(isPersistant = true, guiName = "Start", guiActive = false, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Extended", disabledText = "Retracted")]
		public bool startExtended;
		// Stores the last state of startEnabled so we can tell if it's changed.
		protected bool startExtendedState;

		protected RetractableLadder ladderModule;

		protected TweakableAnimationWrapper ladderAnimation;

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
			this.ladderModule = base.part.Modules.OfType<RetractableLadder>().FirstOrDefault();

			// Fetch the UnityEngine.Animation object from the solar ladder module.
			this.ladderAnimation = new TweakableAnimationWrapper(
				base.part.FindModelTransform(this.ladderModule.ladderAnimationRootName).animation,
				this.ladderModule.ladderRetractAnimationName,
				new GameScenes[] { GameScenes.EDITOR, GameScenes.SPH },
				WrapMode.ClampForever,
				TweakableAnimationWrapper.PlayPosition.End,
				TweakableAnimationWrapper.PlayDirection.Forward,
				1f
			);

			// If we are in the editor and have an animation...
			if (HighLogic.LoadedSceneIsEditor && this.ladderAnimation != null)
			{
				//  ...start the animation.
				this.ladderAnimation.Start();
			}
		}

		public void LateUpdate()
		{
			// If we're in the editor...
			if (HighLogic.LoadedSceneIsEditor)
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
						this.ladderAnimation.SkipTo(TweakableAnimationWrapper.PlayPosition.End);

						// ...flag the ladder as extended.
						this.ladderModule.StateName = "Extended";
					}
					// ...otherwise, we are starting closed...
					else
					{
						// Yay debugging!
						Tools.PostDebugMessage(this, "Retracting ladder.");

						// ...move the animation to the beginning with a "backward" play speed.
						this.ladderAnimation.SkipTo(TweakableAnimationWrapper.PlayPosition.Beginning);

						// ...flag the ladder as retracted.
						this.ladderModule.StateName = "Retracted";
					}
				}
			}
		}
	}
}

