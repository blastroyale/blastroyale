using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;


/// <summary>
/// Decoupled component to handle objects that produces footprints.
/// Will produce footprints when enabled.
/// </summary>
public class FootprinterMonoComponent : MonoBehaviour
{
	private static Queue<GameObject> _globalPool = new ();

	private ItemData _skin;
	private WaitForSeconds _duration = new (1.9f);
	private float _scaleDownEffect = 0.5f;

	private IGameServices _services;
	private IMatchServices _matchServices;
	private Vector3 _rightStepScale;
	private Vector3 _leftStepScale;
	private EntityView _view;
	private PlayerCharacterMonoComponent _character;

	private readonly Vector3 _footSpawnOffset = new (0f, 0.05f, 0f);


	// Local variables to avoid GC
	private GameObject _pooledFootprint;
	private Quaternion _localRotation;

	public bool SpawnFootprints;

	private void Start()
	{
		_services = MainInstaller.Resolve<IGameServices>();
		_matchServices = MainInstaller.Resolve<IMatchServices>();
		QuantumCallback.Subscribe<CallbackGameDestroyed>(this, OnGameDestroyed);
	}

	private void OnGameDestroyed(CallbackGameDestroyed e)
	{
		_globalPool.Clear();
	}

	public void Init(EntityView view, PlayerLoadout loadout)
	{
		var services = MainInstaller.Resolve<IGameServices>();
		_character = view.GetComponent<PlayerCharacterMonoComponent>();
		_view = view;
		_skin = services.CollectionService.GetCosmeticForGroup(loadout.Cosmetics, GameIdGroup.Footprint);

		_character.PlayerView.CharacterSkin.OnStepLeft += OnStepLeft;
		_character.PlayerView.CharacterSkin.OnStepRight += OnStepRight;
	}

	private bool CanSpawn()
	{
		return _character != null && _character.PlayerView != null && _view != null && SpawnFootprints &&
			_skin.Id != GameId.Random;
	}

	private void OnStepRight()
	{
		if (CanSpawn())
		{
			Spawn(_character.PlayerView.CharacterSkin.RightFootAnchor, true).Forget();
		}
	}

	private void OnStepLeft()
	{
		if (CanSpawn())
		{
			Spawn(_character.PlayerView.CharacterSkin.LeftFootAnchor, false).Forget();
		}
	}

	private Quaternion GetFootRotation()
	{
		var f = QuantumRunner.Default.Game.Frames.Predicted;

		// Need this hack because our code handles bots differently from players -.-
		if (_view.EntityRef.IsBot(f)) return _view.transform.rotation;

		
		if (f.TryGet<AIBlackboardComponent>(_view.EntityRef, out var bb) && bb.HasEntry(f, Constants.MOVE_DIRECTION_KEY))
		{
			return Quaternion.LookRotation(bb.GetVector2(f, Constants.MOVE_DIRECTION_KEY).ToUnityVector3());
		}

		return _view.transform.rotation;
	}

	private bool IsValid()
	{
		return _view != null && _character != null & _character.PlayerView != null && !_character.PlayerView.Culled;
	}

	/// <summary>
	/// Spawns the footstep.
	/// </summary>
	private async UniTaskVoid Spawn(Transform anchor, bool right)
	{
		if (!IsValid()) return;
		if (!QuantumRunner.Default.IsDefinedAndRunning()) return;

		if (_globalPool.Count > 0) _pooledFootprint = _globalPool.Dequeue();
		else _pooledFootprint = await _services.CollectionService.LoadCollectionItem3DModel(_skin);
		if (!IsValid() || _pooledFootprint == null)
		{
			Despawn(_pooledFootprint);
			return;
		}

		if (_rightStepScale == Vector3.zero)
		{
			_rightStepScale = _pooledFootprint.transform.localScale;
			_leftStepScale = new (-_rightStepScale.x, _rightStepScale.y, _rightStepScale.z);
		}

		_localRotation = GetFootRotation();
		_pooledFootprint.transform.position = anchor.position + _footSpawnOffset;
		_pooledFootprint.transform.localScale = right ? _rightStepScale : _leftStepScale;
		_pooledFootprint.transform.rotation = Quaternion.Euler(90, _localRotation.eulerAngles.y, 0);
		_pooledFootprint.SetActive(true);
		PlayEffects();
		StartCoroutine(DespawnCoroutine(_pooledFootprint));
	}

	private void PlayEffects()
	{
		var clip = _services.AudioFxService.PlayClip3D(AudioId.PlayerWalkRoad, _character.transform.position);
		if (_matchServices.SpectateService.GetSpectatedEntity() == _character.EntityView.EntityRef)
		{
			clip.Source.volume *= 0.75f;
			_services.VfxService.Spawn(VfxId.StepSmoke).transform.position = _pooledFootprint.transform.position;
		}
	}

	private IEnumerator DespawnCoroutine(GameObject o)
	{
		yield return _duration;
		var initialScale = o.transform.localScale;
		yield return o.transform.DOScale(0, _scaleDownEffect).SetAutoKill().WaitForCompletion();
		o.transform.localScale = initialScale;
		Despawn(o);
	}

	private void Despawn(GameObject o)
	{
		if (o == null || !o.activeSelf) return;
		o.SetActive(false);
		_globalPool.Enqueue(o);
	}
}