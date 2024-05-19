using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.TestCases.FirebaseLab;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using UnityEngine;
using UnityEngine.Profiling;

namespace FirstLight.Game.TestCases
{
	public class BenchmarkCollector
	{
		private int matchCount = 0;
		private string currentLocation = "Loading";
		private TimeFrameCollected currentTimeFrame;
		private TestLabManager _manager;
		private IMessageBrokerService _messageBroker;
		private readonly PlayTestCase _testCase;

		public BenchmarkCollector(TestLabManager manager, PlayTestCase testCase)
		{
			_manager = manager;
			_testCase = testCase;
		}

		public void Start()
		{
			currentTimeFrame = new ();
			_manager.WriteHeaders();
			VersionUtils.LoadVersionData();
			_manager.AppendGeneralInfo(_testCase.GetType().Name);
			Statechart.Statechart.OnStateEntered += (state) => LogMessage("StateEntered " + state);
			CollectFPS().Forget();
			PublishFPS().Forget();
			WaitForServices().Forget();
		}


		public async UniTaskVoid WaitForServices()
		{
			var services = await MainInstaller.WaitResolve<IGameServices>();
			_messageBroker = services.MessageBrokerService;

			Wrap<MatchStartedMessage>();
			Wrap<DataReinitializedMessage>();
			Wrap<SuccessAuthentication>();
			Wrap<MatchSimulationStartedMessage>();
			Wrap<SimulationEndedMessage>();
			Wrap<LocalPlayerClickedPlayMessage>();
			Wrap<GameCompletedRewardsMessage>();
			Wrap<ItemRewardedMessage>();
			Wrap<ClaimedRewardsMessage>();
			Wrap<TrophiesUpdatedMessage>();
			Wrap<BattlePassLevelUpMessage>();
			Wrap<BenchmarkStartedLoadingMatchAssets>();
			Wrap<BenchmarkLoadedMandatoryMatchAssets>();
			Wrap<BenchmarkLoadedOptionalMatchAssets>();
			services.MessageBrokerService.Subscribe<BenchmarkStartedLoadingMatchAssets>((msg) =>
			{
				matchCount++;
				currentLocation = "Match" + matchCount+" "+msg.Map;
			});
			services.MessageBrokerService.Subscribe<MainMenuOpenedMessage>((_) =>
			{
				currentLocation = "MainMenu";
			});
			services.UIService.OnScreenOpened += (screen, layer) => LogMessage("OpenScreen " + screen);
			services.MatchmakingService.OnGameMatched += (g) => LogMessage("OnGameMatched");
			services.MatchmakingService.OnMatchmakingJoined += (g) => LogMessage("OnMatchmakingJoined");
			services.AuthenticationService.OnLogin += (g) => LogMessage("OnLogin");
		}

		public void Wrap<T>() where T : IMessage
		{
			_messageBroker.Subscribe<T>((T t) =>
			{
				LogMessage(typeof(T).Name);
			});
		}

		public void LogMessage(string message)
		{
			_manager.AppendEvent(message, currentLocation);
		}

		public async UniTaskVoid PublishFPS()
		{
			while (true)
			{
				await UniTask.Delay(2000);
				var data = Collect();
				_manager.AppendBenchmark(data, currentLocation);
			}
		}

		private async UniTaskVoid CollectFPS()
		{
			for (;;)
			{
				var lastFrameCount = Time.frameCount;
				var lastTime = Time.realtimeSinceStartup;
				await UniTask.Delay(333);

				var timeSpan = Time.realtimeSinceStartup - lastTime;
				var frameCount = Time.frameCount - lastFrameCount;
				var fps = Mathf.RoundToInt(frameCount / timeSpan);
				currentTimeFrame.fps.Add(fps);

				long byteToMegabyte = 1024 * 1024;
				currentTimeFrame.memory.Add(new MemoryInfo()
				{
					AllocatedTotal = Profiler.GetTotalReservedMemoryLong() / byteToMegabyte,
					AllocatedUsed = Profiler.GetTotalAllocatedMemoryLong() / byteToMegabyte,
					MonoTotal = Profiler.GetMonoHeapSizeLong() / byteToMegabyte,
					MonoUsed = Profiler.GetMonoUsedSizeLong() / byteToMegabyte,
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
			public List<int> fps = new ();
			public List<MemoryInfo> memory = new ();
		}

		public class MemoryInfo
		{
			public long MonoTotal;
			public long MonoUsed;
			public long AllocatedUsed;
			public long AllocatedTotal;
		}
	}
}