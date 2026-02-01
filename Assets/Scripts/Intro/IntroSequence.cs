using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class IntroSequence : MonoBehaviour
{
    [Header("Scene Navigation")]
    [SerializeField] private SceneReference nextScene;

    [Header("Logo Elements")]
    [SerializeField] private CanvasGroup logoGroup;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.8f;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 1.2f;
    [SerializeField] private float delayBeforeNextScene = 0.5f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip ambientSound;

    private AudioSource audioSource;

    private void Start()
    {
        InitializeElements();

        if (ambientSound != null)
            PlayAmbient();

        StartCoroutine(RunIntroSequence());
    }

    private void InitializeElements()
    {
        if (logoGroup != null)
            logoGroup.alpha = 0f;

        if (SceneFader.Instance != null)
            SceneFader.Instance.SetBlack();
    }

    private IEnumerator RunIntroSequence()
    {
        // Breathe
        yield return new WaitForSecondsRealtime(initialDelay);

        // Fade in from black, revealing the logo
        if (logoGroup != null)
            logoGroup.alpha = 1f;

        if (SceneFader.Instance != null)
            yield return StartCoroutine(FadeSceneIn(fadeInDuration));

        // Hold - let it breathe
        yield return new WaitForSecondsRealtime(holdDuration);

        // Fade out to black
        if (SceneFader.Instance != null)
            yield return StartCoroutine(FadeSceneOut(fadeOutDuration));

        // Brief pause in darkness
        yield return new WaitForSecondsRealtime(delayBeforeNextScene);

        // Done
        LoadNextScene();
    }

    private IEnumerator FadeSceneIn(float duration)
    {
        yield return SceneFader.Instance.FadeInCoroutine(duration);
    }

    private IEnumerator FadeSceneOut(float duration)
    {
        yield return SceneFader.Instance.FadeOutCoroutine(duration);
    }

    private void LoadNextScene()
    {
        if (nextScene != null && nextScene.IsValid)
            SceneManager.LoadScene(nextScene.SceneName);
        else
            SceneManager.LoadScene(1);
    }

    private void PlayAmbient()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = ambientSound;
        audioSource.loop = false;
        audioSource.volume = 0.3f;
        audioSource.Play();
    }
}
