using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// A spellcaster is an entity capable of casting spells.
/// </summary>
public class Spellcaster : MonoBehaviour
{

    public MeshCollider CastingLayer;

    /// <summary>
    /// The list of spells that a caster is capable of casting.
    /// </summary>
    public List<Spell> CastableSpells;

    // The spell to be cast when clicked
    private int activeSpellIdx;

    // Which spell is currently ticking - used to determine when changes occur
    private int tickingIdx;

    // Flag to run actual spellcast
    private bool spellWasCast;

    private GameObject currentSpellObject;
    private Spell currentSpell;

    void Start()
    {
        activeSpellIdx = 0;
        tickingIdx = 0;
        spellWasCast = false;
        InstantiateActiveSpell();
        currentSpell.OnSelected();
    }

    void Update()
    {

        // When spell changes, cleanup old and start new
        if (tickingIdx != activeSpellIdx)
        {

            // Cleanup old spell
            currentSpell.OnCleanup();
            GameObject.Destroy(currentSpellObject);

            // Instantiate new spell
            InstantiateActiveSpell();
            currentSpell.OnSelected();

            tickingIdx = activeSpellIdx;

        }

        if (spellWasCast)
        {
            currentSpell.OnCast();
            spellWasCast = false;
        }

        currentSpell.OnIdleTick();

    }

    public void CycleActiveSpell(InputAction.CallbackContext context)
    {

        if (!context.performed) return;

        activeSpellIdx = (activeSpellIdx + 1) % CastableSpells.Count();
        UI.Instance.RedrawSpellIcons();
        
    }

    /// <summary>
    /// x
    /// </summary>
    public void OnSpellCast(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        spellWasCast = true;
    }

    public Spell GetActiveSpell()
    {
        return CastableSpells[activeSpellIdx];
    }

    private Spell GetTickingSpell()
    {
        return CastableSpells[tickingIdx];
    }

    private void InstantiateActiveSpell()
    {
        currentSpellObject = GameObject.Instantiate(GetActiveSpell().gameObject);
        currentSpell = currentSpellObject.GetComponent<Spell>();
        currentSpell.Caster = this;
    }

}
