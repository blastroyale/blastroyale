using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
    /// <summary>
    /// This class manages the visual elements of battle pass segments on the battle pass screen
    /// </summary>
    public class BattlePassSegmentView : IUIView
    {
        private const string UssRarityCommon = "reward-holder__rarity--common";
        private const string UssRarityUncommon = "reward-holder__rarity--uncommon";
        private const string UssRarityRare = "reward-holder__rarity--rare";
        private const string UssRarityEpic = "reward-holder__rarity--epic";
        private const string UssRarityLegendary = "reward-holder__rarity--legendary";
        private const string UssRarityRainbow = "reward-holder__rarity--rainbow";
        public event Action<BattlePassSegmentView> Clicked;

        private VisualElement _rarityImage;
        private VisualElement _root;
        private ImageButton _button;
        
        public void Attached(VisualElement element)
        {
            _root = element;
            _button = _root.Q<ImageButton>("RewardButton").Required();
            _rarityImage = _root.Q("RewardRarity").Required();
            
            _button.clicked += () => Clicked?.Invoke(this);
        }

        public void SubscribeToEvents()
        {
        }

        public void UnsubscribeFromEvents()
        {
        }
        
        /// <summary>
        /// Sets the data needed to fill the segment visuals
        /// </summary>
        public void SetData(BattlePassSegmentData data)
        {
            _rarityImage.AddToClassList(GetRarityStyle(data.RewardConfig.GameId));
        }
        
        private string GetRarityStyle(GameId id)
        {
            switch (id)
            {
                case GameId.CoreCommon:
                    return UssRarityCommon;
                    
                case GameId.CoreUncommon:
                    return UssRarityUncommon;
                
                case GameId.CoreRare:
                    return UssRarityRare;
                
                case GameId.CoreEpic:
                    return UssRarityEpic;
                
                case GameId.CoreLegendary:
                    return UssRarityLegendary;
                
                default:
                    return UssRarityRainbow;
            }
        }
    }

    /// <summary>
    /// This class holds the data used to update BattlePassSegmentViews
    /// </summary>
    public struct BattlePassSegmentData
    {
        public uint SegmentLevel;
        public uint CurrentLevel;
        public uint CurrentProgress;
        public uint PredictedCurrentLevel;
        public uint PredictedCurrentProgress;
        public uint MaxProgress;
        public EquipmentRewardConfig RewardConfig;

        public uint SegmentLevelForRewards => SegmentLevel + 1;
    }
}