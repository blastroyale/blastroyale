using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Presenters;
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
        
        public void Attached(VisualElement element)
        {
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
}