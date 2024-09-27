using BuffSystem;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;


namespace FirstLight.Game.Presenters
{
	public class BuffsView : UIView
	{
		private BuffVirtualEntity _buffs;
		
		protected override void Attached()
		{
			Element.SetDisplay(false);
		}

		public async UniTask Animate()
		{
			if (_buffs == null || _buffs.Stats.Count == 0)
			{
				return;
			}
			
			var duration = 150;
			Element.style.opacity = 0;
			Element.SetDisplay(true);
			Element.AnimatePing();
			Element.AnimateOpacity(0, 1, duration);
			await UniTask.Delay(duration);
			
		}
		
		public void SetBuffs(BuffVirtualEntity e)
		{
			var service = MainInstaller.ResolveServices().BuffService;
			_buffs = e;
			if (e.ActiveBuffs.Count == 0)
			{
				FLog.Verbose("No buffs to display");
				return;
			}
			
			foreach(var (stat, value) in e.Stats) 
			{
				// hard-coded for now
				if (stat == BuffStat.PctBonusBurnForRewards) continue;
				var text = new LabelOutlined($"+{value.FinalValue.AsInt}% {service.GetName(stat)}");
				text.AddToClassList("nft-buff__item");
				Element.Add(text);
			}
		}
	}
}