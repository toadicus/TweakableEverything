// TweakableEverything, a TweakableEverything module
//
// TweakableEverything.cs
//
// Copyright © 2015, toadicus
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
using ToadicusTools.Extensions;
using UnityEngine;

namespace TweakableEverything
{
	[KSPAddon(KSPAddon.Startup.Instantly, false)]
	public class TweakableEverything : MonoBehaviour
	{
		internal bool isActive;

		internal void Awake()
		{
			LoadingScreen loadingScreen;
			List<LoadingSystem> loadingSystems;
			LoadingSystem loadingSystem;
			int partLoaderIdx = -1;

			DontDestroyOnLoad(base.gameObject);

			loadingScreen = FindObjectOfType<LoadingScreen>();

			if (loadingScreen == null)
			{
				this.LogError("Could not find loadingScreen object; bailing out.");

				this.isActive = false;

				return;
			}
			else
			{
				this.isActive = true;
			}

			loadingSystems = loadingScreen.loaders;

			if (loadingSystems != null)
			{
				// Find PartLoader and come in just after it.
				for (int idx = 0; idx < loadingSystems.Count; idx++)
				{
					loadingSystem = loadingSystems[idx];

					if (loadingSystem is PartLoader)
					{
						partLoaderIdx = idx;
						break;
					}
				}

				// This should always happen, but who knows.
				if (partLoaderIdx > -1 && partLoaderIdx < loadingSystems.Count)
				{
					GameObject loaderObject = new GameObject("TweakableLoadingSystem");
					LoadingSystem tweakableLoader = loaderObject.AddComponent<TweakableLoadingSystem>();

					loadingSystems.Insert(partLoaderIdx + 1, tweakableLoader);
				}
			}
		}
	}
}

