using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements.Experimental;


[RequireComponent(typeof(Rigidbody))]
public class ProceduralWalkController : MonoBehaviour
{
    [Header("Leg data array")]
    [SerializeField]
    public LegData[] legData;

    [Header("Movement variables")]
    public float safeRadius;
    public float rayCastDistance;
    public LayerMask whatIsGround;

    [Header("Step profile")]
    
    [Tooltip("In second(s)")]
    public float stepTime = 1; 
    public float stepEndHeightOffset;
    public float stepMaxHeight;

    [Tooltip("Normalized (value 0 -> 1, time 0->1)")]
    public AnimationCurve stepHeightCurve; //With value 0 -> 1, time 0->1

    [Space]
    [Header("Readable")]
    public Quaternion suggestedBodyOrientation;
    public float suggestedBodyHeight;

    [Header("Debug")]
    [SerializeField]
    //Last move time
    private float[] _timingArray;
    
    //Allow movement this frame
    [SerializeField]
    private bool[] _moveCheckArray;

    [SerializeField]
    private bool[] _isMoving;

    //Locally calculated
    private Vector3 _lastPos;
    private Vector3 _velocity;
    void Start()
    {
        _timingArray = new float[legData.Length];
        _moveCheckArray = new bool[legData.Length];
        _isMoving = new bool[legData.Length];

        for (int i = 0; i < legData.Length; ++i)
        {
            _timingArray[i] = Time.unscaledTime + legData[i].timing * stepTime;
            _isMoving[i] = false;
        }
    }

    void FixedUpdate()
    {
        _velocity = transform.position - _lastPos;
        _lastPos = transform.position;

        TimeCheck();
        DistCheck();

        MoveStarter();

        ResetArray();
    }

    private void DebugPrint()
    {
        string dbgMsg = "";
        for (int i = 0; i < legData.Length; ++i)
        {
            dbgMsg += "Leg " + i + " moveCheck: " + _moveCheckArray[i] + " / ";
        }
        Debug.Log(dbgMsg);
    }

    private void MoveStarter()
    {
        for (int i = 0; i < legData.Length; i++)
        {
            if (!_moveCheckArray[i] || _isMoving[i]) continue;

            Transform dest = legData[i].IKDest.transform;

            RaycastHit[] surfaceHit = Physics.RaycastAll(dest.position, Vector3.down, rayCastDistance, whatIsGround);
            if (surfaceHit.Length <= 0) continue;

            RaycastHit hit = surfaceHit[0];

            //To move leg even further in case of body moving
            Vector3 velocityCompensation = Vector3.zero;
            if (_velocity.magnitude > Mathf.Epsilon)
                velocityCompensation = _velocity;
            velocityCompensation.y = 0;
            //velocityCompensation.Normalize();
            velocityCompensation *= safeRadius;

            _isMoving[i] = true;
            StartCoroutine(Step(i, hit.point + Vector3.up * stepEndHeightOffset + velocityCompensation));
        }
    }

    private IEnumerator Step(int legIndex, Vector3 endPos)
    {
        Transform target = legData[legIndex].IKTarget.transform;
        Vector3 startPos = target.position;
        for (float i = 0; i < stepTime; i += 1/stepTime * Time.fixedDeltaTime)
        {
            float progress = i / stepTime;

            target.position = Vector3.Lerp(startPos, endPos, progress);
            target.position += Vector3.up * stepMaxHeight * stepHeightCurve.Evaluate(progress);

            yield return new WaitForFixedUpdate();
        }
        _isMoving[legIndex] = false;
    }
    private void DistCheck()
    {
        for (int i = 0; i < legData.Length; i++)
        {
            Transform target = legData[i].IKTarget.transform;
            Transform dest = legData[i].IKDest.transform;

            RaycastHit[] surfaceHit = Physics.RaycastAll(dest.position, Vector3.down, rayCastDistance, whatIsGround);
            if (surfaceHit.Length <= 0) continue;

            Vector3 hitPosition = surfaceHit[0].point;

            //Debug
            Debug.DrawLine(target.position, hitPosition, UnityEngine.Color.red);
            //

            Vector3 projectedVec = target.position - hitPosition;
            projectedVec = Vector3.ProjectOnPlane(projectedVec, Vector3.up);
            float dist = projectedVec.magnitude;

            _moveCheckArray[i] &= (dist > safeRadius);
        }
    }

    private void TimeCheck()
    {
        for (int i = 0; i < legData.Length; i++)
        {
            _moveCheckArray[i] &= _timingArray[i] + stepTime * 2 <= Time.unscaledTime;
        }
    }

    private void ResetArray()
    {
        for (int i = 0; i < _moveCheckArray.Length; ++i)
        {
            _moveCheckArray[i] = true;

            if (_timingArray[i] + stepTime * 2 <= Time.unscaledTime)
            {
                _timingArray[i] = Time.unscaledTime + legData[i].timing * stepTime;
            }
        }
    }
}

[System.Serializable]
public class LegData
{
    public GameObject IKTarget;
    public GameObject IKDest;
    public float timing;

    LegData(){
        IKTarget = null;
        IKDest = null;
        timing = 0;
    }
}