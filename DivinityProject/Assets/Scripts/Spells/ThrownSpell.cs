using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// A thrown spell is a spell that spawns a projectile and spawns a ground effect on impact.
/// </summary>
public class ThrownSpell : Spell
{

    public float ForwardOffset = 1.0f;
    public float VerticalOffset = 1.0f;
    public float TravelTime = 2.0f;
    public float ImpactRadius = 5.0f;

    public GroundEffect ImpactEffect;
    public GameObject ProjectilePrefab;
    public GameObject TargetPrefab;
    public float TargetProjectionOffset = 0.9f;

    public AnimationCurve HeightCurve;

    private GameObject target;

    public override void OnSelected()
    {

        target = GameObject.Instantiate(TargetPrefab);
        target.SetActive(false);

    }

    public override void OnIdleTick()
    {

        Vector2 mousePos = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Caster.CastingLayer.Raycast(ray, out RaycastHit rayHit, Mathf.Infinity))
        {
            Vector3 targetPos = rayHit.point;
            targetPos.y += TargetProjectionOffset;
            target.transform.position = targetPos;
            target.SetActive(true);
        }
        else
        {
            target.SetActive(false);
        }

    }

    public override void OnCast()
    {

        Vector2 mousePos = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Caster.CastingLayer.Raycast(ray, out RaycastHit rayHit, Mathf.Infinity))
        {

            // Spawn projectile at the appropriate offset
            GameObject spawnedSpell = Instantiate(ProjectilePrefab);
            Transform casterPos = Caster.transform;
            Vector3 spawnPos = casterPos.position + (casterPos.forward * ForwardOffset + Vector3.up * VerticalOffset);
            spawnedSpell.transform.position = spawnPos;

            // Set necessary info for projectile motion. This mostly just involves copying our parameters
            // to the motion component.
            MotionAlongCurve motion = spawnedSpell.AddComponent<MotionAlongCurve>();
            motion.Destination = rayHit.point;
            motion.TravelTime = TravelTime;
            motion.HeightCurve = HeightCurve;
            motion.DestroyAtDestination = true;
            motion.ImpactDelegate = OnProjectileImpact;

        }

    }

    /// <summary>
    /// Behavior to perform when the MotionAlongCurve component completes its trajectory. Note that
    /// since the GameObject attached to this spell component may have been destroyed while the projectile
    /// was in-flight, we shouldn't reference any of the GameObject's data in this function.
    /// </summary>
    /// <param name="self"></param>
    public void OnProjectileImpact(MotionAlongCurve self)
    {
        List<Vector2Int> cells = GroundEffectManager.Instance.GetCellsInCircle(self.Destination, this.ImpactRadius, this.Caster.CastingLayer);
        GroundEffectManager.Instance.ApplyEffect(cells, this.ImpactEffect);
    }

    public override void OnCleanup()
    {
        GameObject.Destroy(target);
        target = null;
    }

}
