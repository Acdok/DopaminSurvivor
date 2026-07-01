using UnityEngine;

/// <summary>
/// 게임플레이 스크립트에 의존하지 않고 직교 카메라를 타겟 중심으로 따라가게 한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    private enum FollowMode
    {
        Instant,
        Smooth
    }

    [Header("Target")]
    [SerializeField]
    private Transform target;

    [Header("Follow")]
    [SerializeField]
    private FollowMode followMode = FollowMode.Smooth;

    [SerializeField, Min(0f)]
    private float followSpeed = 8f;

    private float fixedZPosition;
    private bool warnedMissingTarget;
    private float shakeTimer;
    private float shakeDuration;
    private float shakeStrength;

    public Transform Target
    {
        get => target;
        set
        {
            target = value;
            warnedMissingTarget = false;
        }
    }

    private void Awake()
    {
        fixedZPosition = transform.position.z;
    }

    private void OnValidate()
    {
        followSpeed = Mathf.Max(0f, followSpeed);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            WarnMissingTargetOnce();
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, fixedZPosition);

        // 추적 모드에 따라 즉시 이동하거나 부드럽게 따라간다.
        Vector3 nextPosition = followMode == FollowMode.Instant
            ? targetPosition
            : Vector3.Lerp(currentPosition, targetPosition, followSpeed * Time.deltaTime);

        // 카메라 깊이를 유지하기 위해 X/Y만 따라가고 Z는 고정한다.
        nextPosition.z = fixedZPosition;
        nextPosition += GetShakeOffset();
        transform.position = nextPosition;
    }

    public void Shake(float duration, float strength)
    {
        if (duration <= 0f || strength <= 0f)
        {
            return;
        }

        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimer = Mathf.Max(shakeTimer, duration);
        shakeStrength = Mathf.Max(shakeStrength, strength);
    }

    private Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0f || shakeStrength <= 0f)
        {
            return Vector3.zero;
        }

        shakeTimer = Mathf.Max(0f, shakeTimer - Time.deltaTime);
        float normalizedTime = shakeDuration > 0f ? shakeTimer / shakeDuration : 0f;
        Vector2 offset = Random.insideUnitCircle * shakeStrength * normalizedTime;

        if (shakeTimer <= 0f)
        {
            shakeDuration = 0f;
            shakeStrength = 0f;
        }

        // 추적 위치에 짧은 흔들림만 더해 플레이어 중심 카메라를 유지한다.
        return new Vector3(offset.x, offset.y, 0f);
    }

    private void WarnMissingTargetOnce()
    {
        if (warnedMissingTarget)
        {
            return;
        }

        warnedMissingTarget = true;
        // 초기 설정 중 타겟이 비어 있을 수 있으므로 매 프레임 대신 한 번만 경고한다.
        Debug.LogWarning($"{nameof(CameraFollow)} on {name} has no target assigned.", this);
    }
}
