using Unity.VisualScripting;
using UnityEngine;

public class TriiFireRing : MonoBehaviour
{
    [UnitHeaderInspectable("FireRingScript")]
    public FireRing FireRingScript;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)){
            CallFireRing();
        }
    }

    void CallFireRing()
    {
        if(FireRingScript != null)
        {
            FireRingScript.OnFireRing();
        }
        else
        {
            Debug.Log("Summon Failed");
        }
    }
}
