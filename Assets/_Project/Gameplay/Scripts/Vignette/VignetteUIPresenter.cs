using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

using Memories.Narrative;

[DisallowMultipleComponent]
public class VignetteUIPresenter : MonoBehaviour
{
    [SerializeField] private VignettePanelController panelController;

    [Header("Effect Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private float glitchDuration = 0.15f;
    [SerializeField] private float staticDuration = 0.1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource typewriterAudioSource;
    [SerializeField] private AudioClip typewriterTickClip;
    [SerializeField] private float typewriterTickVolume = 1f;

    private VignetteManager _vignetteManager;
    private PlayerMovement _playerMovement;
    private Coroutine _displayCoroutine;
    private bool _isLineDisplaying;

    private TMP_Text vignetteText => panelController != null ? panelController.GetTextDisplay() : null;

    private void Start()
    {
        _vignetteManager = VignetteManager.Instance;
        if (_vignetteManager == null)
        {
            _vignetteManager = FindFirstObjectByType<VignetteManager>();
        }

        if (_vignetteManager != null)
        {
            _vignetteManager.OnVignetteTriggered += OnVignetteTriggered;
            _vignetteManager.OnVignetteLineShown += OnVignetteLineShown;
            _vignetteManager.OnVignetteClosed += OnVignetteClosed;
            _vignetteManager.OnVignetteFreeze += OnVignetteFreeze;
            _vignetteManager.OnVignetteUnfreeze += OnVignetteUnfreeze;
        }

        if (panelController == null)
        {
            panelController = GetComponent<VignettePanelController>();
        }

        if (panelController != null)
        {
            panelController.Hide();
        }

        _playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (!_vignetteManager.IsShowing)
        {
            return;
        }

        if (UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            OnInteract();
        }
    }

    private void OnDestroy()
    {
        if (_vignetteManager != null)
        {
            _vignetteManager.OnVignetteTriggered -= OnVignetteTriggered;
            _vignetteManager.OnVignetteLineShown -= OnVignetteLineShown;
            _vignetteManager.OnVignetteClosed -= OnVignetteClosed;
            _vignetteManager.OnVignetteFreeze -= OnVignetteFreeze;
            _vignetteManager.OnVignetteUnfreeze -= OnVignetteUnfreeze;
        }
    }

    private void OnVignetteFreeze()
    {
        if (_playerMovement != null)
        {
            _playerMovement.SetMovementEnabled(false);
        }
    }

    private void OnVignetteUnfreeze()
    {
        if (_playerMovement != null)
        {
            _playerMovement.SetMovementEnabled(true);
        }
    }

    private void OnVignetteTriggered(VignetteData vignetteData)
    {
        if (panelController != null)
        {
            panelController.Show();
        }
    }

    private void OnVignetteLineShown(string line, int currentIndex, int totalLines)
    {
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }

        VignetteData activeVignette = _vignetteManager.GetActiveVignette();
        if (activeVignette != null)
        {
            _displayCoroutine = StartCoroutine(DisplayLineWithEffects(line, activeVignette.displayEffects));
        }
        else
        {
            AppendLineToText(line);
        }
    }

    private void OnVignetteClosed(VignetteData vignetteData)
    {
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }

        _isLineDisplaying = false;
        if (vignetteText != null)
        {
            vignetteText.text = "";
        }

        if (panelController != null)
        {
            panelController.Hide();
        }
    }

    public void OnInteract()
    {
        if (!_vignetteManager.IsShowing)
        {
            return;
        }

        if (_isLineDisplaying)
        {
            return;
        }

        _vignetteManager.ShowNextLine();
    }

    private IEnumerator DisplayLineWithEffects(string line, VignetteEffect effects)
    {
        if (effects.HasFlag(VignetteEffect.Typewriter))
        {
            yield return StartCoroutine(TypewriterEffect(line, effects));
        }
        else
        {
            AppendLineToText(line);
        }
    }

    private void AppendLineToText(string line)
    {
        if (vignetteText == null) return;

        if (vignetteText.text.Length > 0)
        {
            vignetteText.text += "\n\n";
        }
        vignetteText.text += line;
    }

    private IEnumerator TypewriterEffect(string line, VignetteEffect effects)
    {
        if (vignetteText == null) yield break;

        _isLineDisplaying = true;
        string displayText = vignetteText.text;
        if (displayText.Length > 0)
        {
            displayText += "\n\n";
        }

        string baseText = displayText;

        for (int i = 0; i < line.Length; i++)
        {
            vignetteText.text = baseText + line.Substring(0, i + 1);

            char visibleCharacter = line[i];
            PlayTypewriterTick(visibleCharacter);

            if (effects.HasFlag(VignetteEffect.Glitch) && Random.value < 0.1f)
            {
                yield return StartCoroutine(GlitchEffect(baseText, line, i + 1));
            }

            if (effects.HasFlag(VignetteEffect.Static) && Random.value < 0.05f)
            {
                yield return StartCoroutine(StaticEffect(baseText, line, i + 1));
            }

            yield return new WaitForSeconds(typewriterSpeed);
        }

        _isLineDisplaying = false;
    }

    private IEnumerator GlitchEffect(string baseText, string line, int charCount)
    {
        string originalText = vignetteText.text;
        float elapsed = 0f;

        while (elapsed < glitchDuration)
        {
            // Random character glitch
            string glitchedLine = line.Substring(0, charCount);
            int glitchPos = Random.Range(0, glitchedLine.Length);
            char glitchChar = (char)Random.Range(33, 126);
            glitchedLine = glitchedLine.Substring(0, glitchPos) + glitchChar + glitchedLine.Substring(glitchPos + 1);

            vignetteText.text = baseText + glitchedLine;
            elapsed += 0.02f;
            yield return new WaitForSeconds(0.02f);
        }

        vignetteText.text = originalText;
    }

    private IEnumerator StaticEffect(string baseText, string line, int charCount)
    {
        string originalText = vignetteText.text;
        float elapsed = 0f;

        while (elapsed < staticDuration)
        {
            // Add random static characters
            string staticLine = line.Substring(0, charCount);
            for (int i = 0; i < Random.Range(1, 4); i++)
            {
                int pos = Random.Range(0, staticLine.Length);
                char staticChar = (char)Random.Range(176, 180);
                staticLine = staticLine.Substring(0, pos) + staticChar + staticLine.Substring(pos);
            }

            vignetteText.text = baseText + staticLine;
            elapsed += 0.03f;
            yield return new WaitForSeconds(0.03f);
        }

        vignetteText.text = originalText;
    }

    private void PlayTypewriterTick(char visibleCharacter)
    {
        if (char.IsWhiteSpace(visibleCharacter))
        {
            return;
        }

        if (typewriterAudioSource == null || typewriterTickClip == null)
        {
            return;
        }

        typewriterAudioSource.PlayOneShot(typewriterTickClip, typewriterTickVolume);
    }
}
