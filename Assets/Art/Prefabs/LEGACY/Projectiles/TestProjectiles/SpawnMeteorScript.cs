using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnMeteorScript : MonoBehaviour
{
    public GameObject vfx;
    public Transform startPoint;
    public Transform endPoint;


    void Start()
    {
        var startPos = startPoint.position;
        GameObject objVFX = Instantiate(vfx, startPos, Quaternion.identity) as GameObject;

        var endPos = endPoint.position;

        RotateTo (objVFX, endPos);
    }

    void RotateTo (GameObject obj, Vector3 destination)
    {
        var direction = destination - obj.transform.position;
        var rotation = Quaternion.LookRotation(direction);
        obj.transform.localRotation = Quaternion.Lerp (obj.transform.rotation, rotation, 1);
    }
 }
