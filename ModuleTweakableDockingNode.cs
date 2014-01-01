// TweakableDockingNode © 2013 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakableDockingNode
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
		// Store a reference to the ModuleAnimateGeneric module here, for toggling.
		protected ModuleAnimateGeneric deployAnimationModule;

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
				if (this.deployAnimationModule == null)
				{
					return true;
				}
				else
				{
					return (this.deployAnimationModule.Progress == 1);
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
				this.deployAnimationModule = base.part.Modules
					.OfType<ModuleAnimateGeneric>()
					.FirstOrDefault(m => m.animationName == this.deployAnimationControllerName);

				// ...and reset the deployAnimationController index for ModuleDockingNode.
				this.dockingNodeModule.deployAnimationController = base.part.Modules.IndexOf(this.deployAnimationModule);
			}
			// ...otherwise, we don't have a shield...
			else
			{
				// ...so disable the option to start it open or closed.
				this.Fields["StartOpened"].guiActiveEditor = false;
			}

			// Start the underlying ModuleDockingNode.
			base.OnStart(st);

			// If we have a referenceAttachNode, use it.  This is for regular docking ports and not used yet.
			if (this.dockingNodeModule.referenceAttachNode != string.Empty)
			{
				this.attachNode = base.part.findAttachNode(this.dockingNodeModule.referenceAttachNode);
			}
			// Otherwise, if we have a tweakable AttachNode, use it.
			else if (this.TDNnodeName != string.Empty)
			{
				this.attachNode = base.part.findAttachNode(this.TDNnodeName);
			}

			if (this.deployAnimationModule != null)
			{
				// Seed the start opened state and stack rules.  This is relevant mostly when loading a saved-open port.
				this.startOpenedState = this.StartOpened;
				base.part.attachRules.allowStack = this.StartOpened | this.AlwaysAllowStack;

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
				this.deployAnimationModule,
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
				if (this.deployAnimationModule != null)
				{
					// If the Opened state of the port has changed since last update and we have an attachNode...
					if (/*this.lastOpenState != this.IsOpen && */this.attachNode != null)
					{
						// ...set the last state to the current state
						this.lastOpenState = this.IsOpen;

						// ...switch allowStack
						base.part.attachRules.allowStack = this.IsOpen | this.AlwaysAllowStack;

						// Yay debugging!
						Tools.PostDebugMessage(string.Format(
							"{0}: IsOpen changed to: {1}, part contains node: {2}",
							this.GetType().Name,
							this.IsOpen,
							base.part.attachNodes.Contains(this.attachNode)
						));

						// ...if the port is open and we don't have an attachNode...
						if (this.IsOpen && !base.part.attachNodes.Contains(this.attachNode))
						{
							// Yay debugging!
							Tools.PostDebugMessage(this.GetType().Name + ": adding node");

							// ...add the attachNode.
							// base.part.attachNodes.Add(this.attachNode);
						}
						// ...if the port is closed and we do have an attachNode...
						if ((!this.IsOpen) && base.part.attachNodes.Contains(this.attachNode))
						{
							// Yay debugging!
							Tools.PostDebugMessage(this.GetType().Name + ": removing node");

							// ...delete the node's icon...
							GameObject.Destroy(this.attachNode.icon);
							this.attachNode.icon = null;

							// ...and remove the node.
							// base.part.attachNodes.Remove(this.attachNode);
						}

						// Yay debugging!
						// This is just too much debugging.
						/*Tools.PostDebugMessage(string.Format(
						"{0}: Updating.  StartOpened={1}, startOpenedState={2}" +
						"\n\tattachRules.allowStack=({3})" +
						"\n\tattachNode: {4}" +
						"\n\tSortedShipList contains base.part: {5}" +
						"\n\tship contains base.part: {6}" +
						"\n\tpart contains attachNode: {7}" +
						"\n\tcontrolTransform position: {8}" +
						"\n\tnodeTransform position: {9}" +
						"\n\tpart.partTransform position: {10}" +
						"\n\tpart.transform position: {11}" +
						"\n\tpart.transform inverse point nodeTransform: {12}",
						this.GetType().Name,
						this.StartOpened,
						this.startOpenedState,
						base.part.attachRules.allowStack,
						string.Format(
							"id: {0}, position: {1}, size: {2}, nodeType: {3}",
							this.attachNode.id,
							this.attachNode.position,
							this.attachNode.size,
							this.attachNode.nodeType
						),
						EditorLogic.SortedShipList.Contains(base.part),
						EditorLogic.fetch.ship.Contains(base.part),
						base.part.attachNodes.Contains(this.attachNode),
						this.controlTransform.position,
						this.nodeTransform.position,
						base.part.partTransform.position,
						base.part.transform.position,
						base.part.transform.InverseTransformPoint(this.nodeTransform.position)
					));*/
					}

					// If StartOpened has changed...
					// TODO: Investigate if StartOpened can be compared against lastOpenState instead of startOpenedState.
					if (this.StartOpened != this.startOpenedState && !this.deployAnimationModule.IsInvoking("Toggle"))
					{
						// Yay debugging!
						Tools.PostDebugMessage(string.Format(
							"{0}: Toggling animation module: StartOpened={1}, startOpenedState={2}",
							this.GetType().Name,
							this.StartOpened,
							this.startOpenedState
						));

						// ...toggle the animation module
						this.deployAnimationModule.Toggle();

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
				}
			}

			// If we are in flight...
			if (HighLogic.LoadedSceneIsFlight)
			{
				// ...and if we have a deploy animation...
				if (this.deployAnimationModule != null)
				{
					// ...and if we have an attached part...
					if (this.attachedPart != null && this.dockingNodeModule.state == "Ready")
					{
						// ...disable the deploy animation.
						this.deployAnimationModule.Events["Toggle"].active = false;
					}
					// ...otherwise...
					else if (this.dockingNodeModule.state == "Ready")
					{
						// ...enable the deploy animation.
						this.deployAnimationModule.Events["Toggle"].active = true;
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

		// Sometimes, when debugging, it's nice to have a "tell me everything" button.
		#if DEBUG
		[KSPEvent(guiActive = true, guiName = "Debug Info")]
		public void DebugInfo()
		{
			System.Text.StringBuilder msg = new System.Text.StringBuilder();
			msg.Append(this.GetType().Name);
			msg.Append(": \n\t");

			foreach (System.Reflection.PropertyInfo prop in this.GetType().GetProperties())
			{
				msg.Append(prop.Name);
				msg.Append(": ");
				msg.Append(prop.GetValue(this, null));
				msg.Append("\n\t");
			}

			foreach (System.Reflection.FieldInfo field in this.GetType().GetFields())
			{
				msg.Append(field.Name);
				msg.Append(": ");
				msg.Append(field.GetValue(this));
				msg.Append("\n\t");
			}

			msg.Remove(msg.Length - 2, 2);

			Tools.PostDebugMessage(msg.ToString());
		}
		#endif
	}
}
