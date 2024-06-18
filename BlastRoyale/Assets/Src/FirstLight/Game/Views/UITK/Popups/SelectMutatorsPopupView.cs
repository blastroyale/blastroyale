using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	public class SelectMutatorsPopupView : UIView
	{
		private readonly Action<List<string>> _onMutatorsSelected;
		private readonly List<string> _currentMutators;

		private readonly IGameServices _services;

		private ScrollView _mutatorsScroller;
		private Label _selectedMutatorsLabel;

		public SelectMutatorsPopupView(Action<List<string>> onMutatorsSelected, List<string> currentMutators)
		{
			_onMutatorsSelected = onMutatorsSelected;
			_currentMutators = currentMutators;

			_services = MainInstaller.ResolveServices();
		}

		protected override void Attached()
		{
			var options = _services.ConfigsProvider.GetConfigsList<QuantumMutatorConfig>();

			_mutatorsScroller = Element.Q<ScrollView>("MutatorsScrollView").Required();
			_mutatorsScroller.Clear();
			foreach (var mutator in options)
			{
				var element = new MatchSettingsSelectionElement(mutator.Id, "some description");
				element.userData = mutator.Id;
				element.clicked += () => OnMutatorClicked(element);

				if (_currentMutators.Contains(mutator.Id))
				{
					element.AddToClassList("match-settings-selection--selected");
				}

				_mutatorsScroller.Add(element);

				LoadMutatorPicture(mutator, element).Forget();
			}

			Element.Q<LocalizedButton>("ConfirmButton").Required().clicked += OnConfirmClicked;
			_selectedMutatorsLabel = Element.Q<Label>("SelectedMutatorsLabel").Required();
			_selectedMutatorsLabel.text = $"#Selected Mutators: {GetSelectedMutators().Count}#";
		}

		private void OnMutatorClicked(MatchSettingsSelectionElement element)
		{
			element.ToggleInClassList("match-settings-selection--selected");
			_selectedMutatorsLabel.text = $"#Selected Mutators: {GetSelectedMutators().Count}#";
		}

		private void OnConfirmClicked()
		{
			var selectedMutators = GetSelectedMutators();
			_onMutatorsSelected(selectedMutators);
		}

		private List<string> GetSelectedMutators()
		{
			return _mutatorsScroller.Children()
				.Where(ve => ve.ClassListContains("match-settings-selection--selected"))
				.Select(ve => ve.userData as string).ToList();
		}

		private async UniTaskVoid LoadMutatorPicture(QuantumMutatorConfig mutatorConfig, MatchSettingsSelectionElement element)
		{
			// TODO
			// var mapImage = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mutatorConfig.Map, false);
			await UniTask.NextFrame(); // Need to wait a frame to make sure the element is attached
			// if (element.panel == null) return;
			// element.SetImage(mapImage);
		}
	}
}