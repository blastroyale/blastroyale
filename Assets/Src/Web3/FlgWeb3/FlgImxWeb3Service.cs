using Cysharp.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using UnityEngine;
using Immutable.Passport;
using FirstLight.Game.Utils;
using Cysharp.Threading.Tasks.CompilerServices;
using PlayFab.ClientModels;

/// <summary>
/// Integrates IMX Passport to FLG Game
/// </summary>
public class FlgImxWeb3Service : MonoBehaviour, IWeb3Service
{
	public const string redirectUri = "unitydl://callback";
	public const string logoutRedirectUri = "unitydl://logout";

	private IGameServices _services;
	private Passport _passport;
	private bool _connected;
	private bool _initialized;

	void Start()
	{
		StartAsync().Forget();
	}

	public bool IsConnected => _connected;

	public bool IsServiceAvailable => !string.IsNullOrEmpty(ImxClientId);

	/// <summary>
	/// Opens IMX login dialog async
	/// </summary>
	public bool OpenWeb3()
	{
		Debug.Log("[Imx] Opening Web3");
		OpenPassaportDialog().Forget();
		return true;
	}

	private async UniTaskVoid StartAsync()
	{
		MainInstaller.Bind<IWeb3Service>(this);
		_services = await MainInstaller.WaitResolve<IGameServices>();
	}

	private async UniTaskVoid OpenPassaportDialog()
	{
		if(!_initialized)
		{
			await InitializeImx();
		}
		await _passport.ConnectEvm();
		var accounts = await _passport.ZkEvmRequestAccounts();
		foreach (var a in accounts) Debug.Log("Account: " + a);
		_connected = true;
	}
	
	private async UniTask InitializeImx()
	{
		Debug.Log("[Imx] Initializing imx web3 passport");
		string environment = _services.GameBackendService.IsDev() ? 
			Immutable.Passport.Model.Environment.SANDBOX : 
			Immutable.Passport.Model.Environment.PRODUCTION;
		_passport = await Passport.Init(ImxClientId, environment, redirectUri, logoutRedirectUri);
		_services.MessageBrokerService.Subscribe<PassportCheck>(OnPassportCheck);
		Application.deepLinkActivated += OnDeepLink;
		await _passport.Login();
		_initialized = true;
		Debug.Log("[Imx] Passport finished setup");
	}

	private void OnDeepLink(string url)
	{
		Debug.Log("[Imx] Deep Link Called: "+url);
		if (url.Contains("login"))
		{
			_services.GenericDialogService.OpenSimpleMessage("Imx", "Login Deeplink answered LOGIN "+url);
		}else if (url.Contains("logout"))
		{
			_services.GenericDialogService.OpenSimpleMessage("Imx", "Login Deeplink answered LOGOUT "+url);
		}
	}

	private void OnPassportCheck(PassportCheck check)
	{
		if (!_connected)
		{ 
			OpenPassaportDialog().Forget();
		}
		else
		{
			_services.GenericDialogService.OpenSimpleMessage("Test", "You already have passport connected");
		}
	}

	private string ImxClientId => _services.GameBackendService.CurrentEnvironmentData.Web3Id;

	public Web3State State
	{
		get
		{
			if (_connected && _initialized)
				return Web3State.Ready;
			else return Web3State.Initializing;
		}
	}
}

