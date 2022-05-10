using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This View handles the IAP shop item View in the UI.
	/// </summary>
	public class ShopItemView : MonoBehaviour
	{
		[SerializeField, Required] private Image _image;
		[SerializeField, Required] private TextMeshProUGUI _titleText;
		[SerializeField, Required] private TextMeshProUGUI _priceText;
		[SerializeField, Required] private UiButtonView _buyButton;
		[SerializeField, Required] private Animation _animation;
		
		protected IGameServices GameServices;
		protected ProductData Product;
		
		private void OnValidate()
		{
			_animation = _animation ? _animation : GetComponent<Animation>();
		}
		
		private void Awake()
		{
			_buyButton.onClick.AddListener(Buy);

			OnAwake();
		}

		protected virtual void OnAwake()
		{
		}
		
		/// <summary>
		/// Set's the shop item information
		/// </summary>
		public void SetInfo(ProductData product)
		{
			GameServices ??= MainInstaller.Resolve<IGameServices>();
			Product = product;
			_priceText.text = Product.Price;
			_titleText.text = LocalizationManager.GetTranslation(Product.Metadata.localizedTitle);
		}
		
		/// <summary>
		/// Triggers the animation to disappear the slot
		/// </summary>
		public void TriggerAppearAnimation()
		{
			gameObject.SetActive(true);
			
			_animation.Rewind();
			_animation.Play("Bundle_Appear");
		}
		
		/// <summary>
		/// Triggers the animation to disappear the slot
		/// </summary>
		public void TriggerUnpackAnimation()
		{
			_animation.Rewind();
			_animation.Play("Bundle_Unpack");
			
			this.LateCall(_animation.clip.length, () => gameObject.SetActive(false));
		}

		protected virtual void Buy()
		{
			GameServices.StoreService.BuyProduct(Product.Id);
		}
	}
}

