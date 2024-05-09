using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// Kill the associated GameObject after DespawnTime seconds have elapsed. Used when spawning
/// short-lived effects.
/// </summary>
public class EffectTimer : MonoBehaviour
{

    public float DespawnTime;

    private float curTime;

    // Start is called before the first frame update
    void Start()
    {
        curTime = 0;
    }

    // Update is called once per frame
    void Update()
    {

        curTime += Time.deltaTime;
        if (curTime > DespawnTime)
        {
            GameObject.Destroy(this.gameObject);
        }
        
    }
}
