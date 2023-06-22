using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Commands;
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
    
    private GameId _id;
    private WaitForSeconds _duration = new (4);
    private Cooldown _cooldown = new (TimeSpan.FromMilliseconds(300));
    
    private Vector3 _localPositionOffset = new (0, 0.1f, 0);
    private IGameServices _services;
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
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnSceneChanged(Scene newScene, Scene oldScene)
    {
        _globalPool.Clear();
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    public void Init(EntityView view, PlayerLoadout loadout)
    {
        _character = view.GetComponent<PlayerCharacterMonoComponent>();
        _view = view;
        _id = loadout.Footstep;
    }

    private void Update()
    {
        if (_character != null && _view != null && SpawnFootprints && _id != GameId.Random && _cooldown.CheckTrigger()) Spawn();
    }

    private Quaternion GetFootRotation()
    {
        var f = QuantumRunner.Default.Game.Frames.Predicted;

        // Need this hack because our code handles bots differently from players -.-
        if (_view.EntityRef.IsBot(f)) return _view.transform.rotation;
        
        if (f.TryGet<AIBlackboardComponent>(_view.EntityRef, out var bb) && bb.HasEntry(f, Constants.MoveDirectionKey))
        {
            return Quaternion.LookRotation(bb.GetVector2(f, Constants.MoveDirectionKey).ToUnityVector3());
        }
        return _view.transform.rotation; 
    }

    /// <summary>
    /// Spawns the footstep.
    /// </summary>
    private async void Spawn()
    {
        if (_character.PlayerView.Culled) return;
        
        if (_globalPool.Count > 0) _pooledFootprint = _globalPool.Dequeue();
        else  _pooledFootprint = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(_id);
        if (!QuantumRunner.Default.IsDefinedAndRunning()) return;
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
        StartCoroutine(Despawn(_pooledFootprint));
    }

    private IEnumerator Despawn(GameObject o)
    {
        yield return _duration;
        if (!o.activeSelf) yield break;
        o.SetActive(false);
        _globalPool.Enqueue(o);
    }
}
