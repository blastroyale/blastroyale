using System.Collections.Generic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Controller for the AnimatedBackground, always use this to modify the background in any way.
/// It is basically a wrapper to change the ScrollingBackground shader
/// </summary>
public class AnimatedBackground : MonoBehaviour
{
	private static readonly int _colorTopPID = Shader.PropertyToID("_ColorTop");
	private static readonly int _colorBottomPID = Shader.PropertyToID("_ColorBottom");
	private static readonly int _colorPatternPID = Shader.PropertyToID("_ColorPattern");

	[SerializeField, Required] private Renderer _quadRenderer;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/common_row")]
	private Color _commonBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/common_row")]
	private Color _commonTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors")]
	private Color _commonPattern;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/uncommon_row")]
	private Color _uncommonBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/uncommon_row")]
	private Color _uncommonTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors")]
	private Color _uncommonPattern;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/rare_row")]
	private Color _rareBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/rare_row")]
	private Color _rareTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors")]
	private Color _rarePattern;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/epic_row")]
	private Color _epicBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/epic_row")]
	private Color _epicTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors")]
	private Color _epicPattern;

	[SerializeField, Required, FoldoutGroup("Rarity Colors"), HorizontalGroup("Rarity Colors/legendary_row")]
	private Color _legendaryBottom;

	[SerializeField, Required, FoldoutGroup("Rarity Colors", expanded: true), HorizontalGroup("Rarity Colors/legendary_row")]
	private Color _legendaryTop;

	[SerializeField, Required, FoldoutGroup("Rarity Colors", expanded: true)]
	private Color _legendaryPattern;

	private Color _defaultColorTop;
	private Color _defaultColorBottom;
	private Color _defaultColorPattern;

	private Dictionary<EquipmentRarity, (Color bottom, Color top, Color gradient)> _colorMap;

	private void Awake()
	{
		_defaultColorTop = _quadRenderer.material.GetColor(_colorTopPID);
		_defaultColorBottom = _quadRenderer.material.GetColor(_colorBottomPID);
		_defaultColorPattern = _quadRenderer.material.GetColor(_colorPatternPID);

		_colorMap = new Dictionary<EquipmentRarity, (Color, Color, Color)>()
		{
			{EquipmentRarity.Common, (_commonBottom, _commonTop, _commonPattern)},
			{EquipmentRarity.CommonPlus, (_commonBottom, _commonTop, _commonPattern)},
			{EquipmentRarity.Uncommon, (_uncommonBottom, _uncommonTop, _uncommonPattern)},
			{EquipmentRarity.UncommonPlus, (_uncommonBottom, _uncommonTop, _uncommonPattern)},
			{EquipmentRarity.Rare, (_rareBottom, _rareTop, _rarePattern)},
			{EquipmentRarity.RarePlus, (_rareBottom, _rareTop, _rarePattern)},
			{EquipmentRarity.Epic, (_epicBottom, _epicTop, _epicPattern)},
			{EquipmentRarity.EpicPlus, (_epicBottom, _epicTop, _epicPattern)},
			{EquipmentRarity.Legendary, (_legendaryBottom, _legendaryTop, _legendaryPattern)},
			{EquipmentRarity.LegendaryPlus, (_legendaryBottom, _legendaryTop, _legendaryPattern)},
		};
	}

	public void SetDefault()
	{
		SetColor(_defaultColorTop, _defaultColorBottom, _defaultColorPattern);
	}

	public void SetColorByRarity(EquipmentRarity rarity)
	{
		SetColor(_colorMap[rarity].top, _colorMap[rarity].bottom, _colorMap[rarity].gradient);
	}

	private void SetColor(Color top, Color bottom, Color pattern)
	{
		_quadRenderer.material.SetColor(_colorTopPID, top);
		_quadRenderer.material.SetColor(_colorBottomPID, bottom);
		_quadRenderer.material.SetColor(_colorPatternPID, pattern);
	}
}