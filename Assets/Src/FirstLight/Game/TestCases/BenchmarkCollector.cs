using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FirstLight.Game.TestCases
{
	public class BenchmarkCollector : MonoBehaviour
	{
		private float frequency = 0.1f;
		private TimeFrameCollected currentTimeFrame;

		private void Start()
		{
			currentTimeFrame = new();
			StartCoroutine(CollectFPS());
		}

		private IEnumerator CollectFPS()
		{
			for (;;)
			{
				var lastFrameCount = Time.frameCount;
				var lastTime = Time.realtimeSinceStartup;
				yield return new WaitForSeconds(frequency);

				var timeSpan = Time.realtimeSinceStartup - lastTime;
				var frameCount = Time.frameCount - lastFrameCount;
				var fps = Mathf.RoundToInt(frameCount / timeSpan);
				currentTimeFrame.fps.Add(fps);
				currentTimeFrame.memory.Add(new MemoryInfo()
				{
					total = SystemInfo.systemMemorySize,
					used = (int) (System.GC.GetTotalMemory(false) / 1024L / 1024L)
				});
			} 
		}

		public TimeFrameCollected Collect()
		{
			var value = currentTimeFrame;
			currentTimeFrame = new TimeFrameCollected();
			return value;
		}


		public class TimeFrameCollected
		{
			public List<int> fps = new();
			public List<MemoryInfo> memory = new();


			public int AvgFps()
			{
				return Convert.ToInt32(fps.Average());
			}

			public int MinFps()
			{
				return fps.Min();
			}

			public int MaxMemoryInMB()
			{
				return memory.Select(memoryInfo => memoryInfo.used).Max();
			}

			public int TotalMemory()
			{
				return memory.First().total;
			}
		}

		public class MemoryInfo
		{
			public int total;
			public int used;
		}
	}
}