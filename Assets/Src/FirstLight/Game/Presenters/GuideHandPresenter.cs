using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// TEMPORARY uGUI-based guide hand. Displays a guide hand at a certain position.
/// To be removed and replaced with a generic UITK system when whole UI is refactored to UITK 
/// </summary>
public class GuideHandPresenter : UIPresenter
{
	[SerializeField] private GameObject _animRoot;
	[SerializeField] private Animator _animator;

	private float _artRotation = 45; // rotation that already is from the art image
	private float _rotationDegreeDegreesOffset;
	
	/// <summary>
	/// Controls the animation rotation so we can
	/// make the hand drag into any direction we think fit
	/// </summary>
	public float RotationDegreeOffset
	{
		get => _rotationDegreeDegreesOffset;
		set
		{
			_rotationDegreeDegreesOffset = value;
			_animRoot.transform.rotation = Quaternion.Euler(0, 0, _rotationDegreeDegreesOffset - _artRotation);
		}
	}

	protected override void QueryElements()
	{
		
	}

	protected override UniTask OnScreenOpen(bool reload)
	{
		Hide();
		return base.OnScreenOpen(reload);
	}
	
	public void Show()
	{
		_animRoot.gameObject.SetActive(true);
		_animator.enabled = true;
	}

	public void Hide()
	{
		_animator.enabled = false;
		_animRoot.gameObject.SetActive(false);
	}
	
	public void SetScreenPosition(Vector2 screenPosition, float fingerRotation = 45)
	{
		_animRoot.transform.position = screenPosition;
		RotationDegreeOffset = fingerRotation;
		Show();
	}


	
}
