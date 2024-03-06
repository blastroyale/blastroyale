using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using UnityEngine;
using Immutable.Passport;
using FirstLight.Game.Utils;
using System.Linq;
using System;
using Environment = Immutable.Passport.Model.Environment;
using Cysharp.Threading.Tasks.CompilerServices;
using FirstLight.Game.Presenters;


/// <summary>
/// Integrates IMX Passport to FLG Game
/// </summary>
public class FlgImxWeb3Service : MonoBehaviour, IWeb3Service
{
	/// <summary>
	/// For testing purposes, if proof key should be enabled or not
	/// </summary>
	public static bool PROOF_KEY = false;

	public const string REDIRECT_URI = "unitydl://callback";
	public const string LOGOUT_REDIRECT_URI = "unitydl://logout";

	private Web3State _state;
	private IGameServices _services;
	public Passport Passport { get; private set; }
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
		_services = await MainInstaller.WaitResolve<IGameServices>();
		MainInstaller.Bind<IWeb3Service>(this);
		Application.deepLinkActivated += OnDeepLink;
		_services.AuthenticationService.OnLogin += e => InitPassport().Forget();
	}

	private async UniTask InitPassport()
	{
		Passport = await Passport.Init(ImxClientId, EnvString, REDIRECT_URI, LOGOUT_REDIRECT_URI);
		State = Web3State.Available;
	}

	private string EnvString => _services.GameBackendService.IsDev() ?
				Environment.SANDBOX : Environment.PRODUCTION;

	public async UniTask<Web3State> OnLoginRequested()
	{
		if (State == Web3State.Authenticated)
		{
			_services.GenericDialogService.OpenButtonDialog("Logout Web3", "You are logged in, do you want to log out ?", true, new GenericDialogButton()
			{
				ButtonOnClick = () => OnLogoutRequested().Forget(),
				ButtonText = "Logout"
			});
			return State;
		}
		_services.GameUiService.OpenUi<LoadingSpinnerScreenPresenter>();
		try
		{
			await OpenLoginDialog();
		}
		catch (Exception e)
		{
			_services.GenericDialogService.OpenSimpleMessage("Web3 Error", e.Message);
			Debug.LogError(e);
		}
		await _services.GameUiService.CloseUi<LoadingSpinnerScreenPresenter>();
		return State;
	}

	public async UniTask<Web3State> OpenLoginDialog()
	{
		Debug.Log("[IMX] Login Requested");
		if (PROOF_KEY) await Passport.LoginPKCE();
		else
		{
			await Passport.Login();
			await Passport.ConnectEvm();
			_wallet = await GetOrCreateWallet();
		}
		State = Web3State.Authenticated;
		return State;
	}

	public async UniTaskVoid OnLogoutRequested()
	{
		if (PROOF_KEY) await Passport.LogoutPKCE();
		else await Passport.Logout();
		_wallet = null;
		State = Web3State.Available;
	}

	public async UniTask<string> GetOrCreateWallet() => (await Passport.ZkEvmRequestAccounts()).First();

	private void OnDeepLink(string url)
	{
		Debug.Log("[Imx] Deep Link Called: " + url);
		if (PROOF_KEY && url.StartsWith(REDIRECT_URI))
		{
			Passport.ConnectEvm().ContinueWith(GetOrCreateWallet).ContinueWith(wallet =>
			{
				_wallet = wallet;
				State = Web3State.Authenticated;
			});
		}
	}

	private string ImxClientId => _services.GameBackendService.CurrentEnvironmentData.Web3Id;

	public Web3State State
	{
		get => _state;
		set
		{
			if (value != _state)
			{
				Debug.Log($"[Imx] State: {_state} -> {value}");
				OnStateChanged?.Invoke(value);
			}
			_state = value;
		}
	}

	public string Web3Account => _wallet;
}

