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
	public class ModuleTweakableDockingNode : ModuleDockingNode, ITargetable
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
			this.deployAnimationControllerName = string.Empty;
			this.TDNnodeName = string.Empty;
		}

		/*
		 * Fields
		 * */
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

		// Stores the open/closed state of the shield.
		protected bool lastOpenState;

		/*
		 * Properties
		 * */
		// Get the part attached on the docking end of things.
		protected Part attachedPart
		{
			get
			{
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
			// If we've loaded a deployAnimationControllerName from the cfg...
			if (this.deployAnimationControllerName != string.Empty)
			{
				// ...go get the module reference from the Part...
				this.deployAnimationModule = base.part.Modules
					.OfType<ModuleAnimateGeneric>()
					.FirstOrDefault(m => m.animationName == this.deployAnimationControllerName);

				// ...and reset the deployAnimationController index for ModuleDockingNode.
				this.deployAnimationController = base.part.Modules.IndexOf(this.deployAnimationModule);
			}

			// Start the underlying ModuleDockingNode.
			base.OnStart(st);

			// If we have a referenceAttachNode, use it.  This is for regular docking ports and not used yet.
			if (this.referenceAttachNode != string.Empty)
			{
				this.attachNode = base.part.findAttachNode(this.referenceAttachNode);
			}
			// Otherwise, if we have a tweakable AttachNode, use it.
			else if (this.TDNnodeName != string.Empty)
			{
				this.attachNode = base.part.findAttachNode(this.TDNnodeName);
			}

			// Seed the start opened state and stack rules.  This is relevant mostly when loading a saved-open port.
			this.startOpenedState = this.StartOpened;
			base.part.attachRules.allowStack = this.StartOpened;

			// Yay debugging!
			Tools.PostDebugMessage(string.Format(
				"{0}: Started.  deployAnimationModule={1}, attachNode={2}, TDNnodeName={3}",
				this.GetType().Name,
				this.deployAnimationModule,
				this.attachNode,
				this.TDNnodeName
			));
		}
		
		// Runs every LateUpdate, because that's how Unity rolls.
		// We're running at LateUpdate to avoid hiding Update, since ModuleDockingNode's Update is private and we
		// can't call it.
		public void LateUpdate()
		{
			// If we're in the Editor and we have a deployAnimationModule...
			if (HighLogic.LoadedSceneIsEditor && this.deployAnimationModule != null)
			{
				// If the Opened state of the port has changed since last update...
				if (this.lastOpenState != this.IsOpen)
				{
					// ...set the last state to the current state
					this.lastOpenState = this.IsOpen;

					// ...switch allowStack
					base.part.attachRules.allowStack = this.IsOpen;

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
						base.part.attachNodes.Add(this.attachNode);
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
						base.part.attachNodes.Remove(this.attachNode);
					}

					// Yay debugging!
					Tools.PostDebugMessage(string.Format(
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
					));
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
			// TODO: Investigate if this logic will ever be necessary at all.
			else if (HighLogic.LoadedSceneIsFlight && false)
			{
				this.isEnabled = (this.deployAnimationModule.Progress == 1);

				/*if (this.deployAnimationModule.Progress != 1)
				{
					this.captureRange = -1 * Math.Abs(this.captureRange);
					this.acquireRange = -1 * Math.Abs(this.acquireRange);
				}
				else
				{
					this.captureRange = Math.Abs(this.captureRange);
					this.acquireRange = Math.Abs(this.acquireRange);
				}*/

				if (this.attachedPart != null && this.part.Modules.Contains(this.deployAnimationModule.ClassID))
				{
					this.part.Modules.Remove(this.deployAnimationModule);

					Tools.PostDebugMessage(string.Format(
						"{0}: removed animation module, new count: {1}",
						this.GetType().Name,
						this.part.Modules.OfType<ModuleAnimateGeneric>().Count()
					));
				}

				if (this.attachedPart == null && !this.part.Modules.Contains(this.deployAnimationModule.ClassID))
				{
					this.part.Modules.Add(this.deployAnimationModule);

					Tools.PostDebugMessage(string.Format(
						"{0}: added animation module, new count: {1}",
						this.GetType().Name,
						this.part.Modules.OfType<ModuleAnimateGeneric>().Count()
					));
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
