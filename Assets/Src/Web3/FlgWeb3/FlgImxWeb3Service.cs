using Cysharp.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using UnityEngine;
using Immutable.Passport;
using FirstLight.Game.Utils;
using System.Linq;
using System;
using Environment = Immutable.Passport.Model.Environment;
using PlayFab.Public;

/// <summary>
/// Integrates IMX Passport to FLG Game
/// </summary>
public class FlgImxWeb3Service : MonoBehaviour, IWeb3Service
{
	public const string redirectUri = "unitydl://callback";
	public const string logoutRedirectUri = "unitydl://logout";

	private Web3State _state;
	private IGameServices _services;
	private Passport _passport;
	private string _wallet;

	public event Action<Web3State> OnStateChanged;

	private void Awake()
	{
		GameObject.DontDestroyOnLoad(this.gameObject);
		StartAsync().Forget();
	}

	private async UniTaskVoid StartAsync()
	{
		Debug.Log("[IMX] Initializing");
		MainInstaller.Bind<IWeb3Service>(this);
		Application.deepLinkActivated += OnDeepLink;
		_services = await MainInstaller.WaitResolve<IGameServices>();
		_services.AuthenticationService.OnLogin += e => InitPassport().Forget();
	}

	private async UniTask InitPassport()
	{
		_passport = await Passport.Init(ImxClientId, EnvString, redirectUri, logoutRedirectUri);
		State = Web3State.Available;
	}

	private string EnvString => _services.GameBackendService.IsDev() ?
				Environment.SANDBOX : Environment.PRODUCTION;

	public bool IsServiceAvailable => !string.IsNullOrEmpty(ImxClientId);

	public async UniTask<Web3State> OnLoginRequested()
	{
		Debug.Log("[IMX] Checking Passport Status");
		await _passport.Login();
		await _passport.ConnectEvm();
		_wallet = await GetOrCreateWallet();
		State = Web3State.Authenticated;

#if DEBUG
		Debug.Log($"[Imx] Token: {await _passport.GetAccessToken()}");
		Debug.Log($"[Imx] Address: {await _passport.GetAddress()}");
		Debug.Log($"[Imx] Email: {await _passport.GetEmail()}");
		Debug.Log($"[Imx] IdToken: {await _passport.GetIdToken()}");
#endif
		return State;
	}

	public async UniTaskVoid OnLogoutRequested()
	{
		await _passport.Logout();
		_wallet = null;
		State = Web3State.Available;
	}

	public async UniTask<string> GetOrCreateWallet()
	{
		var wallets = await _passport.ZkEvmRequestAccounts();
		Debug.Log("[IMX] User Wallets: " + string.Join(',', wallets));
		if(wallets.Count == 0)
		{
			throw new Exception("Something wrong with ZkEvmRequestAccounts, user should never have no wallet");
		}
		return wallets.First();
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

	private string ImxClientId => _services.GameBackendService.CurrentEnvironmentData.Web3Id;

	public Web3State State
	{
		get => _state; 
		set {
			if(value != _state)
			{
				Debug.Log($"[Imx] State: {_state} -> {value}");
				OnStateChanged?.Invoke(value);
			}
			_state = value;
		}
	}

	public string Web3Account => _wallet;
}

