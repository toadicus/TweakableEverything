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
		 * Build ALL the objects.
		 * */
		public ModuleTweakableDockingNode() : base()
		{
			this.StartOpened = false;
			this.startOpenedState = false;
			this.lastOpenState = false;
			// this.attachNodePosition = new Vector3(0, 1, 0);
		}
		// Tweakable property to determine whether the docking port should start opened or closed.
		[KSPField(guiName = "Start", isPersistant = true, guiActiveEditor = true),
		UI_Toggle(disabledText = "Closed", enabledText = "Opened")]
		public bool StartOpened;
		// Save the state here so we can tell if StartOpened has changed.
		protected bool startOpenedState;
		// Field that references the animationName of the ModuleAnimateGeneric doing the animating.
		[KSPField(isPersistant = false)]
		public string deployAnimationControllerName;
		// We will store our attachment node here.
		private AttachNode attachNode;

		[KSPField(isPersistant = true)]
		public Vector3 attachNodePosition;

		// Stores the open/closed state of the shield.
		protected bool lastOpenState;
		// True if we've loaded the AttachNode position from the nodeTransform, false otherwise.
		protected bool loadedNodePosition;
		// Store a reference to the ModuleAnimateGeneric module here, for toggling.
		public ModuleAnimateGeneric deployAnimationModule
		{
			get;
			protected set;
		}
		// Get the part attached on the docking end of things.
		protected Part attachedPart
		{
			get
			{
				return this.attachNode.attachedPart;
			}
			set
			{
				this.attachNode.attachedPart = value;
			}
		}

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
		// Go fetch the deployAnimationModule reference when we start.
		public override void OnStart(StartState st)
		{
			if (this.deployAnimationControllerName != null)
			{
				this.deployAnimationModule = base.part.Modules
					.OfType<ModuleAnimateGeneric>()
					.FirstOrDefault(m => m.animationName == this.deployAnimationControllerName);

				this.deployAnimationController = base.part.Modules.IndexOf(this.deployAnimationModule);
			}

			base.OnStart(st);

			Tools.PostDebugMessage(string.Format(
				"{0}: Started.  attachNodePosition={1}",
				this.GetType().Name,
				this.attachNodePosition
			));

			if (this.referenceAttachNode != string.Empty)
			{
				this.attachNode = base.part.findAttachNode(this.referenceAttachNode);
			}
			else
			{
				this.attachNode = new AttachNode();
				this.attachNode.id = "dockingPort";
				this.attachNode.position = this.attachNodePosition;
				this.attachNode.originalPosition = this.attachNode.position;
				this.attachNode.orientation = base.part.partTransform.InverseTransformDirection(Vector3.up);
				this.attachNode.originalOrientation = this.attachNode.orientation;
				this.attachNode.nodeType = AttachNode.NodeType.Stack;
				this.attachNode.size = 1;
				// base.part.attachNodes.Add(this.attachNode);
			}

			this.startOpenedState = this.StartOpened;
			base.part.attachRules.allowStack = this.StartOpened;

			Tools.PostDebugMessage(string.Format(
				"{0}: Started.  deployAnimationModule={1}",
				this.GetType().Name,
				this.deployAnimationModule
			));
		}

		public override void OnLoad(ConfigNode node)
		{
			Tools.PostDebugMessage(string.Format(
				"{0}: Loading." +
				"\n\tbase.part: {1}",
				this.GetType().Name,
				base.part
			));

			base.OnLoad(node);

			if (node.HasValue("attachNodePosition"))
			{
				this.attachNodePosition = KSPUtil.ParseVector3(node.GetValue("attachNodePosition"));
				this.loadedNodePosition = true;
			}
		}

		public override void OnSave(ConfigNode node)
		{
			Tools.PostDebugMessage(string.Format(
				"{0}: Saving.",
				this.GetType().Name
			));

			base.OnSave(node);

			if (node.HasValue("attachNodePosition"))
			{
				node.SetValue("attachNodePosition", KSPUtil.WriteVector(this.attachNodePosition));
			}
			else
			{
				node.AddValue("attachNodePosition", KSPUtil.WriteVector(this.attachNodePosition));
			}
		}

		// On Update, check to see if StartOpened has changed, and toggle the animation module if so.
		public void Update()
		{
			// If we're in the Editor and we have a deployAnimationModule...
			if (HighLogic.LoadedSceneIsEditor && this.deployAnimationModule != null)
			{
				if (this.lastOpenState != this.IsOpen)
				{
					this.lastOpenState = this.IsOpen;

					if (!loadedNodePosition && this.IsOpen)
					{
						this.attachNodePosition = base.part.transform.InverseTransformPoint(this.nodeTransform.position);
						this.attachNode.position = this.attachNodePosition;
						this.attachNode.originalPosition = this.attachNode.position;

						this.loadedNodePosition = true;
					}

					if (loadedNodePosition)
					{
						Tools.PostDebugMessage(string.Format(
							"{0}: node position loaded, IsOpen: {1}, part contains node: {2}",
							this.GetType().Name,
							this.IsOpen,
							base.part.attachNodes.Contains(this.attachNode)
						));
						if (this.IsOpen && !base.part.attachNodes.Contains(this.attachNode))
						{
							Tools.PostDebugMessage(this.GetType().Name + ": adding node");
							base.part.attachNodes.Add(this.attachNode);
						}
						if ((!this.IsOpen) && base.part.attachNodes.Contains(this.attachNode))
						{
							Tools.PostDebugMessage(this.GetType().Name + ": removing node");
							base.part.attachNodes.Remove(this.attachNode);
						}
					}

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
						"\n\tpart.transform inverse point nodeTransform: {12}" +
						"\n\tloadedNodePosition: {13}",
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
						base.part.transform.InverseTransformPoint(this.nodeTransform.position),
						this.loadedNodePosition
					));
				}

				// ...if StartOpened has changed...
				if (this.StartOpened != this.startOpenedState && !this.deployAnimationModule.IsInvoking("Toggle"))
				{
					Tools.PostDebugMessage(string.Format(
						"{0}: Toggling animation module: StartOpened={1}, startOpenedState={2}",
						this.GetType().Name,
						this.StartOpened,
						this.startOpenedState
					));

					// ...toggle the animation module
					this.deployAnimationModule.Toggle();

					// ...switch allowStack
					base.part.attachRules.allowStack = this.StartOpened;

					// If we are closing and have a part attached...
					if (this.StartOpened == false && this.attachedPart != null)
					{
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

