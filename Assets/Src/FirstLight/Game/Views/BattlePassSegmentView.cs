using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
    /// <summary>
    /// This class manages the visual elements of battle pass segments on the battle pass screen
    /// </summary>
    public class BattlePassSegmentView : IUIView
    {
        public event Action<BattlePassSegmentView> Clicked;

        private VisualElement _root;
        private ImageButton _button;
        
        public void Attached(VisualElement element)
        {
            _root = element;
            _button = _root.Q<ImageButton>().Required();
            
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