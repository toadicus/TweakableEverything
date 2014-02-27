// TweakableAnimateGeneric © 2014 toadicus
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
	public class ModuleTweakableAnimateGeneric : PartModule
	{
		// Stores whether or not the animation will start completed.
		[KSPField(isPersistant = true, guiName = "Start", guiActive = false, guiActiveEditor = true)]
		[UI_Toggle(enabledText = "Open", disabledText = "Closed")]
		public bool startCompleted;
		// Stores the last state of startCompleted so we can tell if it's changed.
		protected bool startCompletedState;

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
		protected ModuleAnimateGeneric animationModule;

		// The animation wrapper used for easy interface
		protected TweakableAnimationWrapper animationWrapper;

		// Gets the BaseField object for startCompleted
		protected BaseField startField
		{
			get
			{
				return (BaseField)this.Fields["startCompleted"];
			}
		}

		// Gets the UI_Toggle object for the current UI control underlying startCompleted.
		protected UI_Toggle startUIControl
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
			// Only do any work if we're in the editor.
			if (state == StartState.Editor)
			{
				// Declare enum values for parsing from string values
				TweakableAnimationWrapper.PlayPosition positionStart;
				TweakableAnimationWrapper.PlayDirection directionStart;

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

				// Fetch the ModuleAnimateGeneric module from the part
				this.animationModule = base.part.Modules.OfType<ModuleAnimateGeneric>().FirstOrDefault();

				// If we didn't get a module, or we can't parse enums from startPosition or startDirection...
				if (this.animationModule == null ||
				   !Tools.TryParse(this.startPosition, out positionStart) ||
				   !Tools.TryParse(this.startDirection, out directionStart))
				{
					// ...disable the control and stop processing.
					this.Abort();
					return;
				}
				// ...otherwise...
				else
				{
					// ...build a new animation wrapper from the animation module and our parsed position and direction
					// data
					this.animationWrapper = new TweakableAnimationWrapper(
						this.animationModule,
						new GameScenes[] { GameScenes.EDITOR, GameScenes.SPH },
						WrapMode.ClampForever,
						positionStart,
						directionStart
					);

					// ...and start the animation
					this.animationWrapper.Start();
				}
			}
		}

		// Runs at Unity's LateUpdate
		public void LateUpdate()
		{
			// Only do any work if we're in the editor and have an animation wrapper.
			if (HighLogic.LoadedSceneIsEditor && this.animationWrapper != null)
			{
				// If startCompleted has changed...
				if (this.startCompletedState != this.startCompleted)
				{
					// ...and if startCompleted is true...
					if (this.startCompleted)
					{
						// ...skip the animation to the ordinal end
						this.animationWrapper.SkipTo(TweakableAnimationWrapper.PlayPosition.End);
					}
					// ...otherwise, startCompleted is false...
					else
					{
						// ..so skip the animation to the beginning
						this.animationWrapper.SkipTo(TweakableAnimationWrapper.PlayPosition.Beginning);
					}

					// ...reset startCompletedState to avoid re-running
					this.startCompletedState = this.startCompleted;
				}
			}
		}

		// If we need to abort, disable the UI elements.
		public void Abort()
		{
			this.startField.guiActiveEditor = false;
			this.startUIControl.controlEnabled = false;
		}

		// Yay debugging!
		#if DEBUG
		[KSPEvent(guiName = "Debug Info", guiActive = true, guiActiveEditor = true)]
		public void DebugInfo()
		{
			System.Text.StringBuilder msg = new System.Text.StringBuilder();
			msg.Append(this.GetType().Name);
			msg.Append(": \n\t");

			foreach (System.Reflection.PropertyInfo prop in this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
			{
				msg.Append(prop.Name);
				msg.Append(string.Intern(": "));
				msg.Append(prop.GetValue(this, null));
				msg.Append(string.Intern("\n\t"));
			}

			foreach (System.Reflection.FieldInfo field in this.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
			{
				msg.Append(field.Name);
				msg.Append(string.Intern(": "));
				msg.Append(field.GetValue(this));
				msg.Append(string.Intern("\n\t"));
			}

			foreach (System.Reflection.PropertyInfo prop in this.animationModule.GetType().GetProperties())
			{
				msg.Append(string.Intern("animationModule."));
				msg.Append(prop.Name);
				msg.Append(string.Intern(": "));
				msg.Append(prop.GetValue(this.animationModule, null));
				msg.Append(string.Intern("\n\t"));
			}

			foreach (System.Reflection.FieldInfo field in this.animationModule.GetType().GetFields())
			{
				msg.Append(string.Intern("animationModule."));
				msg.Append(field.Name);
				msg.Append(string.Intern(": "));
				msg.Append(field.GetValue(this.animationModule));
				msg.Append(string.Intern("\n\t"));
			}

			foreach (System.Reflection.PropertyInfo prop in this.animationWrapper.GetType().GetProperties())
			{
				msg.Append(string.Intern("animationWrapper."));
				msg.Append(prop.Name);
				msg.Append(string.Intern(": "));
				msg.Append(prop.GetValue(this.animationWrapper, null));
				msg.Append(string.Intern("\n\t"));
			}

			foreach (System.Reflection.FieldInfo field in this.animationWrapper.GetType().GetFields())
			{
				msg.Append(string.Intern("animationWrapper."));
				msg.Append(field.Name);
				msg.Append(string.Intern(": "));
				msg.Append(field.GetValue(this.animationWrapper));
				msg.Append(string.Intern("\n\t"));
			}

			msg.Remove(msg.Length - 2, 2);

			Tools.PostDebugMessage(msg.ToString());
		}
		#endif
	}
}

