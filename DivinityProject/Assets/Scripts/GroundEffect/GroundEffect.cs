using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// A ground effect is something which lingers on the ground for a certain period of time and interacts with other
/// ground effects via the GroundEffectManager.
/// </summary>
public class GroundEffect : MonoBehaviour
{
    
    public float Duration;
    public Color DebugColor;
    public float InteractionPriority;
    public float DamageAmount;
    public bool IsPhysicalDamage;
    public bool IsMagicalDamage;

    public virtual GroundEffect InteractWith(GroundEffectManager.EffectData ourData, GroundEffectManager.EffectData theirData)
    {
        return this;
    }

}
