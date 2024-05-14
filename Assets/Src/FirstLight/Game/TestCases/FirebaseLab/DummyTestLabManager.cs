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
using System.IO;
using FirstLight.FLogger;
using FirstLight.Server.SDK.Modules;
using UnityEngine;

namespace FirstLight.Game.TestCases.FirebaseLab
{
	// Dummy class to handle non-Android platform.  This class is instantiated instead to avoid having
	// to wrap code in #if blocks.
	internal class DummyTestLabManager : TestLabManager
	{
		private StreamWriter _fileStream;

		public override void NotifyHarnessTestIsComplete()
		{
			// do nothing!
		}


		public DummyTestLabManager()
		{
		}
		

		public override void WriteLine(string line)
		{
			if (_fileStream == null)
			{
				var persistentDataPath = Application.persistentDataPath + "/testLog.csv";
				_fileStream = File.CreateText(persistentDataPath);
				_fileStream.AutoFlush = true;
				Debug.Log("Dummy test log at " + persistentDataPath);
			}
			_fileStream.WriteLine(line);
		}
	}
}