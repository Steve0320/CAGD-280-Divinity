using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// A spell is an effect cast by a Spellcaster. A spell's lifecycle is managed by the Spellcaster component.
/// We consider a spell anything that can appear in the player's hotbar.
/// </summary>
public abstract class Spell : MonoBehaviour
{

    [HideInInspector]
    public Spellcaster Caster;

    public String SpellName;
    public Texture Icon;
    public bool DisplayTextDuringCast = true;

    public abstract void OnSelected();
    public abstract void OnIdleTick();
    public abstract void OnCast();
    public abstract void OnCleanup();

}
