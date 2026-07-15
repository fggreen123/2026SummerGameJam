using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
[DisallowMultipleComponent]
public sealed class BossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Enemy enemy;

    [SerializeField]
    private Transform player;

    [SerializeField]
    private BossSkillData skillData;

    [Header("Skills")]
    [SerializeField]
    private BossSkill ballThrowSkill;

    [SerializeField]
    private BossSkill hoopSkill;

    [SerializeField]
    private BossSkill fireRingSkill;

    [SerializeField]
    private BossSkill jumpSkill;

    [Header("Pattern")]
    [SerializeField, Min(1)]
    private int firstBallThrowCount = 4;

    [SerializeField, Min(1)]
    private int hoopCount = 1;

    [SerializeField, Min(1)]
    private int secondBallThrowCount = 4;

    [SerializeField, Min(1)]
    private int fireRingCount = 1;

    [SerializeField, Min(1)]
    private int jumpCount = 1;

    [SerializeField]
    private bool startPatternOnEnable = true;

    private Coroutine patternCoroutine;
    private bool patternEnabled;

    public Transform Player => player;
    public BossSkillData SkillData => skillData;

    private void Reset()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        if (startPatternOnEnable)
            StartPattern();
    }

    private void OnDisable()
    {
        StopPattern();
    }

    public void StartPattern()
    {
        if (patternCoroutine != null)
            return;

        patternEnabled = true;
        patternCoroutine = StartCoroutine(PatternRoutine());
    }

    public void StopPattern()
    {
        patternEnabled = false;

        if (patternCoroutine == null)
            return;

        StopCoroutine(patternCoroutine);
        patternCoroutine = null;
    }

    private IEnumerator PatternRoutine()
    {
        if (skillData != null && skillData.patternStartDelay > 0f)
            yield return new WaitForSeconds(skillData.patternStartDelay);

        while (patternEnabled)
        {
            yield return ExecuteRepeatedly(
                ballThrowSkill,
                firstBallThrowCount
            );

            yield return ExecuteRepeatedly(
                hoopSkill,
                hoopCount
            );

            yield return ExecuteRepeatedly(
                ballThrowSkill,
                secondBallThrowCount
            );

            yield return ExecuteRepeatedly(
                fireRingSkill,
                fireRingCount
            );

            yield return ExecuteRepeatedly(
                jumpSkill,
                jumpCount
            );

            if (skillData != null && skillData.patternEndDelay > 0f)
                yield return new WaitForSeconds(skillData.patternEndDelay);
        }

        patternCoroutine = null;
    }

    private IEnumerator ExecuteRepeatedly(
        BossSkill skill,
        int count
    )
    {
        if (skill == null)
            yield break;

        int safeCount = Mathf.Max(1, count);

        for (int i = 0; i < safeCount; i++)
        {
            if (!patternEnabled)
                yield break;

            yield return skill.Execute();
        }
    }
}