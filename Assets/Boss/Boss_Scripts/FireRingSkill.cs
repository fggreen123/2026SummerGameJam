using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FireRingSkill : BossSkill
{
    [Header("References")]
    [Tooltip("BossController입니다.")]
    [SerializeField]
    private BossController bossController;

    [Tooltip("플레이어 주변에 표시할 원형 불 프리팹입니다.")]
    [SerializeField]
    private GameObject fireRingPrefab;

    [Header("Attack Settings")]
    [Tooltip("플레이어에게 주는 피해량입니다.")]
    [SerializeField, Min(0)]
    private int damage = 7;

    [Tooltip("플레이어를 구속하는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float bindDuration = 3f;

    [Tooltip("불 이미지가 플레이어를 계속 따라갈지 설정합니다.")]
    [SerializeField]
    private bool followPlayer = true;

    [Tooltip("불 이미지의 위치 보정값입니다.")]
    [SerializeField]
    private Vector3 effectOffset;

    private void Reset()
    {
        bossController = GetComponentInParent<BossController>();
    }

    private void Awake()
    {
        if (bossController == null)
            bossController = GetComponentInParent<BossController>();
    }

    protected override IEnumerator ExecuteSkill()
    {
        if (bossController == null)
        {
            Debug.LogWarning(
                $"{name}: BossController가 연결되지 않았습니다.",
                this
            );

            yield break;
        }

        Transform player = bossController.Player;

        if (player == null)
        {
            Debug.LogWarning(
                $"{name}: BossController의 Player가 연결되지 않았습니다.",
                this
            );

            yield break;
        }

        GameObject effectObject = CreateEffect(player);

        ApplyDamage(player);

        PlayerMovement playerMovement =
            player.GetComponentInParent<PlayerMovement>();

        Rigidbody2D playerRigidbody =
            player.GetComponentInParent<Rigidbody2D>();

        bool movementWasEnabled =
            playerMovement != null &&
            playerMovement.enabled;

        Vector2 previousVelocity = Vector2.zero;
        float previousAngularVelocity = 0f;

        RigidbodyConstraints2D previousConstraints =
            RigidbodyConstraints2D.None;

        if (playerRigidbody != null)
        {
            previousVelocity =
                playerRigidbody.linearVelocity;

            previousAngularVelocity =
                playerRigidbody.angularVelocity;

            previousConstraints =
                playerRigidbody.constraints;

            playerRigidbody.linearVelocity =
                Vector2.zero;

            playerRigidbody.angularVelocity = 0f;

            playerRigidbody.constraints =
                previousConstraints |
                RigidbodyConstraints2D.FreezePosition;
        }

        if (playerMovement != null)
            playerMovement.enabled = false;

        float elapsedTime = 0f;

        while (elapsedTime < bindDuration)
        {
            if (player == null)
                break;

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity =
                    Vector2.zero;

                playerRigidbody.angularVelocity = 0f;
            }

            if (followPlayer && effectObject != null)
            {
                effectObject.transform.position =
                    player.position + effectOffset;
            }

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        if (playerMovement != null)
            playerMovement.enabled = movementWasEnabled;

        if (playerRigidbody != null)
        {
            playerRigidbody.constraints =
                previousConstraints;

            /*
             * 구속이 끝난 직후 이전 속도로 갑자기 움직이지 않도록
             * 이동 속도는 0으로 유지합니다.
             */
            playerRigidbody.linearVelocity =
                Vector2.zero;

            playerRigidbody.angularVelocity =
                previousAngularVelocity;
        }

        if (effectObject != null)
            Destroy(effectObject);
    }

    private GameObject CreateEffect(Transform player)
    {
        if (fireRingPrefab == null)
        {
            Debug.LogWarning(
                $"{name}: Fire Ring Prefab이 연결되지 않았습니다.",
                this
            );

            return null;
        }

        Vector3 spawnPosition =
            player.position + effectOffset;

        GameObject effectObject = Instantiate(
            fireRingPrefab,
            spawnPosition,
            Quaternion.identity
        );

        return effectObject;
    }
    private void ApplyDamage(Transform target)
    {
        Player player =
            target.GetComponentInParent<Player>();

        if (player == null)
            return;

        IDamageable damageable =
            player.GetComponent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);
    }
}