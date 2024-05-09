using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// This is a "spell" that moves the player. It's not really a spell, but it fits well into that framework.
/// This also handles drawing the navigation path. Since this path drawing can spawn a lot of prefabs, we
/// use a basic object pooling implementation to keep things more reasonable.
/// </summary>
public class WalkTo : Spell
{

    public GameObject TargetPrefab;
    public GameObject DotPrefab;
    public float IconOffset = 0.9f;
    public float PathDotSeparation = 1.0f;
    public int InitialDotPoolAmount = 100;

    private Queue<Vector2> currentWaypoints = new();
    private Coroutine pathLoop;
    private GameObject target;
    private Vector3 lastHitPos;
    private bool lastHitValid;
    private List<GameObject> pathMarkers = new();
    
    public override void OnSelected()
    {

        pathLoop = StartCoroutine(pathCalc());

        // Spawn target
        target = GameObject.Instantiate(TargetPrefab);
        target.SetActive(false);

        // Init pool so we don't have to create/destroy stuff every frame
        for (int i = 0; i < InitialDotPoolAmount; i++)
        {
            GameObject dot = GameObject.Instantiate(DotPrefab);
            dot.SetActive(false);
            pathMarkers.Add(dot);
        }

    }

    public override void OnIdleTick()
    {

        // Move target decal
        if (lastHitValid)
        {

            target.transform.position = lastHitPos;
            target.SetActive(true);

            // Draw path
             if (currentWaypoints.Count > 0)
             {

                List<Vector3> pointPositions = new();

                Vector2 anchor = new(Caster.transform.position.x, Caster.transform.position.z);

                foreach (Vector2 point in currentWaypoints)
                {

                    // Step in direction of next point
                    Vector2 nextPoint = anchor;
                    Vector2 step = (point - anchor).normalized * PathDotSeparation;
                    float dist = (point - anchor).magnitude;

                    // Mark points until we go past the waypoint
                    while ((nextPoint - anchor).magnitude < dist)
                    {
                        pointPositions.Add(new(nextPoint.x, lastHitPos.y + IconOffset, nextPoint.y));
                        nextPoint += step;
                    }

                    anchor = point;

                }

                // Move existing points, hiding or creating as necessary
                int i;
                for (i = 0; i < pointPositions.Count; i++)
                {

                    GameObject dot;

                    if (i >= pathMarkers.Count)
                    {
                        dot = GameObject.Instantiate(DotPrefab);
                        pathMarkers.Add(dot);
                    }
                    else
                    {
                        dot = pathMarkers[i];
                    }

                    dot.SetActive(true);
                    dot.transform.position = pointPositions[i];

                }

                while (i < pathMarkers.Count)
                {
                    pathMarkers[i].SetActive(false);
                    i += 1;
                }

             }

             else
             {
                cleanupPath();
             }

        }
        else
        {
            target.SetActive(false);
            cleanupPath();
        }

    }

    public override void OnCast()
    {
        
        // Using the last computed path, move the player
        MovementManager.Instance.SetPlayerPath(currentWaypoints);

    }

    public override void OnCleanup()
    {
        StopCoroutine(pathLoop);
        GameObject.Destroy(target);
        foreach (GameObject dot in pathMarkers)
        {
            GameObject.Destroy(dot);
        }
        target = null;
    }

    private IEnumerator pathCalc()
    {

        while (true)
        {

            Vector2 mousePos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (MovementManager.Instance && MovementManager.Instance.GetNavCollider() && MovementManager.Instance.GetNavCollider().Raycast(ray, out RaycastHit rayHit, Mathf.Infinity))
            {
                lastHitPos = rayHit.point;
                lastHitValid = true;
                currentWaypoints = MovementManager.Instance.ComputePlayerPath(rayHit.point);
            }
            else
            {
                lastHitValid = false;
                currentWaypoints = new();
            }

            yield return new WaitForEndOfFrame();

        }

    }

    private void cleanupPath()
    {

        foreach (GameObject dot in pathMarkers)
        {
            dot.SetActive(false);
        }

    }

}
