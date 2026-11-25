using System;                 // Action (was missing -> compile error)
using System.Collections;
using UnityEngine;

// A door lock. "on" = locked, "off" = unlocked, "toggle" flips it.
// Animates a bolt child sliding in and out, and optionally plays a click.
// Other scripts can read IsLocked or subscribe to LockStateChanged.
public class SmartDoorLock : SmartObject
{
    [SerializeField] private Transform bolt;              // child mesh that slides
    [SerializeField] private Vector3 boltAxis = Vector3.right; // local direction the bolt slides
    [SerializeField] private float boltTravel = 0.05f;    // metres the bolt moves
    [SerializeField] private float boltDuration = 0.3f;
    [SerializeField] private AudioSource clickSound;      // optional

    public bool IsLocked => IsOn;

    // Other scripts (e.g. a door-opening script) can check this before opening.
    public static event Action<SmartDoorLock> LockStateChanged;

    // Reset on play start so stale subscribers don't survive with Domain Reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => LockStateChanged = null;

    private Coroutine boltRoutine;
    private Vector3 boltUnlockedPos;

    private void Awake()
    {
        if (clickSound != null) clickSound.playOnAwake = false;
        if (bolt != null) boltUnlockedPos = bolt.localPosition; // starts unlocked
    }

    protected override void TurnOn()   // lock
    {
        base.TurnOn();
        MoveBolt(boltTravel);
        LockStateChanged?.Invoke(this);
    }

    protected override void TurnOff()  // unlock
    {
        base.TurnOff();
        MoveBolt(0f);
        LockStateChanged?.Invoke(this);
    }

    private void MoveBolt(float targetOffset)
    {
        if (clickSound != null) clickSound.Play();
        if (bolt == null) return;

        if (boltRoutine != null) StopCoroutine(boltRoutine);

        Vector3 target = boltUnlockedPos + boltAxis.normalized * targetOffset;

        // Coroutines can't run on inactive objects (and would throw); snap instead.
        if (!isActiveAndEnabled || boltDuration <= 0f)
        {
            bolt.localPosition = target;
            return;
        }
        boltRoutine = StartCoroutine(SlideBolt(target));
    }

    private IEnumerator SlideBolt(Vector3 target)
    {
        Vector3 start = bolt.localPosition;
        float t = 0f;
        while (t < boltDuration)
        {
            t += Time.deltaTime;
            bolt.localPosition = Vector3.Lerp(start, target, t / boltDuration);
            yield return null;
        }
        bolt.localPosition = target;
        boltRoutine = null;
    }
}
