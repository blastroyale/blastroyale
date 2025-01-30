using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Used for loading remote avatars, it automatically handles the loading state
	/// use <see cref="SetAvatar(string)"/> method to load the texture
	/// </summary>
	public class RemoteAvatarElement : VisualElement
	{
		private const string USS_BLOCK = "remote-avatar";
		private const string USS_REAL = "remote-avatar__real";
		private const string USS_PLACEHOLDER = "remote-avatar__placeholder";
		private const string USS_IMAGE = "remote-avatar__image";
		private const string USS_SPINNER = "remote-avatar__spinner";

		private VisualElement _realAvatar;
		private VisualElement _placeHolder;
		private VisualElement _loader;
		private Texture2D _texture;

		public RemoteAvatarElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_realAvatar = new VisualElement {name = "RealAvatar"}.AddClass(USS_IMAGE, USS_REAL));
			SetLoading();
			// TODO: Fix this - causes issues when changing tabs on friend screen
			// because the elements detached and gets cleaned up
			// RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
		}
		
		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
			if (_realAvatar != null)
			{
				_realAvatar.style.backgroundImage = null;
				_realAvatar.style.opacity = 0;
			}
				
			if (_texture == null) return;
			UnityEngine.Object.Destroy(_texture);
			_texture  = null;
		}

		public async UniTaskVoid SetAvatar(string url, CancellationToken cancellationToken)
		{
			if (url == null)
			{
				if (_loader != null)
				{
					Remove(_loader);
					_loader = null;
				}

				return;
			}

			var task = MainInstaller.ResolveServices().RemoteTextureService.RequestTexture(url, true, cancellationToken);
			if (!cancellationToken.IsCancellationRequested)
			{
				await SetAvatar(task);	
			}
		}

		public async UniTask SetAvatar(UniTask<Texture2D> task)
		{
			SetLoading();

			try
			{
				var texture = await task;
				if (!this.IsAttached()) return;
				_texture = texture;
				_realAvatar.style.backgroundImage = _texture;
				_realAvatar.style.opacity = 1;
				CleanLoadingState(false);
			}
			catch (Exception ex)
			{
				FLog.Warn("Failed to load avatar ", ex);
				SetFailedState();
			}
		}

		public void SetFailedState()
		{
			SetLoading();
			if (_loader != null)
			{
				Remove(_loader);
				_loader = null;
			}
		}

		public void CleanLoadingState(bool imediately)
		{
			if (!this.IsAttached()) return;
			if (_placeHolder != null && imediately)
			{
				Remove(_placeHolder);
				_placeHolder = null;
			}

			if (!imediately)
			{
				// Remove placeholder later so it blends with the new texture, opacity transition
				_placeHolder?.schedule.Execute(() =>
				{
					if (!this.IsAttached()) return;
					if (_placeHolder != null)
						Remove(_placeHolder);
					_placeHolder = null;
				}).ExecuteLater(500);
			}

			if (_loader != null)
			{
				Remove(_loader);
				_loader = null;
			}
		}

		public void SetLoading()
		{
			if (_placeHolder == null)
			{
				Insert(0, _placeHolder = new VisualElement {name = "Placeholder"}.AddClass(USS_IMAGE, USS_PLACEHOLDER));
			}

			if (_loader == null)
			{
				Insert(1, _loader = new VisualElement().AddClass(USS_SPINNER));
				_loader.AddRotatingEffect(60f, 1);
			}

			_realAvatar.style.opacity = 0;
		}

		public new class UxmlFactory : UxmlFactory<RemoteAvatarElement>
		{
		}
	}
}