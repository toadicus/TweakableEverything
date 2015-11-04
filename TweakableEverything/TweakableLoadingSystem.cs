// TweakableEverything, a TweakableEverything module
//
// TweakableLoadingSystem.cs
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TweakableEverything
{
	public class TweakableLoadingSystem : LoadingSystem
	{
		private const string TWEAKABLE_NODE_KEY = "TWEAKABLE";

		public static TweakableLoadingSystem Instance
		{
			get;
			private set;
		}

		public static bool Ready
		{
			get
			{
				if (Instance != null)
				{
					return Instance.IsReady();
				}

				return false;
			}
		}

		private List<ConfigNode> tweakableConfigs;
		private List<TweakableNode> tweakableNodes;

		private bool fetchedNodes;

		private bool ready;

		public override bool IsReady()
		{
			return this.ready;
		}

		public override float ProgressFraction()
		{
			float progressFraction = 0f;

			if (fetchedNodes)
			{
				progressFraction += .05f;
			}

			if (tweakableConfigs.Count > 0)
			{
				progressFraction += .95f * (float)tweakableNodes.Count / (float)tweakableConfigs.Count;
			}

			return progressFraction;
		}

		public override string ProgressTitle()
		{
			return "Inserting Tweakables";
		}

		public override void StartLoad()
		{
			base.StartCoroutine(this.LoadTweakables());
		}

		private IEnumerator LoadTweakables()
		{
			while (!this.FetchNodes())
			{
				yield return null;
			}

			this.fetchedNodes = true;

			yield return null;

			for (int idx = 0; idx < this.tweakableConfigs.Count; idx++)
			{
				ConfigNode config = this.tweakableConfigs[idx];

				TweakableNode tweak = TweakableNode.Load(config);

				this.tweakableNodes.Add(tweak);

				yield return null;
			}

			this.ready = true;
		}

		private bool FetchNodes()
		{
			if (GameDatabase.Instance == null || !GameDatabase.Instance.IsReady())
			{
				return false;
			}

			IEnumerable<UrlDir.UrlConfig> allConfigs = GameDatabase.Instance.root.AllConfigs;
			IEnumerator<UrlDir.UrlConfig> configEnumerator = allConfigs.GetEnumerator();

			UrlDir.UrlConfig config;

			while (configEnumerator.MoveNext())
			{
				config = configEnumerator.Current;

				if (config.type == TWEAKABLE_NODE_KEY)
				{
					this.tweakableConfigs.Add(config.config);
				}
			}

			return true;
		}

		private void Awake()
		{
			TweakableLoadingSystem.Instance = this;

			this.tweakableConfigs = new List<ConfigNode>();
			this.fetchedNodes = false;
			this.ready = false;

			DontDestroyOnLoad(base.gameObject);
		}

		private void OnDestroy()
		{
			TweakableLoadingSystem.Instance = null;
		}
	}
}