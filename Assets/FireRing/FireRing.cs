using Unity.VisualScripting;
using UnityEngine;

//파이어링
public class FireRing : MonoBehaviour
{
    [UnitHeaderInspectable("FireRing")]
    public GameObject fireRing;

    [Header("FireRingSummonTime")]
    public float FireRingDuration = 3.0f;

    [Header("SummonPoint")]
    public Transform SumPoint;

    [Header("footOffset")]
    public float footOffset = -0.4f;

   public void OnFireRing( )
    {
        if (fireRing == null) return;

        Vector3 basePositiom = SumPoint!=null?SumPoint.position : transform.position;
        Vector3 sumPosition = new Vector3(basePositiom.x, basePositiom.y + footOffset, basePositiom.z);

        GameObject sumedFireRing = Instantiate(fireRing, sumPosition, Quaternion.identity);
        Destroy(sumedFireRing, FireRingDuration);
    }
}
