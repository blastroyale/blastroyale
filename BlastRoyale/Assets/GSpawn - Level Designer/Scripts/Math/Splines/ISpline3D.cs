#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public interface ISpline3D
    {
        int     numControlPoints        { get; }

        void    clear                   ();
        void    copy                    (ISpline3D src);
        void    setControlPoint         (int index, Vector3 controlPt);
        void    moveControlPoint        (int index, Vector3 offset);
        void    moveControlPoints       (Vector3 offset);
        void    addControlPoint         (Vector3 controlPt);
        void    insertControlPoint      (int index, Vector3 controlPt);
        void    removeControlPoint      (int index);
        void    removeLastControlPoint  ();

        Vector3 evalPosition            (float t);
        void    evalPositions           (List<Vector3> positions, float step);

        Vector3 getControlPoint         (int index);
        Vector3 getSegmentStart         (int segmentIndex);
        Vector3 getSegmentEnd           (int segmentIndex);
    }
}
#endif