using UnityEngine;

namespace SecretFour.Presentation
{
    /// <summary>Art and timing for the launch splash. Lives in Resources so the splash needs no scene setup.</summary>
    [CreateAssetMenu(fileName = "SplashScreenSettings", menuName = "Secret Four/Splash Screen Settings")]
    public class SplashScreenSettings : ScriptableObject
    {
        [Tooltip("Game logo shown in the centre.")]
        public Sprite gameLogo;

        [Tooltip("Company logo shown at the bottom.")]
        public Sprite companyLogo;

        [Tooltip("Game logo width as a fraction of screen width.")]
        [Range(.2f, .9f)] public float gameLogoWidth = .5f;

        [Tooltip("Company logo width as a fraction of screen width.")]
        [Range(.1f, .8f)] public float companyLogoWidth = .38f;

        [Min(0.5f)] public float minimumSeconds = 2.5f;
        [Min(1f)] public float maximumSeconds = 6f;
    }
}
