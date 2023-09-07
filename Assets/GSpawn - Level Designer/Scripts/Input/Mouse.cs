#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class Mouse : Singleton<Mouse>
    {
        public class DeltaCapture
        {
            private int         _id;
            private Vector2     _origin;

            public int          id      { get { return _id; } }
            public Vector2      origin  { get { return _origin; } }
            public Vector2      delta   { get { return Mouse.instance.position - _origin; } }

            public DeltaCapture(int id, Vector3 origin)
            {
                _id = id;
                _origin = origin;
            }
        }

        private bool[]                  _buttonStates   = new bool[3];
        private List<DeltaCapture>      _deltaCaptures  = new List<DeltaCapture>();

        public Vector2                  position        { get { return Event.current.mousePosition; } }
        public Vector2                  positionYUp
        {
            get
            {
                Vector2 pos = position;
                pos.y = PluginCamera.camera.pixelHeight - position.y;
                return pos;
            }
        }
        public Ray                      pickRay         { get { return HandleUtility.GUIPointToWorldRay(Event.current.mousePosition); } }
        public bool                     hasMoved        { get { return Event.current.delta.magnitude != 0.0f; } }
        public Vector2                  delta           { get { return Event.current.delta; } }
        public bool                     anyButtonsDown  { get { return _buttonStates[0] || _buttonStates[1] || _buttonStates[2]; } }
        public bool                     noButtonsDown   { get { return !_buttonStates[0] && !_buttonStates[1] && !_buttonStates[2]; } }

        public void onButtonDown(int button)
        {
            _buttonStates[button] = true;
        }

        public void onButtonUp(int button)
        {
            _buttonStates[button] = false;
        }

        public bool isButtonDown(int button)
        {
            return _buttonStates[button];
        }

        public int createDeltaCapture(Vector2 deltaOrigin)
        {
            int deltaCaptureId = 0;
            while (_deltaCaptures.FindAll(item => item.id == deltaCaptureId).Count != 0) ++deltaCaptureId;

            var deltaCapture = new DeltaCapture(deltaCaptureId, deltaOrigin);
            _deltaCaptures.Add(deltaCapture);

            return deltaCaptureId;
        }

        public void removeDeltaCapture(int deltaCaptureId)
        {
            _deltaCaptures.RemoveAll(item => item.id == deltaCaptureId);
        }

        public Vector2 deltaFromCapture(int deltaCaptureId)
        {
            var deltaCaptures = _deltaCaptures.FindAll(item => item.id == deltaCaptureId);
            return deltaCaptures.Count != 0 ? deltaCaptures[0].delta : Vector2.zero;
        }
    }
}
#endif