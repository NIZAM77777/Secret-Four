using System.Collections;
using UnityEngine;

public class WinningCoinGlow : MonoBehaviour
{
    [Header("Glow Settings")]

    [SerializeField]
    private float glowScale = 1.2f;

    [SerializeField]
    private float glowSpeed = 2f;


    private Vector3 originalScale;

    private Coroutine glowCoroutine;


    private void Awake()
    {
        originalScale =
            transform.localScale;
    }


    // =========================================
    // START GLOW
    // =========================================

    public void StartGlow()
    {
        if (glowCoroutine != null)
        {
            StopCoroutine(
                glowCoroutine
            );
        }


        glowCoroutine =
            StartCoroutine(
                GlowRoutine()
            );
    }


    // =========================================
    // STOP GLOW
    // =========================================

    public void StopGlow()
    {
        if (glowCoroutine != null)
        {
            StopCoroutine(
                glowCoroutine
            );

            glowCoroutine = null;
        }


        transform.localScale =
            originalScale;
    }


    // =========================================
    // GLOW ANIMATION
    // =========================================

    private IEnumerator GlowRoutine()
    {
        while (true)
        {
            float scale =
                1f +
                Mathf.Sin(
                    Time.time * glowSpeed
                )
                *
                (glowScale - 1f);


            transform.localScale =
                originalScale * scale;


            yield return null;
        }
    }
}