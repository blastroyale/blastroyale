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
		[SerializeField, Required] public Color Middle;
		[SerializeField, Required] public Color Top;
		[SerializeField, Required] public Color Pattern;
	}

	private static readonly int _trigerredColorChange = Shader.PropertyToID("_TrigerredColorChange");

	private static readonly int _colorTopPID = Shader.PropertyToID("_ColorTop");
	private static readonly int _colorMiddlePID = Shader.PropertyToID("_ColorMiddle");
	private static readonly int _colorBottomPID = Shader.PropertyToID("_ColorBottom");
	private static readonly int _colorPatternPID = Shader.PropertyToID("_ColorPattern");

	private static readonly int _colorTargetTopPID = Shader.PropertyToID("_ColorTopTarget");
	private static readonly int _colorTargetMiddlePID = Shader.PropertyToID("_ColorMiddleTarget");
	private static readonly int _colorTargetBottomPID = Shader.PropertyToID("_ColorBottomTarget");
	private static readonly int _colorTargetPatternPID = Shader.PropertyToID("_ColorPatternTarget");

	[SerializeField, Required] private Renderer _quadRenderer;

	[SerializeField, Required] private AnimatedBackgroundColor _default;
	[SerializeField, Required] private AnimatedBackgroundColor _dimmedColor;

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

	private AnimatedBackgroundColor _lastColor;

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

	public void SetDimmed()
	{
		SetColor(_dimmedColor);
	}

	public void SetColorByRarity(EquipmentRarity rarity)
	{
		SetColor(_colorMap[rarity]);
	}

	public void SetColor(AnimatedBackgroundColor color, bool animate = false)
	{
		if (_lastColor != null && animate)
		{
			_quadRenderer.material.SetColor(_colorTopPID, _lastColor.Top);
			_quadRenderer.material.SetColor(_colorMiddlePID, _lastColor.Middle);
			_quadRenderer.material.SetColor(_colorBottomPID, _lastColor.Bottom);
			_quadRenderer.material.SetColor(_colorPatternPID, _lastColor.Pattern);
		}
		else
		{
			_quadRenderer.material.SetColor(_colorTopPID, color.Top);
			_quadRenderer.material.SetColor(_colorMiddlePID, color.Middle);
			_quadRenderer.material.SetColor(_colorBottomPID, color.Bottom);
			_quadRenderer.material.SetColor(_colorPatternPID, color.Pattern);
		}

		_quadRenderer.material.SetFloat(_trigerredColorChange, Time.time);
		_quadRenderer.material.SetColor(_colorTargetTopPID, color.Top);
		_quadRenderer.material.SetColor(_colorTargetMiddlePID, color.Middle);
		_quadRenderer.material.SetColor(_colorTargetBottomPID, color.Bottom);
		_quadRenderer.material.SetColor(_colorTargetPatternPID, color.Pattern);
		_lastColor = color;
	}

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
	private bool _usingDimmed = false;
#pragma warning restore CS0414 // Field is assigned but its value is never used

	[Button("Test Default Color")]
	private void SetDefaultButton()
	{
		_usingDimmed = false;
		SetDefault();
	}

	[Button("Test Dimmed Color")]
	private void SetDimmedButton()
	{
		_usingDimmed = true;
		SetDimmed();
	}

	/*private void OnValidate()
	{
		if (Application.isPlaying) return;
		if (usingDimmed)
		{
			SetDimmed();
		}
		else { SetDefault(); }
	}*/
#endif
}