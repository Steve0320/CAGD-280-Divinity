using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// Fire spawns an explosion at the interaction boundary when contacting oil.
/// </summary>
public class FireGroundEffect : GroundEffect
{

    public GameObject ExplosionPrefab;
    
    public override GroundEffect InteractWith(GroundEffectManager.EffectData ourData, GroundEffectManager.EffectData theirData)
    {

        // Fire interacts with oil to produce an explosion and more fire
        if (theirData.Effect is OilGroundEffect)
        {

            GameObject explosion = Instantiate(ExplosionPrefab);
            explosion.transform.position = theirData.WorldPos;
            return this;

        }

        return this;
        
    }

}
