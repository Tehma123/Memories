using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Memories.Narrative
{
    public class VignettePanelController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private RectTransform paperPanel;
        [SerializeField] private Image paperBackground;

        [Header("Border")]
        [SerializeField] private Image borderFrame;

        [Header("Content")]
        [SerializeField] private TMP_Text textDisplay;

        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;


        private Coroutine _blinkCoroutine;

        // --- Public API ---

        public void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
            gameObject.SetActive(false);
            overlay.alpha = 0f;
        }

        public TMP_Text GetTextDisplay() => textDisplay;

        // --- Private ---
        private IEnumerator FadeIn()
        {
            float t = 0f;
            while (t < fadeInDuration)
            {
                overlay.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
                t += Time.deltaTime;
                yield return null;
            }
            overlay.alpha = 1f;
        }
    }
}