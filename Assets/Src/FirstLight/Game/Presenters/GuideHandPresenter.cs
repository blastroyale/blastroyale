using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;

/// <summary>
/// TEMPORARY uGUI-based guide hand. Displays a guide hand at a certain position.
/// To be removed and replaced with a generic UITK system when whole UI is refactored to UITK 
/// </summary>
public class GuideHandPresenter : UiPresenter
{
	[SerializeField] private GameObject _animRoot;
	[SerializeField] private Animator _animator;
	
	protected override void OnOpened()
	{
		base.OnOpened();
		
		Hide();
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
	
	public void SetPosition(Vector3 pos)
	{
		_animRoot.transform.position = pos;
	}
	
	public void SetPositionAndShow(Vector3 pos)
	{
		SetPosition(pos);
		Show();
	}
}
