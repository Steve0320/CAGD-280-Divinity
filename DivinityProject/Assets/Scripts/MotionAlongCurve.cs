using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// Animate the owning object along the given AnimationCurve. When the end of the path is
/// reached, ImpactDelegate is called.
/// </summary>
public class MotionAlongCurve : MonoBehaviour
{

    public Vector3 Destination;
    public float TravelTime;
    public AnimationCurve HeightCurve;
    public bool DestroyAtDestination;
    public delegate void ImpactBehavior(MotionAlongCurve self);
    public ImpactBehavior ImpactDelegate;

    private Vector3 projectedPos;
    private float totalDist;
    private float speed;


    // Start is called before the first frame update
    void Start()
    {
        totalDist = Vector3.Magnitude(Destination - this.transform.position);
        speed = totalDist / TravelTime;
        projectedPos = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {

        // Move torwards destination
        projectedPos = Vector3.MoveTowards(projectedPos, Destination, Time.deltaTime * speed);

        float perc = 1.0f - Vector3.Magnitude(projectedPos - Destination) / totalDist;
        Vector3 pos = new(projectedPos.x, projectedPos.y + HeightCurve.Evaluate(perc), projectedPos.z);
        this.transform.position = pos;

        if (pos == Destination)
        {
            ImpactDelegate(this);
            if (DestroyAtDestination)
            {
                GameObject.Destroy(this.gameObject);
            }
        }
        
    }
}
