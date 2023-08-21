using System;
using System.Collections;
using System.Collections.Generic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Controller for the AnimatedBackground, always use this to modify the background in any way.
/// It is basically a wrapper to change the ScrollingBackground shader
/// </summary>
public class AnimatedBackground : MonoBehaviour
{
	private static readonly int _colorTop = Shader.PropertyToID("_ColorTop");
	private static readonly int _colorBottom = Shader.PropertyToID("_ColorBottom");

	[SerializeField, Required] private Renderer _quadRenderer;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/common_row")]
	private Color _commonBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/common_row")]
	private Color _commonTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/uncommon_row")]
	private Color _uncommonBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/uncommon_row")]
	private Color _uncommonTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/rare_row")]
	private Color _rareBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/rare_row")]
	private Color _rareTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/epic_row")]
	private Color _epicBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/epic_row")]
	private Color _epicTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/legendary_row")]
	private Color _legendaryBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors", expanded: true), HorizontalGroup("Rarity Colors/legendary_row")]
	private Color _legendaryTop;

	private Color _defaultColorTop;
	private Color _defaultColorBottom;

	private Dictionary<EquipmentRarity, (Color bottom, Color top)> _colorMap;


	private void Awake()
	{
		_defaultColorTop = _quadRenderer.material.GetColor(_colorTop);
		_defaultColorBottom = _quadRenderer.material.GetColor(_colorBottom);

		_colorMap = new Dictionary<EquipmentRarity, (Color, Color)>()
		{
			{EquipmentRarity.Common, (_commonBottom, _commonTop)},
			{EquipmentRarity.CommonPlus, (_commonBottom, _commonTop)},
			{EquipmentRarity.Uncommon, (_uncommonBottom, _uncommonTop)},
			{EquipmentRarity.UncommonPlus, (_uncommonBottom, _uncommonTop)},
			{EquipmentRarity.Rare, (_rareBottom, _rareTop)},
			{EquipmentRarity.RarePlus, (_rareBottom, _rareTop)},
			{EquipmentRarity.Epic, (_epicBottom, _epicTop)},
			{EquipmentRarity.EpicPlus, (_epicBottom, _epicTop)},
			{EquipmentRarity.Legendary, (_legendaryBottom, _legendaryTop)},
			{EquipmentRarity.LegendaryPlus, (_legendaryBottom, _legendaryTop)},
		};
	}

	public void SetDefault()
	{
		SetColor(_defaultColorTop, _defaultColorBottom);
	}

	public void SetColorByRarity(EquipmentRarity rarity)
	{
		SetColor(_colorMap[rarity].top, _colorMap[rarity].bottom);
	}

	public void SetColor(Color color, float desaturation = 0.8f)
	{
		SetColor(color, color * desaturation);
	}

	public void SetColor(Color top, Color bottom)
	{
		_quadRenderer.material.SetColor("_ColorTop", top);
		_quadRenderer.material.SetColor("_ColorBottom", bottom);
	}
}