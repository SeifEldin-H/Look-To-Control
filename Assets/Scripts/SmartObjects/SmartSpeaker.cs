using System.Collections;
using UnityEngine;

// A speaker that fades its AudioSource in and out instead of cutting abruptly.
// Turning off pauses the audio, turning on resumes it where it left off.
[RequireComponent(typeof(AudioSource))]
public class SmartSpeaker : SmartObject
{
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float fadeDuration = 0.5f;

    private AudioSource speaker;
    private Coroutine fadeRoutine;
    private bool paused;

    private void Awake()
    {
        speaker = GetComponent<AudioSource>();
        speaker.playOnAwake = false; // otherwise it would start playing silently
        speaker.Stop();
        speaker.volume = 0f;
    }

    protected override void TurnOn()
    {
        base.TurnOn();

        // Pause() followed by Play() would restart the clip; UnPause() resumes it.
        if (paused) { speaker.UnPause(); paused = false; }
        else if (!speaker.isPlaying) speaker.Play();

        StartFade(maxVolume);
    }

    protected override void TurnOff()
    {
        base.TurnOff();
        StartFade(0f);
    }

    private void StartFade(float target)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        // Coroutines can't run on inactive objects (and would throw); snap instead.
        if (!isActiveAndEnabled || fadeDuration <= 0f)
        {
            Finish(target);
            return;
        }
        fadeRoutine = StartCoroutine(FadeTo(target));
    }

    private IEnumerator FadeTo(float target)
    {
        float start = speaker.volume;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            speaker.volume = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        Finish(target);
        fadeRoutine = null;
    }

    private void Finish(float target)
    {
        speaker.volume = target;
        if (target <= 0f && speaker.isPlaying)
        {
            speaker.Pause();
            paused = true;
        }
    }
}
