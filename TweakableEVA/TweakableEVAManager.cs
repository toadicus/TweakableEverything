// TweakableEVA, a TweakableEverything module
//
// TweakableEVAManager.cs
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

namespace TweakableEVA
{
	[KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
	public class TweakableEVAManager : MonoBehaviour
	{
		private bool runOnce = true;

		public void Update()
		{
			if (runOnce && PartLoader.Instance.IsReady())
			{
				Tools.PostDebugMessage(this, "Looking for kerbalEVA.");
				{
					foreach (var loadedPart in PartLoader.LoadedPartsList)
					{
						if (loadedPart.name.ToLower() == "kerbaleva")
						{
							Tools.PostDebugMessage(this, "Found kerbalEVA");

							Part evaPart = loadedPart.partPrefab;

							ConfigNode tweakableEVANode = new ConfigNode("MODULE");
							tweakableEVANode.AddValue("name", typeof(ModuleTweakableEVA).Name);

							Tools.PostDebugMessage(this, "ModuleTweakableEVA prefab built.");

							try
							{
								evaPart.AddModule(tweakableEVANode);
							}
							catch (Exception ex)
							{
								Debug.Log(string.Format("TweakableEVAManager handled exception {0} while adding modules to kerbalEVA.",
									ex.GetType().Name
								));
							}

							Debug.Log("TweakableEVAManager added ModuleTweakableEVA to kerbalEVA part.");

							this.runOnce = false;

							break;
						}
					}
				}
			}
		}
	}
}

