using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.Store
{
    
    public class StoreCreatorCodeElement : VisualElement
    {

        private VisualElement notSupportingCreatorContainer;
        private VisualElement supportingCreatorContainer;

        private Label creatorNameLabel;
        
        private Button enterCodeButton;
        private Button changeCodeButton;
        private Button stopSupportingButton;

        private string currentCreatorSupporting;

        public Action OnEnterCodeClicked;
        public Action OnUpdateCodeClicked;
        public Action OnStopSupportingClicked;

        public StoreCreatorCodeElement()
        {
            var treeAsset = Resources.Load<VisualTreeAsset>("StoreCreatorCodeElement");
            treeAsset.CloneTree(this);

            notSupportingCreatorContainer = this.Q<VisualElement>("NotSupportingContainer");
            supportingCreatorContainer = this.Q<VisualElement>("SupportingContainer");
            
            creatorNameLabel = this.Q<Label>("SupportingCreatorNameText");
            
            enterCodeButton = this.Q<Button>("EnterCodeButton");
            enterCodeButton.clicked += () => OnEnterCodeClicked?.Invoke();
            
            changeCodeButton = this.Q<Button>("ChangeButton");
            changeCodeButton.clicked += () => OnUpdateCodeClicked?.Invoke();
            
            stopSupportingButton = this.Q<Button>("StopButton");
            stopSupportingButton.clicked += () => OnStopSupportingClicked?.Invoke();
        }


        public void SetData(string creatorCode)
        {
            currentCreatorSupporting = creatorCode;
            
            if (string.IsNullOrEmpty(currentCreatorSupporting))
            {
                notSupportingCreatorContainer.SetDisplay(true);
                supportingCreatorContainer.SetDisplay(false);
                return;
            }
            
            notSupportingCreatorContainer.SetDisplay(false);
            supportingCreatorContainer.SetDisplay(true);

            creatorNameLabel.text = currentCreatorSupporting;
        }
        
        public void UpdateContentCreator(string previousValue, string newValue)
        {
            if (previousValue.Equals(newValue))
                return;
            
            SetData(newValue);
        }


        public new class UxmlFactory : UxmlFactory<StoreGameProductElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
        }

        
    }
}