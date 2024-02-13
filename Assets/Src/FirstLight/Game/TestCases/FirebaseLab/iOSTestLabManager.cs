// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


#if UNITY_IOS
using System;
using System.Linq;
using UnityEngine;

namespace FirstLight.Game.TestCases.FirebaseLab
{
	internal class iOSTestLabManager : TestLabManager
	{
		public iOSTestLabManager()
		{
			CheckScenario();
		}


		private void CheckScenario()
		{
			if (!string.IsNullOrEmpty(Application.absoluteURL))
			{
				var uri = new Uri(Application.absoluteURL);
				if (uri.Scheme == "firebase-game-loop")
				{
					// this do not handle encoded strings not a problem for now
					var arguments = uri.Query
						.Substring(1) // Remove '?'
						.Split('&')
						.Select(q => q.Split('='))
						.ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());
					if (arguments.TryGetValue("scenario",out var scenario))
					{
						try
						{
							var castedValue = Convert.ToInt32(scenario);
							ScenarioNumber = castedValue;
							return;
						}
						catch (FormatException)
						{
						}
					}

					Debug.Log("Unable to parse firebase scenario using default value!");
					ScenarioNumber = 1;
				}
			}
		}

		public override void EarlyQuitApp()
		{
			Application.OpenURL("firebase-game-loop-complete://");
		}

		public override void NotifyHarnessTestIsComplete()
		{
		}
	}
}
#endif