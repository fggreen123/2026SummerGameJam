using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private const float Interval = 0.03f;

    private CinemachineImpulseSource impulse;

    private void Awake()
    {
        impulse = GetComponent<CinemachineImpulseSource>() ?? gameObject.AddComponent<CinemachineImpulseSource>();

        CinemachineCamera camera = FindFirstObjectByType<CinemachineCamera>();
        CinemachineImpulseListener listener = camera.GetComponent<CinemachineImpulseListener>()
            ?? camera.gameObject.AddComponent<CinemachineImpulseListener>();

        listener.ChannelMask = impulse.ImpulseDefinition.ImpulseChannel = 1;
        listener.Gain = 1f;

        impulse.ImpulseDefinition.ImpulseDuration = Interval;
        impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(Shake(duration, magnitude));
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Interval)
        {
            impulse.GenerateImpulseWithVelocity(Random.insideUnitCircle.normalized * magnitude);
            yield return new WaitForSeconds(Interval);
        }
    }
}
