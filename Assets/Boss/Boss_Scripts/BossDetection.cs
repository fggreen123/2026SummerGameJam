using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossDetection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("감지 후 활성화할 BossController입니다.")]
    [SerializeField]
    private BossController bossController;

    [Tooltip("비어 있으면 씬에서 Player 컴포넌트를 자동으로 찾습니다.")]
    [SerializeField]
    private Player player;

    [Header("Detection")]
    [Tooltip("보스가 플레이어를 인식하는 거리입니다.")]
    [SerializeField, Min(0f)]
    private float detectionRange = 8f;

    [Tooltip("플레이어가 범위 밖으로 나가면 보스 공격을 중단할지 설정합니다.")]
    [SerializeField]
    private bool stopWhenPlayerLeavesRange = false;

    [Tooltip("한 번 인식하면 계속 전투 상태를 유지할지 설정합니다.")]
    [SerializeField]
    private bool keepDetectedAfterFirstDetection = true;

    [Tooltip("감지 상태를 확인하는 간격입니다.")]
    [SerializeField, Min(0.02f)]
    private float detectionCheckInterval = 0.1f;

    [Header("Debug")]
    [Tooltip("현재 플레이어를 감지한 상태입니다.")]
    [SerializeField]
    private bool isPlayerDetected;

    private float nextCheckTime;
    private bool hasDetectedPlayerOnce;

    public bool IsPlayerDetected =>
        isPlayerDetected;

    public float DetectionRange =>
        detectionRange;

    private void Reset()
    {
        bossController =
            GetComponent<BossController>();

        player =
            FindFirstObjectByType<Player>();
    }

    private void Awake()
    {
        if (bossController == null)
        {
            bossController =
                GetComponent<BossController>();
        }

        FindPlayer();

        if (bossController != null)
        {
            bossController.enabled =
                false;
        }
    }

    private void Start()
    {
        FindPlayer();
        CheckDetectionImmediately();
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime =
            Time.time +
            detectionCheckInterval;

        if (player == null)
        {
            FindPlayer();

            if (player == null)
            {
                SetDetected(false);
                return;
            }
        }

        CheckDetection();
    }

    private void FindPlayer()
    {
        if (player != null &&
            player.gameObject.activeInHierarchy)
        {
            return;
        }

        player =
            FindFirstObjectByType<Player>();

        if (player != null)
        {
            return;
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
            return;
        }

        if (taggedPlayer == null)
        {
            return;
        }

        player =
            taggedPlayer.GetComponent<Player>();

        if (player == null)
        {
            player =
                taggedPlayer.GetComponentInParent<Player>();
        }

        if (player == null)
        {
            player =
                taggedPlayer.GetComponentInChildren<Player>();
        }
    }

    private void CheckDetectionImmediately()
    {
        nextCheckTime = 0f;
        CheckDetection();
    }

    private void CheckDetection()
    {
        if (player == null)
        {
            SetDetected(false);
            return;
        }

        if (keepDetectedAfterFirstDetection &&
            hasDetectedPlayerOnce)
        {
            SetDetected(true);
            return;
        }

        Vector2 bossPosition =
            transform.position;

        Vector2 playerPosition =
            player.transform.position;

        float squaredDistance =
            (playerPosition - bossPosition)
            .sqrMagnitude;

        float squaredDetectionRange =
            detectionRange *
            detectionRange;

        bool playerIsInsideRange =
            squaredDistance <=
            squaredDetectionRange;

        if (playerIsInsideRange)
        {
            hasDetectedPlayerOnce = true;
            SetDetected(true);
            return;
        }

        if (stopWhenPlayerLeavesRange)
        {
            SetDetected(false);
        }
    }

    private void SetDetected(
        bool detected
    )
    {
        if (isPlayerDetected == detected)
        {
            return;
        }

        isPlayerDetected =
            detected;

        if (bossController == null)
        {
            return;
        }

        if (detected)
        {
            bossController.enabled =
                true;

            Debug.Log(
                $"{name}: 플레이어를 감지하여 보스 전투를 시작합니다.",
                this
            );

            return;
        }

        if (stopWhenPlayerLeavesRange)
        {
            bossController.enabled =
                false;

            Debug.Log(
                $"{name}: 플레이어가 감지 범위를 벗어났습니다.",
                this
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        detectionRange =
            Mathf.Max(
                0f,
                detectionRange
            );

        detectionCheckInterval =
            Mathf.Max(
                0.02f,
                detectionCheckInterval
            );
    }
#endif
}