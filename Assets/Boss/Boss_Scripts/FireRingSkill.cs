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
        bossController =
            GetComponentInParent<BossController>();
    }

    private void Awake()
    {
        if (bossController == null)
        {
            bossController =
                GetComponentInParent<BossController>();
        }
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

        Player playerComponent =
            FindActualPlayer();

        if (playerComponent == null)
        {
            Debug.LogWarning(
                $"{name}: 활성화된 Player를 찾지 못했습니다.",
                this
            );

            yield break;
        }

        Transform playerTransform =
            playerComponent.transform;

        GameObject effectObject =
            CreateEffect(playerTransform);

        ApplyDamage(playerComponent);

        PlayerMovement playerMovement =
            playerComponent.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            playerMovement =
                playerComponent.GetComponentInParent<PlayerMovement>();
        }

        Rigidbody2D playerRigidbody =
            playerComponent.GetComponent<Rigidbody2D>();

        if (playerRigidbody == null)
        {
            playerRigidbody =
                playerComponent.GetComponentInParent<Rigidbody2D>();
        }

        bool movementWasEnabled =
            playerMovement != null &&
            playerMovement.enabled;

        float previousAngularVelocity =
            0f;

        RigidbodyConstraints2D previousConstraints =
            RigidbodyConstraints2D.None;

        if (playerRigidbody != null)
        {
            previousAngularVelocity =
                playerRigidbody.angularVelocity;

            previousConstraints =
                playerRigidbody.constraints;

            playerRigidbody.linearVelocity =
                Vector2.zero;

            playerRigidbody.angularVelocity =
                0f;

            playerRigidbody.constraints =
                previousConstraints |
                RigidbodyConstraints2D.FreezePosition;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled =
                false;
        }

        float elapsedTime = 0f;

        while (elapsedTime < bindDuration)
        {
            if (playerComponent == null)
            {
                break;
            }

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity =
                    Vector2.zero;

                playerRigidbody.angularVelocity =
                    0f;
            }

            if (followPlayer &&
                effectObject != null)
            {
                effectObject.transform.position =
                    playerTransform.position +
                    effectOffset;
            }

            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled =
                movementWasEnabled;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.constraints =
                previousConstraints;

            playerRigidbody.linearVelocity =
                Vector2.zero;

            playerRigidbody.angularVelocity =
                previousAngularVelocity;
        }

        if (effectObject != null)
        {
            Destroy(effectObject);
        }
    }

    private Player FindActualPlayer()
    {
        Player foundPlayer =
            FindFirstObjectByType<Player>();

        if (foundPlayer != null &&
            foundPlayer.gameObject.activeInHierarchy)
        {
            return foundPlayer;
        }

        GameObject taggedPlayer = null;

        try
        {
            taggedPlayer =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );
        }
        catch (UnityException)
        {
            Debug.LogWarning(
                $"{name}: Player 태그가 등록되어 있지 않습니다.",
                this
            );
        }

        if (taggedPlayer == null)
        {
            return null;
        }

        foundPlayer =
            taggedPlayer.GetComponent<Player>();

        if (foundPlayer == null)
        {
            foundPlayer =
                taggedPlayer.GetComponentInParent<Player>();
        }

        if (foundPlayer == null)
        {
            foundPlayer =
                taggedPlayer.GetComponentInChildren<Player>();
        }

        return foundPlayer;
    }

    private GameObject CreateEffect(
        Transform playerTransform
    )
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
            playerTransform.position +
            effectOffset;

        GameObject effectObject =
            Instantiate(
                fireRingPrefab,
                spawnPosition,
                Quaternion.identity
            );

        return effectObject;
    }

    private void ApplyDamage(
        Player player
    )
    {
        if (player == null)
        {
            return;
        }

        IDamageable damageable =
            player.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable =
                player.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
        {
            return;
        }

        damageable.TakeDamage(
            damage
        );
    }
}