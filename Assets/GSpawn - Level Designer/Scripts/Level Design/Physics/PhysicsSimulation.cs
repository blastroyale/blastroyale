#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public struct PhysicsSimulationConfig
    {
        public bool                                     simulate2D;
        public float                                    simulationTime;
        public float                                    simulationStep;
        public float                                    outOfBoundsYCoord;

        public static readonly PhysicsSimulationConfig  defaultConfig = new PhysicsSimulationConfig()
        {
            simulate2D              = false,
            simulationTime          = 10.0f,
            simulationStep          = 0.01f,
            outOfBoundsYCoord       = -10.0f,
        };
    }

    public class PhysicsSimulation : Singleton<PhysicsSimulation>
    {
        private List<PhysicsSimulationObjectMono>       _simObjects         = new List<PhysicsSimulationObjectMono>();
        private PhysicsSimulationConfig                 _config;
        private float                                   _simulationTime;
        private bool                                    _isRunning;

        public bool         isRunning           { get { return _isRunning; } }
        public float        simulationTime      { get { return _simulationTime; } }

        public void performInstantSimulation(PhysicsSimulationConfig config)
        {
            _isRunning          = true;
            _config             = config;
            _simulationTime     = 0.0f;
            if (_config.simulationStep < 0.0f) _config.simulationStep = 0.01f;

            while (_isRunning)
                update();
        }

        public void start(PhysicsSimulationConfig config)
        {
            _isRunning          = true;
            _config             = config;
            _simulationTime     = 0.0f;
            if (_config.simulationStep < 0.0f) _config.simulationStep = 0.01f;
        }

        public void stop()
        {
            if (!_isRunning) return;

            foreach(var simObject in _simObjects)
                PhysicsSimulationObjectMono.DestroyImmediate(simObject);
        
            _simObjects.Clear();
            _isRunning = false;
        }

        public void addObject(GameObject gameObject)
        {
            var simObject = UndoEx.addComponent<PhysicsSimulationObjectMono>(gameObject);
            simObject.onEnterSimulation();
            _simObjects.Add(simObject);
        }

        public void update()
        {
            if (!_isRunning) return;

            _simulationTime += _config.simulationStep;
            #if UNITY_2022_2_OR_NEWER
            var oldSimulationMode   = Physics.simulationMode;
            Physics.simulationMode  = SimulationMode.Script;
            Physics.Simulate(_config.simulationStep);
            Physics.simulationMode  = oldSimulationMode;
            #else
            Physics.autoSimulation = false;
            Physics.Simulate(_config.simulationStep);
            Physics.autoSimulation = true;
            #endif

            if (_config.simulate2D)
            {
                var oldSimulMode            = Physics2D.simulationMode;
                Physics2D.simulationMode    = SimulationMode2D.Script;
                Physics2D.Simulate(_config.simulationStep);
                Physics2D.simulationMode    = oldSimulMode;
            }

            for (int i = 0; i < _simObjects.Count;)
            {
                var simObject = _simObjects[i];
            
                // Note: Can happen if Undo/Redo is used.
                if (simObject == null)
                {
                    _simObjects.RemoveAt(i);
                    continue;
                }

                if (simObject.gameObject.transform.position.y <= _config.outOfBoundsYCoord)
                {
                    GameObject.DestroyImmediate(simObject.gameObject);
                    _simObjects.RemoveAt(i);
                    continue;
                }
                if (simObject.velocity.magnitude < 1e-5f)
                {
                    simObject.onExitSimulation();
                    PhysicsSimulationObjectMono.DestroyImmediate(simObject);
                    _simObjects.RemoveAt(i);
                    continue;
                }

                ++i;
            }
       
            if (_simulationTime >= _config.simulationTime ||
                _simObjects.Count == 0) stop();
        }
    }
}
#endif