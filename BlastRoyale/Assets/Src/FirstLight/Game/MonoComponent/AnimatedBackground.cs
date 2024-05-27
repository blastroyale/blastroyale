using System;
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
	[Serializable]
	public class AnimatedBackgroundColor
	{
		[SerializeField, Required] public Color Bottom;
		[SerializeField, Required] public Color Top;
		[SerializeField, Required] public Color Pattern;
	}

	private static readonly int _colorTopPID = Shader.PropertyToID("_ColorTop");
	private static readonly int _colorBottomPID = Shader.PropertyToID("_ColorBottom");
	private static readonly int _colorPatternPID = Shader.PropertyToID("_ColorPattern");

	[SerializeField, Required] private Renderer _quadRenderer;

	[SerializeField, Required] private AnimatedBackgroundColor _default;

	[SerializeField, Required, FoldoutGroup("Rarity Colors")]
	private AnimatedBackgroundColor _common;

	[SerializeField, Required, FoldoutGroup("Rarity Colors")]
	private AnimatedBackgroundColor _uncommon;

	[SerializeField, Required, FoldoutGroup("Rarity Colors")]
	private AnimatedBackgroundColor _rare;

	[SerializeField, Required, FoldoutGroup("Rarity Colors")]
	private AnimatedBackgroundColor _epic;

	[SerializeField, Required, FoldoutGroup("Rarity Colors", expanded: true)]
	private AnimatedBackgroundColor _legendary;

	private Dictionary<EquipmentRarity, AnimatedBackgroundColor> _colorMap;

	private void Awake()
	{
		_colorMap = new Dictionary<EquipmentRarity, AnimatedBackgroundColor>()
		{
			{EquipmentRarity.Common, _common},
			{EquipmentRarity.CommonPlus, _common},
			{EquipmentRarity.Uncommon, _uncommon},
			{EquipmentRarity.UncommonPlus, _uncommon},
			{EquipmentRarity.Rare, _rare},
			{EquipmentRarity.RarePlus, _rare},
			{EquipmentRarity.Epic, _epic},
			{EquipmentRarity.EpicPlus, _epic},
			{EquipmentRarity.Legendary, _legendary},
			{EquipmentRarity.LegendaryPlus, _legendary},
		};
		SetDefault();
	}

	public void SetDefault()
	{
		SetColor(_default);
	}

	public void SetColorByRarity(EquipmentRarity rarity)
	{
		SetColor(_colorMap[rarity]);
	}

	public void SetColor(AnimatedBackgroundColor color)
	{
		_quadRenderer.material.SetColor(_colorTopPID, color.Top);
		_quadRenderer.material.SetColor(_colorBottomPID, color.Bottom);
		_quadRenderer.material.SetColor(_colorPatternPID, color.Pattern);
	}

#if UNITY_EDITOR
	[Button("Test Default Color")]
	private void SetDefaultButton()
	{
		SetDefault();
	}
#endif
}