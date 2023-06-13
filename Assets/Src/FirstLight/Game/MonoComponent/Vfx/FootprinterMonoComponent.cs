using System;
using System.Collections;
using System.Collections.Generic;
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
    private GameId _id;
    private WaitForSeconds _duration = new (4);
    private Cooldown _cooldown = new (TimeSpan.FromMilliseconds(300));
    private Queue<GameObject> _pool = new();
    private Vector3 _localPositionOffset = new (0, 0.1f, 0);
    private IGameServices _services;
    private Vector3 _stepVariation = new(-0.1f, 0, 0);
    private Vector3 _stepScale;
    private Vector3 _stepInverseScale;
    private EntityView _view;

    public bool SpawnFootprints;
    
    private void Start()
    {
        _services = MainInstaller.Resolve<IGameServices>();
    }

    public void Init(EntityView view, PlayerLoadout loadout)
    {
        _view = view;
        _id = loadout.Footstep;
    }
    
    /// <summary>
    /// Sets the sprite of the footprint that shall be used
    /// </summary>
    public GameId FootprintId
    {
        get => _id;
        set
        {
            _id = value;
            _pool.Clear();
        }
    }
    
    private void Update()
    {
        if (_view != null && SpawnFootprints && _id != GameId.Random && _cooldown.CheckTrigger()) Spawn();
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
        GameObject o;
        if (_pool.Count > 0) o = _pool.Dequeue();
        else  o = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(_id);
        if (_stepScale == Vector3.zero) _stepScale = o.transform.localScale;
        _stepVariation *= -1;
        _stepScale = new(-_stepScale.x, _stepScale.y, _stepScale.z);
        var localTransform = _view.transform;
        var footRotation = GetFootRotation();
        o.transform.position = localTransform.position + _localPositionOffset + (footRotation * _stepVariation);
        o.transform.localScale = _stepScale;
        o.transform.rotation = Quaternion.Euler(90, footRotation.eulerAngles.y, 0);
        o.SetActive(true);
        StartCoroutine(Despawn(o));
    }

    private IEnumerator Despawn(GameObject o)
    {
        yield return _duration;
        if (!o.activeSelf) yield break;
        o.SetActive(false);
        _pool.Enqueue(o);
    }
}
