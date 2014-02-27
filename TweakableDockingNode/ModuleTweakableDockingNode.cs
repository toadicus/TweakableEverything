// TweakableDockingNode © 2014 toadicus
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
	public class ModuleTweakableDockingNode : PartModule
	{
		/*
		 * Ctor
		 * Build ALL the objects.
		 * */
		public ModuleTweakableDockingNode() : base()
		{
			this.StartOpened = false;
			this.startOpenedState = false;
			this.lastOpenState = false;
			this.AlwaysAllowStack = false;
			this.fuelCrossFeed = true;

			this.deployAnimationControllerName = string.Empty;
			this.TDNnodeName = string.Empty;

			this.acquireRange = -1;
			this.acquireForce = -1;
			this.acquireTorque = -1;
			this.undockEjectionForce = -1;
			this.minDistanceToReEngage = -1;
		}

		/*
		 * Fields
		 * */
		// Stores the ModuleDockingNode we're wrapping.
		protected ModuleDockingNode dockingNodeModule;

		// Tweakable property to determine whether the docking port should start opened or closed.
		[KSPField(guiName = "Start", isPersistant = true, guiActiveEditor = true),
		UI_Toggle(disabledText = "Closed", enabledText = "Opened")]
		public bool StartOpened;
		// Save the state here so we can tell if StartOpened has changed.
		protected bool startOpenedState;

		// Field that references the animationName of the ModuleAnimateGeneric doing the animating.
		[KSPField(isPersistant = false)]
		public string deployAnimationControllerName;
		// Wrap the animation.
		protected TweakableAnimationWrapper deployAnimation;

		// String containing the name of the AttachNode that we will toggle.
		[KSPField(isPersistant = false)]
		public string TDNnodeName;
		// We will store our attachment node here.
		protected AttachNode attachNode;

		// Some parts need to leave stacking allowed all the time.
		[KSPField(isPersistant = false)]
		public bool AlwaysAllowStack;

		// Stores the open/closed state of the shield.
		protected bool lastOpenState;

		[KSPField(isPersistant = true, guiName = "Crossfeed", guiActiveEditor = true, guiActive = true),
		UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
		public bool fuelCrossFeed;

		/*
		 * This functionality is disabled until Squad fixes tweakable tips.
		[KSPField(isPersistant = false, guiName = "Advanced Options", guiActiveEditor = true, guiActive = false)]
		[UI_Toggle(enabledText = "Shown", disabledText = "Hidden")]
		public bool showAdvanced;
		*/

		[KSPField(isPersistant = true, guiName = "Acquire Range (m)", guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float acquireRange;

		[KSPField(isPersistant = true, guiName = "Acquire Force (kN)", guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float acquireForce;

		[KSPField(isPersistant = true, guiName = "Acquire Torque (kN-m)", guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float acquireTorque;

		[KSPField(isPersistant = true, guiName = "Ejection Force (kN)", guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float undockEjectionForce;

		[KSPField(isPersistant = true, guiName = "Re-engage Distance (m)", guiActiveEditor = true, guiActive = false)]
		[UI_FloatRange(minValue = -1f, maxValue = float.MaxValue, stepIncrement = 1f)]
		public float minDistanceToReEngage;

		// Gets the base part's fuelCrossFeed value.
		public bool partCrossFeed
		{
			get
			{
				return base.part.fuelCrossFeed;
			}
			set
			{
				base.part.fuelCrossFeed = value;
			}
		}

		/*
		 * Properties
		 * */
		// Get the part attached on the docking end of things.
		protected Part attachedPart
		{
			get
			{
				if (this.attachNode == null)
				{
					return null;
				}

				return this.attachNode.attachedPart;
			}
		}

		// Get the open/closed state of the shield.
		protected bool IsOpen
		{
			get
			{
				if (this.deployAnimation == null)
				{
					return false;
				}
				else
				{
					return (this.deployAnimation.normalizedTime == 1);
				}
			}
		}

		/*
		 * Methods
		 * */
		// Runs when each new part is started.
		public override void OnStart(StartState st)
		{
			this.dockingNodeModule = (ModuleDockingNode)base.part.Modules["ModuleDockingNode"];

			// If we've loaded a deployAnimationControllerName from the cfg...
			if (this.deployAnimationControllerName != string.Empty)
			{
				// ...go get the module reference from the Part...
				this.deployAnimation = new TweakableAnimationWrapper(
					base.part.Modules
					.OfType<ModuleAnimateGeneric>()
					.First(m => m.animationName == this.deployAnimationControllerName),
					new GameScenes[] { GameScenes.EDITOR, GameScenes.SPH, GameScenes.FLIGHT },
					WrapMode.ClampForever,
					TweakableAnimationWrapper.PlayPosition.Beginning,
					TweakableAnimationWrapper.PlayDirection.Backward
				);
			}
			// ...otherwise, we don't have a shield...
			else
			{
				// ...so disable the option to start it open or closed.
				this.Fields["StartOpened"].guiActiveEditor = false;
			}

			// Start the underlying ModuleDockingNode.
			base.OnStart(st);

			ModuleDockingNode prefabModule = PartLoader.getPartInfoByName(this.part.partInfo.name).partPrefab.Modules
				.OfType<ModuleDockingNode>()
				.FirstOrDefault();

			Tools.InitializeTweakable<ModuleTweakableDockingNode>(
				(UI_FloatRange)this.Fields["acquireRange"].uiControlCurrent(),
				ref this.acquireRange,
				ref this.dockingNodeModule.acquireRange,
				prefabModule.acquireRange
			);

			Tools.InitializeTweakable<ModuleTweakableDockingNode>(
				(UI_FloatRange)this.Fields["acquireForce"].uiControlCurrent(),
				ref this.acquireForce,
				ref this.dockingNodeModule.acquireForce,
				prefabModule.acquireForce
			);

			Tools.InitializeTweakable<ModuleTweakableDockingNode>(
				(UI_FloatRange)this.Fields["acquireTorque"].uiControlCurrent(),
				ref this.acquireTorque,
				ref this.dockingNodeModule.acquireTorque,
				prefabModule.acquireForce
			);

			Tools.InitializeTweakable<ModuleTweakableDockingNode>(
				(UI_FloatRange)this.Fields["undockEjectionForce"].uiControlCurrent(),
				ref this.undockEjectionForce,
				ref this.dockingNodeModule.undockEjectionForce,
				prefabModule.undockEjectionForce
			);

			Tools.InitializeTweakable<ModuleTweakableDockingNode>(
				(UI_FloatRange)this.Fields["minDistanceToReEngage"].uiControlCurrent(),
				ref this.minDistanceToReEngage,
				ref this.dockingNodeModule.minDistanceToReEngage,
				prefabModule.minDistanceToReEngage
			);

			// If we have a tweakable AttachNode, use it.
			if (this.TDNnodeName != string.Empty)
			{
				this.attachNode = base.part.findAttachNode(this.TDNnodeName);
			}

			if (this.deployAnimation != null)
			{
				// Seed the start opened state and stack rules.  This is relevant mostly when loading a saved-open port.
				this.startOpenedState = this.StartOpened;
				base.part.attachRules.allowStack = this.StartOpened | this.AlwaysAllowStack;

				Tools.PostDebugMessage(this, string.Format("Set allowStack to {0}", base.part.attachRules.allowStack));

				// Seed the lastOpenState to the opposite of IsOpen, to force the node code to run once in the first update.
				this.lastOpenState = !this.IsOpen;
			}

			this.partCrossFeed = this.fuelCrossFeed;

			this.dockingNodeModule.Events["EnableXFeed"].guiActive = false;
			this.dockingNodeModule.Events["DisableXFeed"].guiActive = false;

			this.dockingNodeModule.Events["EnableXFeed"].guiActiveEditor = false;
			this.dockingNodeModule.Events["DisableXFeed"].guiActiveEditor = false;

			this.dockingNodeModule.Events["EnableXFeed"].active = false;
			this.dockingNodeModule.Events["DisableXFeed"].active = false;

			// Yay debugging!
			Tools.PostDebugMessage(string.Format(
				"{0}: Started with assembly version {4}." +
				"\n\tdeployAnimationModule={1}, attachNode={2}, TDNnodeName={3}, attachedPart={5}, fuelCrossFeed={6}",
				this.GetType().Name,
				this.deployAnimation,
				this.attachNode,
				this.TDNnodeName,
				this.GetType().Assembly.GetName().Version,
				this.attachedPart,
				this.fuelCrossFeed
			));
		}

		// Runs every LateUpdate, because that's how Unity rolls.
		// We're running at LateUpdate to avoid hiding Update, since ModuleDockingNode's Update is private and we
		// can't call it.
		public void LateUpdate()
		{
			// If we're in the Editor...
			if (HighLogic.LoadedSceneIsEditor)
			{
				// ...and if we have a deployAnimationModule...
				if (this.deployAnimation != null)
				{
					// If the Opened state of the port has changed since last update and we have an attachNode...
					if (this.attachNode != null && this.IsOpen != this.lastOpenState)
					{
						// ...set the last state to the current state
						this.lastOpenState = this.IsOpen;

						// ...and switch allowStack.
						base.part.attachRules.allowStack = this.IsOpen | this.AlwaysAllowStack;

						// Yay debugging!
						Tools.PostDebugMessage(string.Format(
							"{0}: IsOpen changed to: {1}, part contains node: {2}, allowStack: {3}",
							this.GetType().Name,
							this.IsOpen,
							base.part.attachNodes.Contains(this.attachNode),
							base.part.attachRules.allowStack
						));
					}

					// If StartOpened has changed...
					// TODO: Investigate if StartOpened can be compared against lastOpenState instead of startOpenedState.
					if (this.StartOpened != this.startOpenedState)
					{
						// Yay debugging!
						Tools.PostDebugMessage(string.Format(
							"{0}: Toggling animation module: StartOpened={1}, startOpenedState={2}",
							this.GetType().Name,
							this.StartOpened,
							this.startOpenedState
						));

						// ...toggle the animation module
						if (this.StartOpened)
						{
							this.deployAnimation.SkipTo(TweakableAnimationWrapper.PlayPosition.End);
						}
						else
						{
							this.deployAnimation.SkipTo(TweakableAnimationWrapper.PlayPosition.Beginning);
						}

						// If we are closing and have a part attached...
						if (this.StartOpened == false && this.attachedPart != null)
						{
							// Yay debugging!
							Tools.PostDebugMessage(string.Format(
								"{0}: Updating.  attachedPart={1}",
								this.GetType().Name,
								this.attachedPart
							));

							// ...select the part, putting it on the mouse.
							EditorLogic.fetch.PartSelected = this.attachedPart;
						}

						// ...and store the new StartOpened state.
						this.startOpenedState = this.StartOpened;
					}


					// ...if the port is closed and the attachNode icon is active...
					if ((!this.IsOpen) && this.attachNode != null && this.attachNode.icon != null)
					{
						// Yay debugging!
						Tools.PostDebugMessage(this.GetType().Name + ": removing node");

						// ...delete the node's icon.
						GameObject.Destroy(this.attachNode.icon);
						this.attachNode.icon = null;
					}
				}

				/*
				 * This functionality is disabled until Squad fixes tweakable tips.
				this.Fields["acquireRange"].guiActiveEditor = this.showAdvanced;
				this.Fields["acquireForce"].guiActiveEditor = this.showAdvanced;
				this.Fields["acquireTorque"].guiActiveEditor = this.showAdvanced;
				this.Fields["undockEjectionForce"].guiActiveEditor = this.showAdvanced;
				this.Fields["minDistanceToReEngage"].guiActiveEditor = this.showAdvanced;
				*/
			}

			// If we are in flight...
			if (HighLogic.LoadedSceneIsFlight)
			{
				// ...and if we have a deploy animation module and are ready...
				if (
					this.deployAnimation != null &&
					this.deployAnimation.module != null &&
					this.dockingNodeModule.state == "Ready"
				)
				{
					// ...and if we have an attached part...
					if (this.attachedPart != null)
					{
						// ...disable the deploy animation.
						this.deployAnimation.module.Events["Toggle"].active = false;
					}
					// ...otherwise...
					else
					{
						// ...enable the deploy animation.
						this.deployAnimation.module.Events["Toggle"].active = true;
					}
				}

				// ...and if the crossfeed status has changed...
				if (this.fuelCrossFeed != this.partCrossFeed)
				{
					// ...assign our crossfeed status to the part, since that's where it matters.
					this.partCrossFeed = this.fuelCrossFeed;
				}
			}
		}

		[KSPAction("Control from Here")]
		public void MakeReferenceTransformAction(KSPActionParam param)
		{
			if (this.dockingNodeModule.Events["MakeReferenceTransform"].active)
			{
				this.dockingNodeModule.MakeReferenceTransform();
			}
		}

		// Sometimes, when debugging, it's nice to have a "tell me everything" button.
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
				msg.Append(": ");
				msg.Append(prop.GetValue(this, null));
				msg.Append("\n\t");
			}

			foreach (System.Reflection.FieldInfo field in this.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
			{
				msg.Append(field.Name);
				msg.Append(": ");
				msg.Append(field.GetValue(this));
				msg.Append("\n\t");
			}

			foreach (System.Reflection.PropertyInfo prop in this.dockingNodeModule.GetType().GetProperties())
			{
				msg.Append(prop.Name);
				msg.Append(": ");
				msg.Append(prop.GetValue(this.dockingNodeModule, null));
				msg.Append("\n\t");
			}

			foreach (System.Reflection.FieldInfo field in this.dockingNodeModule.GetType().GetFields())
			{
				msg.Append(field.Name);
				msg.Append(": ");
				msg.Append(field.GetValue(this.dockingNodeModule));
				msg.Append("\n\t");
			}

			msg.Remove(msg.Length - 2, 2);

			Tools.PostDebugMessage(msg.ToString());
		}
		#endif
	}
}
