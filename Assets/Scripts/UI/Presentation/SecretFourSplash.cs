using System.Collections;
using MN7.Store;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretFour.Presentation
{
    /// <summary>
    /// Launch splash: full black screen, game logo in the centre, company logo at the bottom.
    /// Created automatically before the first scene loads and drawn above everything. It stays
    /// until the main menu and store are ready (between the minimum and maximum time set in
    /// Resources/SplashScreenSettings) and holds back the banner ad while visible.
    /// </summary>
    public class SecretFourSplash : MonoBehaviour
    {
        private SplashScreenSettings settings;
        private CanvasGroup logos;
        private RectTransform gameLogo;
        private CanvasGroup fade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<SecretFourSplash>() != null)
                return;

            GameObject root = new("Secret Four Splash");
            DontDestroyOnLoad(root);
            root.AddComponent<SecretFourSplash>();
        }

        private void Awake()
        {
            settings = Resources.Load<SplashScreenSettings>("SplashScreenSettings");
            MN7AdMobBootstrap.SetBannerHeldBySplash(true);
            Build();
        }

        private void OnDestroy() => MN7AdMobBootstrap.SetBannerHeldBySplash(false);

        private IEnumerator Start()
        {
            float min = settings != null ? settings.minimumSeconds : 2.5f;
            float max = settings != null ? Mathf.Max(min, settings.maximumSeconds) : 6f;
            float start = Time.realtimeSinceStartup;
            float Elapsed() => Time.realtimeSinceStartup - start;

            // Logos fade in on black with a gentle settle.
            const float intro = .7f;
            for (float t = 0; t < intro; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / intro);
                logos.alpha = k;
                gameLogo.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, k);
                yield return null;
            }
            logos.alpha = 1f;
            gameLogo.localScale = Vector3.one;

            while (Elapsed() < max && (Elapsed() < min || !GameReady()))
                yield return null;

            // Fade the whole splash out to reveal the menu.
            const float outro = .45f;
            for (float t = 0; t < outro; t += Time.unscaledDeltaTime)
            {
                fade.alpha = 1f - Mathf.SmoothStep(0, 1, t / outro);
                yield return null;
            }

            Destroy(gameObject);
        }

        private static bool GameReady()
        {
            if (!SceneManager.GetActiveScene().isLoaded)
                return false;

            MN7StoreManager store = MN7StoreManager.Instance;
            return store == null || store.IsReady;
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            gameObject.AddComponent<GraphicRaycaster>(); // blocks taps while the splash is up
            fade = gameObject.AddComponent<CanvasGroup>();

            RectTransform root = (RectTransform)transform;

            Image black = NewRect(root, "Black").gameObject.AddComponent<Image>();
            black.color = Color.black;
            Stretch(black.rectTransform);

            RectTransform logoGroup = NewRect(root, "Logos");
            Stretch(logoGroup);
            logos = logoGroup.gameObject.AddComponent<CanvasGroup>();
            logos.alpha = 0f;
            logos.blocksRaycasts = false;

            float gameWidth = settings != null ? settings.gameLogoWidth : .5f;
            float companyWidth = settings != null ? settings.companyLogoWidth : .38f;

            // Centre logo: anchored to the middle, width relative to screen width.
            gameLogo = LogoImage(logoGroup, "Game logo", settings ? settings.gameLogo : null);
            gameLogo.anchorMin = new Vector2(.5f - gameWidth / 2f, .5f);
            gameLogo.anchorMax = new Vector2(.5f + gameWidth / 2f, .5f);
            gameLogo.pivot = new Vector2(.5f, .5f);
            gameLogo.anchoredPosition = new Vector2(0, 0);
            FitHeight(gameLogo);

            // Bottom logo: centred horizontally, a little above the bottom edge (and above any nav bar).
            RectTransform company = LogoImage(logoGroup, "Company logo", settings ? settings.companyLogo : null);
            float bottom = Mathf.Max(.045f, Screen.height > 0 ? Screen.safeArea.yMin / Screen.height + .02f : .045f);
            company.anchorMin = new Vector2(.5f - companyWidth / 2f, bottom);
            company.anchorMax = new Vector2(.5f + companyWidth / 2f, bottom);
            company.pivot = new Vector2(.5f, 0f);
            company.anchoredPosition = Vector2.zero;
            FitHeight(company);
        }

        /// <summary>Height follows width and the sprite's aspect ratio, on any screen size.</summary>
        private static void FitHeight(RectTransform rect)
        {
            Image image = rect.GetComponent<Image>();
            if (image.sprite == null)
                return;

            AspectRatioFitter fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
        }

        private static RectTransform LogoImage(Transform parent, string name, Sprite sprite)
        {
            RectTransform rect = NewRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform NewRect(Transform parent, string name)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
