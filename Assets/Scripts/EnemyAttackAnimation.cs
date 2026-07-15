using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAttackAnimation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("공격 애니메이션을 재생할 Animator입니다.")]
    [SerializeField]
    private Animator animator;

    [Header("Animation Parameter")]
    [Tooltip("Animator에 생성한 공격 Trigger 파라미터 이름입니다.")]
    [SerializeField]
    private string attackTriggerName = "Attack";

    private int attackTriggerHash;
    private bool isInitialized;

    private void Reset()
    {
        animator =
            GetComponentInChildren<Animator>(
                true
            );
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>(
                    true
                );
        }

        if (animator == null)
        {
            Debug.LogError(
                $"{name}: 공격 애니메이션을 재생할 Animator가 없습니다.",
                this
            );

            enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(
            attackTriggerName))
        {
            Debug.LogError(
                $"{name}: 공격 Trigger 이름이 비어 있습니다.",
                this
            );

            enabled = false;
            return;
        }

        attackTriggerHash =
            Animator.StringToHash(
                attackTriggerName
            );

        isInitialized =
            true;
    }

    public void PlayAttackAnimation()
    {
        if (!enabled ||
            !isInitialized ||
            animator == null)
        {
            return;
        }

        animator.ResetTrigger(
            attackTriggerHash
        );

        animator.SetTrigger(
            attackTriggerHash
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>(
                    true
                );
        }
    }
#endif
}