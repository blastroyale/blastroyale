using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.UserReporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	[UILayer(UILayer.Debug)]
	public class UserReportScreenPresenter : UIPresenter
	{
		private readonly List<string> _categories = new ()
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
		private ButtonOutlined _sendButton;
		private ProgressBar _progressBar;
		private VisualElement _screenshot;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();

			_summaryInput = Root.Q<TextField>("SummaryInput").Required();
			_descriptionInput = Root.Q<TextField>("DescriptionInput").Required();
			_reportContainer = Root.Q<ImageButton>("ReportContainer").Required();
			_categoryField = Root.Q<DropdownField>("CategoryDropdown").Required();
			_sendButton = Root.Q<ButtonOutlined>("SendButton").Required();
			_progressBar = Root.Q<ProgressBar>("ProgressBar").Required();
			_screenshot = Root.Q<VisualElement>("Screenshot").Required();

			_sendButton.Required().clicked += () => SendReport().Forget();
			Root.Q<ImageButton>("ExitButton").clicked += ClosePopup;

			Root.Q<ImageButton>("Icon").Required().clicked += () => OnOpenReportClicked().Forget();

			if (Debug.isDebugBuild)
			{
				// Development build default to "Internal" category since we don't really need categories for internal reports.
				_categories.Insert(0, "Internal");
				_categoryField.value = _categories[0];
			}

			_categoryField.choices = _categories;

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
			_categoryField.value = _categories[0];
			_progressBar.SetVisibility(false);
			_screenshot.style.backgroundImage = null;
			_descriptionInput.value = string.Empty;
			_summaryInput.value = string.Empty;
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

			if (!Debug.isDebugBuild && string.IsNullOrWhiteSpace(_descriptionInput.value))
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

			FLog.Info("Gathering data for user report");
			var playfabId = MainInstaller.ResolveServices().NetworkService.UserId;
			var ucsId = AuthenticationService.Instance.PlayerId;
			var logFileBytes = await File.ReadAllBytesAsync(FLog.GetCurrentLogFilePath());
			var playerDataBytes =
				Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_services.DataService.GetData<PlayerData>(), Formatting.Indented));
			var appDataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_services.DataService.GetData<AppData>(), Formatting.Indented));

			FLog.Info("Adding data to user report");
			UserReportingService.Instance.AddAttachmentToReport("Log", "log.txt", logFileBytes, "text/plain");
			UserReportingService.Instance.AddAttachmentToReport("PlayerData", "player_data.json", playerDataBytes, "application/json");
			UserReportingService.Instance.AddAttachmentToReport("AppData", "app_data.json", appDataBytes, "application/json");

			UserReportingService.Instance.AddMetadata("playfab_user_id", playfabId);
			UserReportingService.Instance.AddMetadata("ucs_user_id", ucsId);

			FLog.Info("Creating user report");
			await UserReportingService.Instance.CreateNewUserReportAsync();

			UserReportingService.Instance.AddDimensionValue("environment", FLEnvironment.Current.UCSEnvironmentName);
			UserReportingService.Instance.AddDimensionValue("category", _categoryField.text);

			UserReportingService.Instance.SetReportSummary(_summaryInput.value);
			UserReportingService.Instance.SetReportDescription(_descriptionInput.value);

			try
			{
				FLog.Info("Sending user report");
				await UserReportingService.Instance.SendUserReportAsync(progress => _progressBar.value = progress * 0.9f);
				_progressBar.value = 1f;
				_progressBar.title = "Report sent! Thank you!";
				FLog.Info("User report sent!");
			}
			catch (Exception)
			{
				_progressBar.value = 1f;
				_progressBar.title = "Error sending report. Please try again.";
				FLog.Error("Error sending user report :(");
			}
		}
	}
}