using System;
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters.Store
{
	public class StoreCategoryElement : VisualElement
	{
		public const string UssCategory = "product-category";
		public const string UssCategoryLabel = "category-label";

		public StoreCategoryElement(string categoryName)
		{
			var categoryElement = this;
			categoryElement.AddToClassList(UssCategory);

			var categoryLabel = new LabelOutlined("Category") {name = "CategoryLabel"};
			categoryLabel.AddToClassList(UssCategoryLabel);
			categoryLabel.text = categoryName;
			categoryElement.Add(categoryLabel);
		}

		public void EnsureSize(StoreDisplaySize size)
		{
			if (size != StoreDisplaySize.Half) return;
			var className = $"{UssCategory}--small";
			if (!ClassListContains(className))
			{
				AddToClassList(className);
			}
		}

		public StoreCategoryElement() : this("This is a category")
		{
		}

		public new class UxmlFactory : UxmlFactory<StoreCategoryElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
			}
		}
	}
}