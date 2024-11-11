using System.Linq;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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

		public void ResizeContainer()
		{
			if (ClassListContains($"{UssCategory}--small"))
			{
				if (childCount == 0)
				{
					return;
				}
				
				var childElements = Children().Where(c => c.GetType() == typeof(StoreGameProductElement)).ToList();
				
				
				var firstRowColumns = Mathf.CeilToInt(childElements.Count / 2f); 
				var secondRowColumns = childElements.Count - firstRowColumns;
				var maxColumns = Mathf.Max(firstRowColumns, secondRowColumns);

				var baseChildElement = childElements.First();
				
				var baseWidth = baseChildElement.resolvedStyle.width + baseChildElement.resolvedStyle.marginLeft + baseChildElement.resolvedStyle.marginRight;
				
				style.maxWidth = baseWidth * (maxColumns + 1);

				if (childElements.Count % 2 != 0)
				{
					style.justifyContent = Justify.Center;
				}
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