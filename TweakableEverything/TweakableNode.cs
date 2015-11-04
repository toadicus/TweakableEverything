// TweakableEverything, a TweakableEverything module
//
// TweakableNode.cs
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

using System;
using ToadicusTools.Extensions;

namespace TweakableEverything
{
	public class TweakableNode
	{
		private const string MODULE_NAME_KEY = "ModuleName";
		private const string FIELD_NAME_KEY = "FieldName";
		private const string TWEAK_TYPE_KEY = "TweakType";
		private const string IS_PERSISTENT_KEY = "IsPersistent";
		private const string MAX_VALUE_KEY = "MaxValue";

		private const string TWEAK_TYPE_FLOAT = "FloatRange";
		private const string TWEAK_TYPE_TOGGLE = "Toggle";

		public static TweakableNode Load(ConfigNode node)
		{
			TweakableNode tweak = new TweakableNode();

			tweak.ModuleName = node.GetValue(MODULE_NAME_KEY, string.Empty);

			tweak.FieldName = node.GetValue(FIELD_NAME_KEY, string.Empty);

			tweak.TweakableType = node.GetValue(TWEAK_TYPE_KEY, string.Empty);

			tweak.IsPersistent = node.GetValue(IS_PERSISTENT_KEY, false);

			tweak.InjectTweak();

			return tweak;
		}

		public string ModuleName
		{
			get;
			private set;
		}

		public string FieldName
		{
			get;
			private set;
		}

		public string TweakableType
		{
			get;
			private set;
		}

		public bool IsPersistent
		{
			get;
			private set;
		}

		private void InjectTweak()
		{
			ToadicusTools.Logging.PostLogMessage(
				"Loaded Tweakable '{0}' for PartModule '{1}'.\n" +
				"\tTweakableType: {2}\n" +
				"\tIsPersistent: {3}\n",
				this.FieldName,
				this.ModuleName,
				this.TweakableType,
				this.IsPersistent
			);
		}

		private TweakableNode() {}
	}
}

