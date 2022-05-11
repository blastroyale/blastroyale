using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class ShopScreenPresenter : AnimatedUiPresenterData<ShopScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnShopBackButtonClicked;
		}

		[SerializeField, Required] private Transform _contentTransform;
		[SerializeField, Required] private Button _backButton;
		[SerializeField] private float _iapItemAppearDuration = 0.1f;  
		
		private readonly List<ShopItemView> _shopItemViews = new List<ShopItemView>();

		private void Awake()
		{
			_backButton.onClick.AddListener(OnBackButtonPressed);
		}

		private void OnDestroy()
		{
			foreach (var view in _shopItemViews)
			{
				Addressables.ReleaseInstance(view.gameObject);
			}
		}

		protected override async void OnInitialized()
		{
			base.OnInitialized();
			
			var products = Services.StoreService.Products;

			for (var i = 0; i < products.Count; i++)
			{
				var go = await Addressables.InstantiateAsync($"Shop/{products[i].Id}.prefab").Task;
				var shopItemView = go.GetComponent<ShopItemView>();

				shopItemView.SetInfo(products[i]);

				go.transform.SetParent(_contentTransform);
				go.SetActive(false);
				_shopItemViews.Add(shopItemView);
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			foreach (var view in _shopItemViews)
			{
				this.LateCall(_iapItemAppearDuration, view.TriggerAppearAnimation);
			}
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			
			foreach (var view in _shopItemViews)
			{
				view.TriggerUnpackAnimation();
			}
		}
		
		private void OnBackButtonPressed()
		{
			Data.OnShopBackButtonClicked();
		}
	}
}