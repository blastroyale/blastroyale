using System;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;

namespace FirstLight.Game.Services
{
	public interface IRewardService
	{
		public bool TryGetUnseenCore(out OpenedCoreMessage msg);
	}
	
	public class RewardService : IRewardService
	{
		private IGameServices _services;
		private Queue<OpenedCoreMessage> _unseenCores = new();
		
		public RewardService(IGameServices services)
		{
			_services = services;
			_services.MessageBrokerService.Subscribe<OpenedCoreMessage>(OnOpenedCore);
		}

		public IEnumerable<OpenedCoreMessage> UnseenOpenedCores => _unseenCores;

		private void OnOpenedCore(OpenedCoreMessage opened)
		{
			// Store still handles itself, for now
			if (_services.GameUiService.IsOpen<StoreScreenPresenter>()) return;
			_unseenCores.Enqueue(opened);
		}

		public void SetCoresSeen()
		{
			_unseenCores.Clear();
		}

		public bool TryGetUnseenCore(out OpenedCoreMessage msg)
		{
			return _unseenCores.TryDequeue(out msg);
		}
	}
}