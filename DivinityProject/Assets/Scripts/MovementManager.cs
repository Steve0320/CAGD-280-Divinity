using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// Manage movement of all entities in the scene (currently just the player).
/// </summary>
public class MovementManager : MonoBehaviour
{

    public static MovementManager Instance;

    public Player CurrentPlayer;
    public Camera CurrentCamera;

    public float RotationSpeed = 10.0f;
    public float MovementSpeed = 10.0f;

    public GameObject NavigationMesh;

    /// <summary>
    /// The offset from which to cast rays to detect the Y coordinate during movement. Usually this should be approximately the
    /// player's height. Too high and it would break overhangs, and too low and it may not hit the nav mesh on inclines.
    /// </summary>
    public float MovementRaycastOffset = 3.0f;

    private bool isRotating;
    private bool isMoving;
    private Vector2 playerTarget;
    private Quaternion targetRotation;

    // Pieces of NavMesh broken out for easy access
    private Mesh navMesh;
    private MeshCollider navCollider;
    private Transform navTransform;
    private Pathfinder navPathfinder;

    private Queue<Vector2> waypoints;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        } else {
            Instance = this;
        }
    }

    void Start()
    {

        navCollider = NavigationMesh.GetComponent<MeshCollider>();
        navTransform = NavigationMesh.GetComponent<Transform>();
        navMesh = navCollider.sharedMesh;

        // Set up the navmesh for future pathfinding
        navPathfinder = new Pathfinder(navMesh, navTransform);
        StartCoroutine(navPathfinder.BuildGraph());

        // DebugShapes.DrawRegularPoly(CurrentPlayer.transform.position + Vector3.up, 6, 1.0f, Color.green, Mathf.Infinity);

    }

    // Update is called once per frame
    void Update()
    {

        // Debug info
        // navPathfinder.DrawDebugCells(Color.green);
        // navPathfinder.DrawDebugCenters(Color.red);
        // navPathfinder.DrawDebugGraph(Color.blue);

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

            Vector3 playerPos3D = playerTransform.position;
            Vector2 playerPos2D = new(playerPos3D.x, playerPos3D.z);
            playerPos2D = Vector2.MoveTowards(playerPos2D, playerTarget, Time.deltaTime * MovementSpeed);

            CurrentPlayer.Animator.SetFloat("Speed", MovementSpeed);

            // Ending actions when the player arrives at the target.
            if (playerPos2D == playerTarget)
            {

                // Actions to take on final target point
                if (waypoints.Count == 0)
                {
                    isMoving = false;
                    CurrentPlayer.Animator.SetFloat("Speed", 0.0f);
                }
                else
                {
                    setPlayerTarget(waypoints.Dequeue());
                }
                
            }

            // To keep the player snapped to the movement mesh while in motion, we cast a ray downward and use that Y value.
            Vector3 yRayOrigin = playerPos3D + new Vector3(0, MovementRaycastOffset, 0);
            Ray yRay = new(yRayOrigin, Vector3.down);
            float yPos;
            if (navCollider.Raycast(yRay, out RaycastHit rayHit, Mathf.Infinity))
            {
                yPos = rayHit.point.y;
            }
            else
            {
                yPos = playerPos3D.y;
            }

            playerTransform.position = new Vector3(playerPos2D.x, yPos, playerPos2D.y);

        }
        
    }

    private void setPlayerTarget(Vector2 targetPoint)
    {

        // Set location
        playerTarget = targetPoint;

        Vector3 playerPos = CurrentPlayer.gameObject.transform.position;
        Vector2 playerPos2D = new(playerPos.x, playerPos.z);

        // Compute target angle
        Vector3 lookDirection = new Vector3(playerTarget.x, 0, playerTarget.y) - new Vector3(playerPos2D.x, 0, playerPos2D.y);
        if (lookDirection != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(lookDirection);
        }

        // Enable movement updates
        isRotating = true;
        isMoving = true;

    }

    public void SetPlayerPath(Queue<Vector2> points)
    {

        waypoints = points;

        if (waypoints.Count != 0)
        {
            setPlayerTarget(waypoints.Dequeue());
        }

    }

    public MeshCollider GetNavCollider()
    {
        return navCollider;
    }

    public Queue<Vector2> ComputePlayerPath(Vector3 target)
    {
        Queue<Vector2> points = navPathfinder.ComputePath(CurrentPlayer.gameObject.transform.position, target);
        return points;
    }

}
