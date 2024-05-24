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
using FirstLight.FLogger;


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
		_services = await MainInstaller.WaitResolve<IGameServices>();
		FLog.Info("[IMX Web3]", "Initializing");
		Application.deepLinkActivated += OnDeepLink;
		_services.AuthenticationService.OnLogin += e => InitPassport().Forget();
	}

	private async UniTask InitPassport()
	{
		Passport = await Passport.Init(ImxClientId, EnvString, REDIRECT_URI, LOGOUT_REDIRECT_URI);
		MainInstaller.Bind<IWeb3Service>(this);
		State = Web3State.Available;
		if (await Passport.HasCredentialsSaved())
		{
			FLog.Info("[IMX Web3]", "Imx has credentials saved, auto-logging in");
			if (await Passport.Login(true))
			{
				await ConnectBlockchain();
			}
		}
	}

	private string EnvString => _services.GameBackendService.IsDev() ? Environment.SANDBOX : Environment.PRODUCTION;

	public async UniTask<Web3State> RequestLogin()
	{
		if (State == Web3State.Authenticated)
		{
			_services.GenericDialogService.OpenButtonDialog("Logout Web3", "You are logged in, do you want to log out ?", true,
				new GenericDialogButton()
				{
					ButtonOnClick = () => RequestLogout().Forget(),
					ButtonText = "Logout"
				}).Forget();
			return State;
		}

		await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
		try
		{
			await OpenLoginDialog();
		}
		catch (Exception e)
		{
			_services.GenericDialogService.OpenSimpleMessage("Web3 Error", e.Message).Forget();
			Debug.LogError(e);
		}
		finally
		{
			await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
		}

		return State;
	}

	public async UniTask<Web3State> OpenLoginDialog()
	{
		FLog.Verbose("[IMX Web3]", "Login Requested");
		if (PROOF_KEY) await Passport.LoginPKCE();
		else
		{
			if (!await Passport.Login())
			{
				return State;
			}

			await ConnectBlockchain();
		}

		State = Web3State.Authenticated;
		return State;
	}

	public async UniTaskVoid RequestLogout()
	{
		if (PROOF_KEY) await Passport.LogoutPKCE();
		else await Passport.Logout();
		_wallet = null;
		State = Web3State.Available;
	}

	public async UniTask<string> GetOrCreateWallet() => (await Passport.ZkEvmRequestAccounts()).First();

	private async UniTask ConnectBlockchain()
	{
		await Passport.ConnectEvm();
		_wallet = await GetOrCreateWallet();
		State = Web3State.Authenticated;
	}

	private void OnDeepLink(string url)
	{
		FLog.Verbose("[IMX Web3]", "Deep Link Called " + url);
		if (PROOF_KEY && url.StartsWith(REDIRECT_URI))
		{
			ConnectBlockchain().Forget();
		}
	}

	private string ImxClientId => FLEnvironment.Current.Web3ID;

	public Web3State State
	{
		get => _state;
		set
		{
			if (value != _state)
			{
				OnStateChanged?.Invoke(value);
			}

			_state = value;
		}
	}

	public string Web3Account => _wallet;
}