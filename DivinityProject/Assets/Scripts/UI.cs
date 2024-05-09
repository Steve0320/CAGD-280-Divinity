using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// UI management. This populates ability slots, displays help text, shows HP/Armor bars, etc.
/// </summary>
public class UI : MonoBehaviour
{

    public static UI Instance;

    public List<GameObject> AbilitySlots;
    public GameObject HighlightSlot;
    public Spellcaster CurrentCaster;
    public Texture DefaultIcon;

    public TextMeshProUGUI SpellText;
    public AnimationCurve SpellTextAlpha;
    public float SpellTextTime;
    public String SpellTextFormat = "Casting {0}!";

    public GameObject InstructionComponent;
    public GameObject HPComponent;
    public GameObject PhysicalArmorComponent;
    public GameObject MagicArmorComponent;

    private bool shouldUpdateSlots;
    private bool animateSpellText;
    private float animateSpellTextTime;

    private Vector3 startingPhysicalArmorPos;
    private Vector3 startingMagicalArmorPos;
    private float physicalArmorWidth = 275;
    private float magicalArmorWidth = 275;
    private float hpStartingScale;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        } else {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

        hpStartingScale = HPComponent.transform.localScale.x;

        startingPhysicalArmorPos = PhysicalArmorComponent.transform.position;
        startingMagicalArmorPos = MagicArmorComponent.transform.position;

        foreach (GameObject slot in AbilitySlots)
        {
            RawImage slotImage = slot.AddComponent<RawImage>();
            slotImage.texture = DefaultIcon;
        }

        RedrawSpellIcons();
        
    }

    // Update is called once per frame
    void Update()
    {

        if (shouldUpdateSlots)
        {

            Spell active = CurrentCaster.GetActiveSpell();

            // Draw all icons in spellbook
            for (int i = 0; i < CurrentCaster.CastableSpells.Count; i++)
            {

                // Ensure we don't try to write to more slots than actually exist
                if (i >= AbilitySlots.Count) break;

                Spell spell = CurrentCaster.CastableSpells[i];
                GameObject slot = AbilitySlots[i];

                slot.GetComponent<RawImage>().texture = (spell.Icon != null) ? spell.Icon : DefaultIcon;

                if (spell == active)
                {
                    HighlightSlot.transform.position = slot.transform.position;
                }

            }

            // Update casting text
            SpellText.text = String.Format(SpellTextFormat, active.SpellName);
            SpellText.gameObject.SetActive(active.DisplayTextDuringCast);
            animateSpellText = active.DisplayTextDuringCast;
            animateSpellTextTime = 0;

            shouldUpdateSlots = false;

        }

        // Fade in/out the casting text
        if (animateSpellText)
        {

            float animationPosition = animateSpellTextTime / SpellTextTime;

            Color curColor = SpellText.color;
            curColor.a = SpellTextAlpha.Evaluate(animationPosition);

            SpellText.color = curColor;
            animateSpellTextTime += Time.deltaTime;
            // print(curColor);

        }
        
    }

    public void RedrawSpellIcons()
    {
        shouldUpdateSlots = true;
    }

    public void OnToggleInstructions(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        InstructionComponent.SetActive(!InstructionComponent.activeSelf);
    }

    // Scale x
    public void SetHPPercent(float perc)
    {
        float clampedPerc = Mathf.Clamp(perc, 0, 1);
        Vector3 scale = HPComponent.transform.localScale;
        scale.x = clampedPerc * hpStartingScale;

        HPComponent.transform.localScale = scale;

    }

    // Scoot left
    public void SetPhysicalArmorPercent(float perc)
    {
        float clampedPerc = 1 - Mathf.Clamp(perc, 0, 1);
        Vector3 pos = startingPhysicalArmorPos;
        pos.x -= physicalArmorWidth * clampedPerc;
        PhysicalArmorComponent.transform.position = pos;
    }

    // Scoot right
    public void SetMagicalArmorPercent(float perc)
    {
        float clampedPerc = 1 - Mathf.Clamp(perc, 0, 1);
        Vector3 pos = startingMagicalArmorPos;
        pos.x += magicalArmorWidth * clampedPerc;
        MagicArmorComponent.transform.position = pos;
    }

}
