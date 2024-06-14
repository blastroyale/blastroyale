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

using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.TestCases.FirebaseLab
{
	// This class serves as the entry point for the Firebase Test Lab, described in detail at
	// https://firebase.google.com/docs/test-lab/game-loop
	// It will check to see if the application was launched with the intent to test, and handle
	// the parameters that were passed in.
	// Note this is only supported at the moment on Android platforms.
	public abstract class TestLabManager
	{
		public const int NoScenarioPresent = -1;
		public int ScenarioNumber = NoScenarioPresent;

		public bool IsTestingScenario
		{
			get { return ScenarioNumber > NoScenarioPresent; }
		}

		// Notify the harness that the testing scenario is complete.  This will cause the app to quit
		public abstract void NotifyHarnessTestIsComplete();

		public static TestLabManager Instantiate()
		{
			TestLabManager manager;
#if UNITY_ANDROID && !UNITY_EDITOR
      manager = new AndroidTestLabManager();
#elif UNITY_IOS
		manager = new iOSTestLabManager();
#else
			manager = new DummyTestLabManager();
#endif // UNITY_ANDROID
			return manager;
		}

		public virtual void EarlyQuitApp()
		{
			Application.Quit();
		}

		public string Version()
		{
			return VersionUtils.BuildNumber == "0" ? VersionUtils.Commit : VersionUtils.BuildNumber;
		}

		public virtual void WriteLine(string text)
		{
			Debug.Log("TesterLog: " + text);
		}

		public void WriteHeaders()
		{
			WriteLine(
				"timestamp," +
				"eventName," +
				"minFpsOrResultBool," +
				"avgFpsOrResultMessage," +
				"memoryUsed," +
				"memoryAllocated," +
				"monoUsed," +
				"monoAllocated," +
				"location"
			);
		}


		public void AppendBenchmark(BenchmarkCollector.TimeFrameCollected benchmark, string location)
		{
			WriteLine(
				$"{DateTime.Now.ToString("O")}," +
				"benchmark," +
				$"{benchmark.fps.Min()}," +
				$"{Math.Floor(benchmark.fps.Average())}," +
				$"{benchmark.memory.Select(m => m.AllocatedUsed).Max()}," +
				$"{benchmark.memory.Select(m => m.AllocatedTotal).Max()}," +
				$"{benchmark.memory.Select(m => m.MonoUsed).Max()}," +
				$"{benchmark.memory.Select(m => m.MonoTotal).Max()}," +
				$"{location}"
			);
		}

		public void AppendGeneralInfo(string testCase)
		{
			var data = new Dictionary<string, string>();
			data.Add("Commit", VersionUtils.Commit);
			data.Add("Branch", VersionUtils.Branch);
			data.Add("Build", VersionUtils.BuildNumber);
			data.Add("DeviceModel", SystemInfo.deviceModel);
			data.Add("DeviceType", SystemInfo.deviceType.ToString());
			data.Add("DeviceUniqueId", SystemInfo.deviceUniqueIdentifier);
			data.Add("GraphicsDeviceName", SystemInfo.graphicsDeviceName);
			data.Add("ProcessorCount", SystemInfo.processorCount.ToString());
			data.Add("ProcessorType", SystemInfo.processorType);
			data.Add("SystemMemorySize", SystemInfo.systemMemorySize.ToString());
			data.Add("GraphicsMemorySize", SystemInfo.graphicsMemorySize.ToString());
			data.Add("TestCase", testCase);
			var props = string.Join(";", data.Select(a => a.Key + ":" + a.Value.Replace(",", ".")));
			WriteLine(
				$"{DateTime.Now.ToString("O")}," +
				$"generalInfo," +
				$"0," +
				$"{props}," +
				$"0," +
				$"0," +
				$"0," +
				$"0," +
				$"0"
			);
		}

		public void AppendEvent(string eventName, string location)
		{
			WriteLine(
				$"{DateTime.Now.ToString("O")}," +
				$"{eventName.Replace(",", ".")}," +
				$"0," +
				$"0," +
				$"0," +
				$"0," +
				$"0," +
				$"0," +
				$"{location}"
			);
		}

		public void AppendResult(bool succeeded, string message)
		{
			WriteLine($"{DateTime.Now.ToString("O")}," +
				$"result," +
				$"{succeeded}," +
				$"{message.Replace(",", ":")}," +
				$"0," +
				$"0," +
				$"0," +
				$"0," +
				$"0"
			);
		}

		public void AppendException(string message)
		{
			WriteLine($"{DateTime.Now.ToString("O")}," +
				$"exception," +
				$"0," +
				$"{message.Replace(",", ":").Replace("\n", "  ")}," +
				$"0," +
				$"0," +
				$"0," +
				$"0," +
				$"0"
			);
		}
	}
}