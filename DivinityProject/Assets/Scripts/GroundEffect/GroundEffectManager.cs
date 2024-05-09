using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// This class holds and monitors the ground effects cell grid. It is responsible for applying any
/// defined interactions.
/// </summary>
public class GroundEffectManager : MonoBehaviour
{

    public static GroundEffectManager Instance;
    public MeshCollider EffectsLayer;

    public float CellRadius = 1.0f;
    public float RaycastHeight = 500.0f;

    public float InteractionTickTime = 2.0f;

    // This is the backing list holding the list of cells to the pool that they belong to. This is indexed
    // using 2d axial coordinates, as described here:
    // https://www.redblobgames.com/grids/hexagons/#coordinates-axial
    private Dictionary<Vector2Int, EffectData> cellPools;
    private Dictionary<Vector2Int, GameObject> spawnedEffects;

    // Effects which will be added next tick
    private Dictionary<Vector2Int, EffectData> queuedEffects;

    public class EffectData
    {
        public GroundEffect Effect;
        public float RemainingTime;
        public Vector3 WorldPos;
        public bool EffectHasChanged;
    }

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

        cellPools = new();
        spawnedEffects = new();
        queuedEffects = new();

        StartCoroutine(CheckInteractions());

    }

    void Update()
    {

        List<Vector2Int> cellsToCleanUp = new();

        // Determine what cells are still live
        foreach (KeyValuePair<Vector2Int, EffectData> kv in cellPools)
        {

            Vector2Int cell = kv.Key;
            EffectData data = kv.Value;

            data.RemainingTime -= Time.deltaTime;

            // Flag dead cells for cleanup
            if (data.RemainingTime < 0)
            {
                cellsToCleanUp.Add(cell);
            }

            // Draw live cells
            else
            {

                // If not already spawned, spawn the appropriate effect
                if (!spawnedEffects.ContainsKey(cell))
                {
                    GameObject spawned = GameObject.Instantiate(data.Effect.gameObject);
                    spawned.transform.position = data.WorldPos;
                    spawnedEffects[cell] = spawned;
                }
                
                // If the ground effect is outdated, swap it out
                else if (data.EffectHasChanged)
                {
                    GameObject existingEffect = spawnedEffects[cell];
                    GameObject spawned = GameObject.Instantiate(data.Effect.gameObject);
                    spawned.transform.position = data.WorldPos;
                    spawnedEffects[cell] = spawned;
                    GameObject.Destroy(existingEffect);
                    data.EffectHasChanged = false;
                }


                DebugShapes.DrawRegularPoly(data.WorldPos, 6, CellRadius, data.Effect.DebugColor, 0);

            }

        }

        // Remove any dead cells
        foreach (Vector2Int deadCell in cellsToCleanUp)
        {
            cellPools.Remove(deadCell);
            if (spawnedEffects.ContainsKey(deadCell))
            {
                GameObject deadEffect = spawnedEffects[deadCell];
                GameObject.Destroy(deadEffect);
                spawnedEffects.Remove(deadCell);
            }
        }

    }

    // Coroutine - applies all interactions between neighboring cells.
    private IEnumerator CheckInteractions()
    {
        while(true)
        {

            // Test all neighbors
            // If interactable, update cell's data
            foreach (KeyValuePair<Vector2Int, EffectData> kv in cellPools)
            {

                Vector2Int cell = kv.Key;
                EffectData data = kv.Value;
                GroundEffect effect = data.Effect;

                foreach (Vector2Int neighborCell in GetNeighboringCells(cell))
                {

                    EffectData toInteract = cellPools[neighborCell];
                    if (toInteract.Effect.GetType() != effect.GetType())
                    {

                        GroundEffect resultingEffect;
                        if (effect.InteractionPriority > toInteract.Effect.InteractionPriority)
                        {
                            resultingEffect = effect.InteractWith(data, toInteract);
                        }
                        else
                        {
                            resultingEffect = toInteract.Effect.InteractWith(toInteract, data);
                        }

                        data.Effect = resultingEffect;
                        data.RemainingTime = resultingEffect.Duration;
                        data.EffectHasChanged = true;
                        
                    }

                }

            }

            // Iterate through queued effects and apply them to any existing effects (as if they were neighbors)
            foreach (KeyValuePair<Vector2Int, EffectData> kv in queuedEffects)
            {

                Vector2Int cell = kv.Key;
                EffectData data = kv.Value;
                GroundEffect effect = data.Effect;

                if (cellPools.ContainsKey(cell))
                {
                    EffectData toInteract = cellPools[cell];

                    // Ignore self interactions
                    if (toInteract.Effect.GetType() != effect.GetType())
                    {

                        GroundEffect resultingEffect;
                        if (effect.InteractionPriority > toInteract.Effect.InteractionPriority)
                        {
                            resultingEffect = effect.InteractWith(data, toInteract);
                        }
                        else
                        {
                            resultingEffect = toInteract.Effect.InteractWith(toInteract, data);
                        }

                        data.Effect = resultingEffect;
                        data.RemainingTime = resultingEffect.Duration;
                        data.EffectHasChanged = true;

                    }
                    
                }

                cellPools[cell] = data;

            }

            queuedEffects.Clear();

            yield return new WaitForSeconds(InteractionTickTime);

        }
    }

    public void ApplyEffect(List<Vector2Int> cells, GroundEffect effect)
    {

        // Initialize data w/ starting values.
        foreach (Vector2Int cell in cells)
        {

            Vector3 point = AxialToWorld(cell);
            point.y = RaycastHeight;
            Ray ray = new(point, Vector3.down);

            if (EffectsLayer.Raycast(ray, out RaycastHit rayHit, Mathf.Infinity))
            {
                queuedEffects[cell] = new() { Effect = effect, RemainingTime = effect.Duration, WorldPos = rayHit.point };
            }

        }

    }

    public GroundEffect CheckCellEffect(Vector2Int cell)
    {

        if (cellPools != null && cellPools.ContainsKey(cell))
        {
            return cellPools[cell].Effect;
        }

        return null;
    }

    /// <summary>
    /// Helper - get the cells (in axial coordinates) that are encompassed by the world-space circle defined
    /// by the center and radius parameters. Any cell which overlaps any part of the circle is considered inside
    /// the circle.
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="CastingLayer"></param>
    /// <returns></returns>
    public List<Vector2Int> GetCellsInCircle(Vector3 center, float radius, MeshCollider CastingLayer)
    {

        // Generate a set of candidate points within the circle to test. This set of test points is currently
        // very badly distributed and way too dense, but it'll work for now.
        float radiusStep = 0.1f;
        float angleStep = 1.0f;

        List<Vector3> testPoints = new();

        float curRadius = 0.0f;
        while (curRadius <= radius)
        {

            float curAngle = 0.0f;
            while (curAngle < 360.0f)
            {
                Vector3 point = center + Quaternion.AngleAxis(curAngle, Vector3.up) * Vector3.forward * curRadius;
                testPoints.Add(point);
                curAngle += angleStep;
            }

            curRadius += radiusStep;

        }

        // Test each point, blacklisting any that fall outside of the permitted casting area.
        HashSet<Vector2Int> blacklist = new();
        HashSet<Vector2Int> cells = new();

        foreach (Vector3 point in testPoints)
        {

            Vector2Int axial = WorldToAxial(point);

            if (blacklist.Contains(axial)) { continue; }

            // If the point is not above navigation mesh, discard the whole cell.
            Vector3 yRayOrigin = point + new Vector3(0, 1.0f, 0);
            Ray yRay = new(yRayOrigin, Vector3.down);
            if (!CastingLayer.Raycast(yRay, out RaycastHit _, Mathf.Infinity))
            {
                blacklist.Add(axial);
                cells.Remove(axial);
                continue;
            }

            cells.Add(axial);


        }

        return cells.ToList();

    }

    /// <summary>
    /// Helper to convert a world coordinate to the axial coordinates which contains it. Note that though this
    /// takes a Vector3, the Y coordinate is discarded as our effects grid is 2D.
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Vector2Int WorldToAxial(Vector3 point)
    {

        float q = (Mathf.Sqrt(3) / 3.0f * point.x - 1.0f / 3.0f * point.z) / CellRadius;
        float r = 2.0f / 3.0f * point.z / CellRadius;

        float qPrime = Mathf.Round(q);
        float rPrime = Mathf.Round(r);
        q -= qPrime;
        r -= rPrime;

        // TODO: If everything works these should always be ints
        if (Mathf.Abs(q) >= Mathf.Abs(r))
        {
            return new Vector2Int((int)(qPrime + Mathf.Round(q + 0.5f * r)), (int)rPrime);
        }
        else
        {
            return new Vector2Int((int)qPrime, (int)(rPrime + Mathf.Round(0.5f * q + r)));
        }

    }

    /// <summary>
    /// Helper to convert an axial coordinate to the corresponding world coordinate at its center. Note that
    /// the Y coordinate of the returned Vector3 is always zero.
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Vector3 AxialToWorld(Vector2Int point)
    {

        float x = CellRadius * (Mathf.Sqrt(3) * point.x + Mathf.Sqrt(3) / 2 * point.y);
        float y = CellRadius * (3.0f / 2.0f) * point.y;
        return new Vector3(x, 0, y);

    }

    private List<Vector2Int> GetNeighboringCells(Vector2Int point)
    {
        List<Vector2Int> neighborDirections = new()
        {
            new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)
        };

        List<Vector2Int> neighbors = new();
        foreach (Vector2Int dir in neighborDirections)
        {
            Vector2Int neighbor = point + dir;
            if (cellPools.ContainsKey(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;

    }

}
