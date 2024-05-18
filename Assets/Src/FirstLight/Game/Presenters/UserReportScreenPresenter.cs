using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.UserReporting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;

namespace FirstLight.Game.Presenters
{
	[UILayer(UILayer.Debug)]
	public class UserReportScreenPresenter : UIPresenter
	{
		private readonly List<string> CATEGORIES = new List<string>
		{
			"Gameplay",
			"Account",
			"UI",
			"Performance",
			"Other"
		};

		private IGameServices _services;

		private ImageButton _reportContainer;
		private TextField _summaryInput;
		private TextField _descriptionInput;
		private DropdownField _categoryField;
		private Button _sendButton;
		private ProgressBar _progressBar;
		private VisualElement _screenshot;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			_summaryInput = Root.Q<TextField>("SummaryInput").Required();
			_descriptionInput = Root.Q<TextField>("DescriptionInput").Required();
			_reportContainer = Root.Q<ImageButton>("ReportContainer").Required();
			_categoryField = Root.Q<DropdownField>("CategoryDropdown").Required();
			_sendButton = Root.Q<Button>("SendButton").Required();
			_progressBar = Root.Q<ProgressBar>("ProgressBar").Required();
			_screenshot = Root.Q<VisualElement>("Screenshot").Required();

			_sendButton.Required().clicked += () => SendReport().Forget();
			Root.Q<ImageButton>("ExitButton").clicked += ClosePopup;
			;
			Root.Q<ImageButton>("Icon").Required().clicked += () => OnOpenReportClicked().Forget();

			_categoryField.choices = CATEGORIES;

			_reportContainer.SetDisplay(false);
			_progressBar.SetVisibility(false);
		}

		private async UniTaskVoid OnOpenReportClicked()
		{
			UserReportingService.Instance.TakeScreenshot(Screen.width, Screen.height);
			await UniTask.DelayFrame(5);

			_screenshot.style.backgroundImage = UserReportingService.Instance.GetLatestScreenshot();
			_reportContainer.SetDisplay(true);
		}

		private void ClosePopup()
		{
			_reportContainer.SetDisplay(false);
			_sendButton.SetEnabled(true);
			_summaryInput.SetEnabled(true);
			_descriptionInput.SetEnabled(true);
			_categoryField.SetEnabled(true);
			_progressBar.SetVisibility(false);
			_screenshot.style.backgroundImage = null;
			_categoryField.value = string.Empty;
			_descriptionInput.value = string.Empty;
			_summaryInput.value = string.Empty;
			_categoryField.value = CATEGORIES[0];
		}

		private async UniTaskVoid SendReport()
		{
			if (string.IsNullOrWhiteSpace(_categoryField.value))
			{
				_categoryField.AnimatePing(1.1f);
				return;
			}

			if (string.IsNullOrWhiteSpace(_summaryInput.value))
			{
				_summaryInput.AnimatePing(1.1f);
				return;
			}

			if (string.IsNullOrWhiteSpace(_descriptionInput.value))
			{
				_descriptionInput.AnimatePing(1.1f);
				return;
			}

			_sendButton.SetEnabled(false);
			_progressBar.SetVisibility(true);
			_progressBar.title = "Sending report...";
			_summaryInput.SetEnabled(false);
			_descriptionInput.SetEnabled(false);
			_categoryField.SetEnabled(false);

			var playfabId = MainInstaller.ResolveServices().NetworkService.UserId;
			var ucsId = AuthenticationService.Instance.PlayerId;
			var logFileBytes = File.ReadAllBytes(FLog.GetCurrentLogFilePath());
			var playerDataBytes =
				Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_services.DataService.GetData<PlayerData>(), Formatting.Indented));
			var appDataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_services.DataService.GetData<AppData>(), Formatting.Indented));

			UserReportingService.Instance.AddAttachmentToReport("Log", "log.txt", logFileBytes, "text/plain");
			UserReportingService.Instance.AddAttachmentToReport("PlayerData", "player_data.json", playerDataBytes, "application/json");
			UserReportingService.Instance.AddAttachmentToReport("AppData", "app_data.json", appDataBytes, "application/json");

			UserReportingService.Instance.AddMetadata("playfab_user_id", playfabId);
			UserReportingService.Instance.AddMetadata("ucs_user_id", ucsId);

			await UserReportingService.Instance.CreateNewUserReportAsync();

			UserReportingService.Instance.AddDimensionValue("environment", FLEnvironment.Current.UCSEnvironmentName);
			UserReportingService.Instance.AddDimensionValue("category", _categoryField.text);

			UserReportingService.Instance.SetReportSummary(_summaryInput.value);
			UserReportingService.Instance.SetReportDescription(_descriptionInput.value);

			await UserReportingService.Instance.SendUserReportAsync(progress => _progressBar.value = progress * 0.9f);

			_progressBar.value = 1f;
			_progressBar.title = "Report sent! Thank you!";
		}
	}
}