using UnityEngine;

/// <summary>
/// Applies mobile-friendly runtime defaults without affecting desktop play.
/// </summary>
public class MobileRuntimeSettings : MonoBehaviour
{
    public bool mobileOnly = true;
    public int targetFrameRate = 60;
    public bool forceLandscape = true;
    public bool allowLandscapeRight = true;
    public bool enableReducedFxOnMobile = true;

    private void Awake()
    {
        if (mobileOnly && !Application.isMobilePlatform)
        {
            return;
        }

        Input.multiTouchEnabled = false;
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (forceLandscape)
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = allowLandscapeRight;
        }

        if (enableReducedFxOnMobile)
        {
            ParticleManager.Instance?.SetReducedFX(true);
        }
    }
}
