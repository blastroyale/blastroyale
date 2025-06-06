using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Shows a list of all available mutators and allows the user to select as many as they want.
	/// </summary>
	public class SelectMutatorsPopupView : UIView
	{
		[Q("MutatorsScrollView")] private ScrollView _mutatorsScroller;
		[Q("SelectedMutatorsLabel")] private Label _selectedMutatorsLabel;
		[Q("ConfirmButton")] private LocalizedButton _confirmButton;

		private readonly Action<Mutator> _onMutatorsSelected;
		private readonly Mutator _currentMutators;

		public SelectMutatorsPopupView(Action<Mutator> onMutatorsSelected, Mutator currentMutators)
		{
			_onMutatorsSelected = onMutatorsSelected;
			_currentMutators = currentMutators;
		}

		protected override void Attached()
		{
			var options = Enum.GetValues(typeof(Mutator))
				.Cast<Mutator>()
				.Where(m => m != Mutator.None && m != Mutator.HammerTime) // TODO: Remove hammertime when we have weapon filters
				.ToArray();

			_mutatorsScroller.Clear();
			foreach (Mutator mutator in options)
			{
				var element = new MatchSettingsSelectionElement(mutator.GetLocalizationKey(), mutator.GetDescriptionLocalizationKey());
				element.userData = mutator;
				element.clicked += () => OnMutatorClicked(element);

				if (_currentMutators.HasFlagFast(mutator))
				{
					element.AddToClassList("match-settings-selection--selected");
				}

				_mutatorsScroller.Add(element);

				LoadMutatorPicture(mutator, element).Forget();
			}

			_confirmButton.Required().clicked += OnConfirmClicked;
			_selectedMutatorsLabel.text = string.Format(ScriptLocalization.UITCustomGames.selected_mutators, GetSelectedMutators().CountSetFlags());
		}

		private void OnMutatorClicked(MatchSettingsSelectionElement element)
		{
			element.ToggleInClassList("match-settings-selection--selected");
			_selectedMutatorsLabel.text = string.Format(ScriptLocalization.UITCustomGames.selected_mutators, GetSelectedMutators().CountSetFlags());
		}

		private void OnConfirmClicked()
		{
			var selectedMutators = GetSelectedMutators();
			_onMutatorsSelected(selectedMutators);
		}

		private Mutator GetSelectedMutators()
		{
			return _mutatorsScroller.Children()
				.Where(ve => ve.ClassListContains("match-settings-selection--selected"))
				.Select(ve => (Mutator) ve.userData)
				.DefaultIfEmpty(Mutator.None)
				.Aggregate((acc, mut) => acc | mut);
		}

		private async UniTaskVoid LoadMutatorPicture(Mutator mutator, MatchSettingsSelectionElement element)
		{
			// TODO
			// var mapImage = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mutatorConfig.Map, false);
			await UniTask.NextFrame(); // Need to wait a frame to make sure the element is attached
			// if (element.panel == null) return;
			// element.SetImage(mapImage);
		}
	}
}