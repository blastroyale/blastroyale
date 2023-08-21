using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Presenters;
using FirstLight.Services;
using UnityEngine.UIElements;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Saves and loads UI Tooklkit screen setups
	/// </summary>
	public interface IControlSetupService
	{
		/// <summary>
		/// Saves current visual element states
		/// </summary>
		void SaveControlsPositions(IReadOnlyCollection<VisualElement> elements);

		/// <summary>
		/// Load visual element positions from data and modify elements on root.
		/// </summary>
		void SetControlPositions(VisualElement root);
	}
	
	public class ControlSetupService : IControlSetupService
	{
		private readonly DataService _localData;

		public ControlSetupService()
		{
			_localData = new DataService();
			_localData.LoadData<UIPositionData>();
		}

		public void SaveControlsPositions(IReadOnlyCollection<VisualElement> elements)
		{
			Data.HudScreenSetup = elements.Select(e => new SerializedVisualElementSetup().FromElement(e)).ToList();
			Save();
		}

		public void SetControlPositions(VisualElement root)
		{
			foreach (var saved in Data.HudScreenSetup)
			{
				var e = root.Q(saved.ElementId);
				if (e == null) continue;
				saved.ToElement(e);
			}
		}
		
		private UIPositionData Data => _localData.GetData<UIPositionData>();
		private void Save() => _localData.SaveData<UIPositionData>();

	}
}