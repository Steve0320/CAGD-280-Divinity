using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerMovement : MonoBehaviour
{

    public Player CurrentPlayer;
    public Camera CurrentCamera;
    // public LayerMask MovementLayers;

    public float RotationSpeed = 10.0f;
    public float MovementSpeed = 10.0f;

    public DecalProjector MovementIconProjector;
    public float ProjectionOffset = 0.9f;

    // public MeshFilter NavigationMesh;
    public GameObject NavigationMesh;

    /// <summary>
    /// The offset from which to cast rays to detect the Y coordinate during movement. Usually this should be approximately the
    /// player's height. Too high and it would break overhangs, and too low and it may not hit the nav mesh on inclines.
    /// </summary>
    public float MovementRaycastOffset = 3.0f;

    private bool isRotating;
    private bool isMoving;
    private Vector3 playerTarget;
    private Quaternion targetRotation;

    // Pieces of NavMesh broken out for easy access
    private Mesh navMesh;
    private MeshCollider navCollider;
    private Transform navTransform;
    private Pathfinder navPathfinder;

    private Queue<Vector3> waypoints;

    void Start()
    {

        MovementIconProjector.enabled = false;

        navCollider = NavigationMesh.GetComponent<MeshCollider>();
        navTransform = NavigationMesh.GetComponent<Transform>();
        navMesh = navCollider.sharedMesh;

        // Set up the navmesh for future pathfinding
        navPathfinder = new Pathfinder(navMesh, navTransform);

        // Debug info
        // navPathfinder.ShowDebugCells(Color.green);
        navPathfinder.ShowDebugCenters(Color.red);
        navPathfinder.ShowDebugGraph(Color.blue);

    }

    // Update is called once per frame
    void Update()
    {

        Transform playerTransform = CurrentPlayer.gameObject.transform;

        if (isRotating)
        {
            playerTransform.rotation = Quaternion.RotateTowards(playerTransform.rotation, targetRotation, Time.deltaTime * 100.0f * RotationSpeed);
            if (playerTransform.rotation == targetRotation)
            {
                isRotating = false;
            }
        }

        if (isMoving)
        {

            Vector3 playerPosition = Vector3.MoveTowards(playerTransform.position, playerTarget, Time.deltaTime * MovementSpeed);

            // To keep the player snapped to the movement mesh while in motion, we cast a ray downward and use that Y value. This should also
            // work with overhangs as long as they're not closer than MovementRaycastOffset units.
            Vector3 yRayOrigin = playerPosition + new Vector3(0, MovementRaycastOffset, 0);
            Ray yRay = new(yRayOrigin, Vector3.down);
            if (navCollider.Raycast(yRay, out RaycastHit rayHit, Mathf.Infinity))
            {
                playerPosition.y = rayHit.point.y;
            }

            CurrentPlayer.Animator.SetFloat("Speed", MovementSpeed);

            // Ending actions when the player arrives at the target.
            if (playerPosition == playerTarget)
            {

                // Actions to take on final target point
                if (waypoints.Count == 0)
                {
                    isMoving = false;
                    CurrentPlayer.Animator.SetFloat("Speed", 0.0f);
                    MovementIconProjector.enabled = false;
                }
                else
                {
                    SetPlayerTarget(waypoints.Dequeue());
                }
                
            }

            playerTransform.position = playerPosition;

        }
        
    }

    private void SetPlayerTarget(Vector3 targetPoint)
    {

        // Set location
        playerTarget = targetPoint;

        // Compute target angle
        Vector3 lookDirection = playerTarget - CurrentPlayer.gameObject.transform.position;
        if (lookDirection != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(playerTarget - CurrentPlayer.gameObject.transform.position);
        }

        // Enable movement updates
        isRotating = true;
        isMoving = true;

    }

    public void OnMovementClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            
            // Raycast the click on to the nearest moveable surface
            Vector2 mousePos = Input.mousePosition;
            Ray ray = CurrentCamera.ScreenPointToRay(mousePos);

            if (navCollider.Raycast(ray, out RaycastHit rayHit, Mathf.Infinity))
            {

                // Vector3 projectorPosition = playerTarget;
                // projectorPosition.y += ProjectionOffset;
                // MovementIconProjector.gameObject.transform.position = projectorPosition;
                // MovementIconProjector.enabled = true;

                // DEBUG
                // Vector3 p0 = navMesh.vertices[navMesh.triangles[rayHit.triangleIndex] * 3 + 0];
                // Vector3 p1 = navMesh.vertices[navMesh.triangles[rayHit.triangleIndex] * 3 + 1];
                // Vector3 p2 = navMesh.vertices[navMesh.triangles[rayHit.triangleIndex] * 3 + 2];
                // print("Hit point was: " + rayHit.point);
                // print("Enclosing cell should be: " + navTransform.TransformPoint(p0) + " | " + navTransform.TransformPoint(p1) + " | " + navTransform.TransformPoint(p2));

                // Compute path
                waypoints = navPathfinder.ComputePath(CurrentPlayer.gameObject.transform.position, rayHit.point);

                // TODO: probably should do something in the UI here
                if (waypoints.Count == 0)
                {
                    print("No path found.");
                }
                else
                {
                    SetPlayerTarget(waypoints.Dequeue());
                }

            }
        }
    }

}
