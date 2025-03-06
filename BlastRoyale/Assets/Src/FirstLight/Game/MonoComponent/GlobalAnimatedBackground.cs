using System;
using FirstLight.FLogger;
using FirstLight.Game.Core;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

public class GlobalAnimatedBackground : MonoSingleton<GlobalAnimatedBackground>
{
	[Required] [SerializeField] private AnimatedBackground _animatedBackground;

	protected override void _Awake()
	{
		Disable();
		DontDestroyOnLoad(gameObject);
	}

	public void Enable()
	{
		Debug.Log("Turning on global animated background");
		_animatedBackground.gameObject.SetActive(true);
	}

	public void Disable()
	{
		Debug.Log("Turning off global animated background");
		_animatedBackground.gameObject.SetActive(false);
	}

	public void SetColor(AnimatedBackground.AnimatedBackgroundColor color)
	{
		_animatedBackground.SetColor(color);
	}

	public void SetDimmedColor()
	{
		Enable();
		_animatedBackground.SetDimmed();
	}

	public void SetDefault()
	{
		Enable();
		_animatedBackground.SetDefault();
	}

	public void SetColorByRarity(EquipmentRarity rarity)
	{
		_animatedBackground.SetColorByRarity(rarity);
	}
}