using UnityEngine;

/// <summary>
/// Applies mobile-friendly runtime defaults without affecting desktop play.
/// </summary>
public class MobileRuntimeSettings : MonoBehaviour
{
    public bool mobileOnly = true;
    public int targetFrameRate = 60;
    public bool forcePortrait = true;
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

        if (forcePortrait)
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }

        if (enableReducedFxOnMobile)
        {
            ParticleManager.Instance?.SetReducedFX(true);
        }
    }
}
