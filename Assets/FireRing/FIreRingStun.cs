using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

//플레이어 구속

public class FIreRingStun : MonoBehaviour
{
    [UnitHeaderInspectable("Stun Time")]
    public float stunDuration = 3.0f;
   
    private void OntTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartCoroutine(StunPlayerRoutine(collision.gameObject));
        }
    }

    private IEnumerator StunPlayerRoutine(GameObject player)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            playerRb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        yield return new WaitForSeconds(stunDuration);

        if (playerRb != null)
        {
            playerRb.constraints = RigidbodyConstraints2D.None;
            playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
}
