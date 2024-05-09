using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// The healing spell sets the player's HP and armor to maximum.
/// </summary>
public class Heal : Spell
{

    public GameObject TargetPrefab;
    public float IconOffset = 0.1f;
    public GameObject HealEffectPrefab;
    public float HealEffectOffset;

    private GameObject target;

    public override void OnCast()
    {
        
        // Heal all damage
        GameObject caster = Caster.gameObject;
        if (caster.TryGetComponent<Player>(out Player player))
        {
            player.HealAllDamage();
            GameObject effect = GameObject.Instantiate(HealEffectPrefab);
            effect.transform.position = player.transform.position + Vector3.up * HealEffectOffset;
        }

    }

    public override void OnCleanup()
    {
        GameObject.Destroy(target);
        target = null;
    }

    public override void OnIdleTick()
    {
        
    }

    public override void OnSelected()
    {
        target = GameObject.Instantiate(TargetPrefab);
        target.transform.position = Caster.transform.position;
    }

}
