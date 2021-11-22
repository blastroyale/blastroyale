using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The <see cref="PlayableAsset"/> for controlling UI presenters
	/// </remarks>
	public class UiPresenterAsset : PlayableAssetBase<UiPresenterBehaviour>
	{
		public ExposedReference<GameObject> Reference;

		protected override ScriptPlayable<UiPresenterBehaviour> OnCreated(PlayableGraph graph, GameObject owner)
		{
			var playable = base.OnCreated(graph, owner);
			var obj = Reference.Resolve(graph.GetResolver());
			var presenter = obj.GetComponent<UiPresenter>();

			if (presenter == null)
			{
				throw new InvalidCastException($"The reference object {obj} does not contain a {typeof(UiPresenter)} component");
			}
			
			if (!Application.isPlaying)
			{
				return playable;
			}
			
			var guiService = MainInstaller.Resolve<IGameServices>().GuidService;
			var behaviour = playable.GetBehaviour();
			
			behaviour.UiService = guiService.GetElement(GuidId.Main).Elements[0].GetComponent<Main>().UiService;
			behaviour.UiPresenter = presenter.GetType();

			return playable;
		}
	}
}