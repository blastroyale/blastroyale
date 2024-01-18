using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Decoupled component to handle objects that produces footprints.
/// Will produce footprints when enabled.
/// </summary>
public class FootprinterMonoComponent : MonoBehaviour
{
    private static Queue<GameObject> _globalPool = new(); 
    private static readonly Vector3 _rightStepVariation = new(-0.1f, 0, 0);
    private static readonly Vector3 _leftStepVariation = new(0.1f, 0, 0);

    private ItemData _skin;
    private WaitForSeconds _duration = new (2.4f);
    private Cooldown _cooldown = new (TimeSpan.FromMilliseconds(300));
    
    private Vector3 _localPositionOffset = new (0, 0.17f, 0);
    private IGameServices _services;
    private IMatchServices _matchServices;
    private Vector3 _rightStepScale;
    private Vector3 _leftStepScale;
    private EntityView _view;
    private PlayerCharacterMonoComponent _character;
    private bool _right;
    
    // Local variables to avoid GC
    private GameObject _pooledFootprint;
    private Transform _localTransform;
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
    }

    private void Update()
    {
        if (_character != null && _view != null && SpawnFootprints && _skin.Id != GameId.Random && _cooldown.CheckTrigger())
        {
            Spawn().Forget();
        }
    }

    private Quaternion GetFootRotation()
    {
        var f = QuantumRunner.Default.Game.Frames.Predicted;

        // Need this hack because our code handles bots differently from players -.-
        if (_view.EntityRef.IsBot(f)) return _view.transform.rotation;
        
        if (f.TryGet<AIBlackboardComponent>(_view.EntityRef, out var bb) && bb.HasEntry(f, Constants.MoveDirectionKey))
        {
            // TODO: Use lookup table for performance
            return Quaternion.LookRotation(bb.GetVector2(f, Constants.MoveDirectionKey).ToUnityVector3());
        }
        return _view.transform.rotation; 
    }

    /// <summary>
    /// Spawns the footstep.
    /// </summary>
    private async UniTaskVoid Spawn()
    {
        if (_character.PlayerView.Culled) return;
        if (!QuantumRunner.Default.IsDefinedAndRunning()) return;
        
        if (_globalPool.Count > 0) _pooledFootprint = _globalPool.Dequeue();
        else  _pooledFootprint = await _services.CollectionService.LoadCollectionItem3DModel(_skin);
        if (_rightStepScale == Vector3.zero)
        {
            _rightStepScale = _pooledFootprint.transform.localScale;
            _leftStepScale = new(-_rightStepScale.x, _rightStepScale.y, _rightStepScale.z);
        }
        _right = !_right;
        _localTransform = _view.transform;
        _localRotation = GetFootRotation();
        _pooledFootprint.transform.position = _localTransform.position + _localPositionOffset + (_localRotation * (_right ? _rightStepVariation : _leftStepVariation));
        _pooledFootprint.transform.localScale = _right ? _rightStepScale : _leftStepScale;
        _pooledFootprint.transform.rotation = Quaternion.Euler(90, _localRotation.eulerAngles.y, 0);
        _pooledFootprint.SetActive(true);
        PlayEffects();
        StartCoroutine(Despawn(_pooledFootprint));
    }

    private void PlayEffects()
    {
        var clip = _services.AudioFxService.PlayClip3D(AudioId.PlayerWalkRoad, _character.transform.position);
        if (_matchServices.SpectateService.GetSpectatedEntity() == _character.EntityView.EntityRef)
        {
            clip.Source.volume /= 3;
            _services.VfxService.Spawn(VfxId.StepSmoke).transform.position = _pooledFootprint.transform.position;
        }
    }

    private IEnumerator Despawn(GameObject o)
    {
        yield return _duration;
        if (!o.activeSelf) yield break;
        o.SetActive(false);
        _globalPool.Enqueue(o);
    }
}
