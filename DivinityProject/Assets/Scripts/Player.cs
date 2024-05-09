using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// Holds player attributes - speed, HP, armor, etc.
/// </summary>
public class Player : MonoBehaviour
{

    public Animator Animator;

    public float Speed = 10.0f;

    public float MaxHP = 100.0f;
    public float MaxMagicArmor = 100.0f;
    public float MaxPhysicalArmor = 100.0f;
    public float DamageTickTime = 2.0f;

    private float curHP;
    private float curMagicArmor;
    private float curPhysicalArmor;

    // Start is called before the first frame update
    void Start()
    {
        curHP = MaxHP;
        curMagicArmor = MaxMagicArmor;
        curPhysicalArmor = MaxPhysicalArmor;
        StartCoroutine(checkDamage());
    }

    public void HealAllDamage()
    {
        curHP = MaxHP;
        curMagicArmor = MaxMagicArmor;
        curPhysicalArmor = MaxPhysicalArmor;
        UpdateUIBars();
    }

    private IEnumerator checkDamage()
    {

        while (true)
        {

            // Check if player standing on square with effect
            Vector3 playerPos = this.transform.position;
            Vector2Int playerCell = GroundEffectManager.Instance.WorldToAxial(playerPos);
            GroundEffect currentEffect = GroundEffectManager.Instance.CheckCellEffect(playerCell);

            if (currentEffect != null)
            {

                // First drain from physical or magic armor
                if (currentEffect.IsPhysicalDamage && curPhysicalArmor > 0)
                {
                    curPhysicalArmor = Mathf.Clamp(curPhysicalArmor - currentEffect.DamageAmount, 0, MaxPhysicalArmor);
                }
                else if (currentEffect.IsMagicalDamage && curMagicArmor > 0)
                {
                    curMagicArmor = Mathf.Clamp(curMagicArmor - currentEffect.DamageAmount, 0, MaxMagicArmor);
                }
                else
                {
                    curHP = Mathf.Clamp(curHP - currentEffect.DamageAmount, 0, MaxHP);
                }

                // Update all bars
                UpdateUIBars();

            }

            yield return new WaitForSeconds(DamageTickTime);

        }

    }

    private void UpdateUIBars()
    {
        UI.Instance.SetHPPercent(curHP / MaxHP);
        UI.Instance.SetPhysicalArmorPercent(curPhysicalArmor / MaxPhysicalArmor);
        UI.Instance.SetMagicalArmorPercent(curMagicArmor / MaxMagicArmor);
    }
}
