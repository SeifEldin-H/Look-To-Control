using UnityEngine;
using UnityEngine.Video;

// A TV that swaps its screen material on/off and can drive a VideoPlayer.
// Setup: assign the screen Renderer and both materials. If a VideoPlayer is
// assigned, the TV plays that video when turned on and stops it when turned off.
public class SmartTV : SmartObject
{
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private Material screenOnMaterial;   // e.g. emissive or video-output material
    [SerializeField] private Material screenOffMaterial;  // dark glass
    [SerializeField] private VideoPlayer videoPlayer;     // optional

    private void Awake()
    {
        // Start in the "off" state regardless of how the scene was left in the editor.
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
        }
        SetScreen(screenOffMaterial);
    }

    protected override void TurnOn()
    {
        base.TurnOn();
        SetScreen(screenOnMaterial);
        if (videoPlayer != null)
            videoPlayer.Play();
    }

    protected override void TurnOff()
    {
        base.TurnOff();
        // Stop() is safe in any state; isPlaying is false while the video is still preparing,
        // so checking it could leave a preparing video to start playing later.
        if (videoPlayer != null)
            videoPlayer.Stop();
        SetScreen(screenOffMaterial);
    }

    // sharedMaterial swaps the asset without cloning a per-renderer instance.
    private void SetScreen(Material mat)
    {
        if (screenRenderer != null && mat != null)
            screenRenderer.sharedMaterial = mat;
    }
}
