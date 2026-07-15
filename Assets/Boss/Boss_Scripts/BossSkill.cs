using System.Collections;
using UnityEngine;

public abstract class BossSkill : MonoBehaviour
{
    public bool IsRunning { get; private set; }

    public IEnumerator Execute()
    {
        if (IsRunning)
            yield break;

        IsRunning = true;

        yield return ExecuteSkill();

        IsRunning = false;
    }

    protected abstract IEnumerator ExecuteSkill();
}