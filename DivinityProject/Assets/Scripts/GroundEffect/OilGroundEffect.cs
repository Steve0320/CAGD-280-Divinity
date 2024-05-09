using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// Oil explodes when it comes into contact with fire.
/// </summary>
public class OilGroundEffect : GroundEffect
{

    public GameObject ExplosionPrefab;
    
    public override GroundEffect InteractWith(GroundEffectManager.EffectData ourData, GroundEffectManager.EffectData theirData)
    {

        // Oil interacts with fire to produce an explosion and more fire
        if (theirData.Effect is FireGroundEffect)
        {

            GameObject explosion = Instantiate(ExplosionPrefab);
            explosion.transform.position = theirData.WorldPos;
            return theirData.Effect;

        }

        return this;

    }

}
