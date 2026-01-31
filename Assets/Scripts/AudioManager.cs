using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
	public static AudioManager Instance { get; private set; }

	[Header("Audio Sources")]
	[SerializeField] private AudioSource musicSource;
	[SerializeField] private AudioSource sfxSource;

	[Header("Music Settings")]
	[SerializeField] private float fadeDuration = 1f;

	[Header("Audio Clips (opcional)")]
	[SerializeField] private List<AudioClipData> musicClips = new List<AudioClipData>();
	[SerializeField] private List<AudioClipData> sfxClips = new List<AudioClipData>();

	private Dictionary<string, AudioClip> musicLibrary = new Dictionary<string, AudioClip>();
	private Dictionary<string, AudioClip> sfxLibrary = new Dictionary<string, AudioClip>();
	private Coroutine fadeCoroutine;

	// Volúmenes desde configuración
	private float volGeneral = 1f;
	private float volMusic = 0.7f;
	private float volEffects = 0.7f;

	private void Awake()
	{
		// Singleton
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		// Crear AudioSources si no están asignados
		if (musicSource == null)
		{
			musicSource = gameObject.AddComponent<AudioSource>();
			musicSource.loop = true;
			musicSource.playOnAwake = false;
		}

		if (sfxSource == null)
		{
			sfxSource = gameObject.AddComponent<AudioSource>();
			sfxSource.loop = false;
			sfxSource.playOnAwake = false;
		}

		// Cargar clips en diccionarios
		foreach (var clip in musicClips)
		{
			if (clip.clip != null && !string.IsNullOrEmpty(clip.name))
				musicLibrary[clip.name] = clip.clip;
		}

		foreach (var clip in sfxClips)
		{
			if (clip.clip != null && !string.IsNullOrEmpty(clip.name))
				sfxLibrary[clip.name] = clip.clip;
		}
	}

	private void Start()
	{
		// Cargar volúmenes de la configuración guardada
		LoadVolumeSettings();
	}

	/// <summary>
	/// Carga los volúmenes desde SaveManager y los aplica
	/// </summary>
	public void LoadVolumeSettings()
	{
		if (SaveManager.Instance == null)
		{
			Debug.LogWarning("SaveManager not found! Using default volumes.");
			return;
		}

		SettingsData settings = SaveManager.Instance.LoadSettings();
		volGeneral = settings.volGeneral;
		volMusic = settings.volMusic;
		volEffects = settings.volEffects;

		ApplyMusicVolume();
		Debug.Log($"Audio settings loaded - General: {volGeneral}, Music: {volMusic}, Effects: {volEffects}");
	}

	/// <summary>
	/// Aplica el volumen actual a la música (General * Music)
	/// </summary>
	private void ApplyMusicVolume()
	{
		if (musicSource != null && musicSource.isPlaying)
		{
			musicSource.volume = GetMusicVolume();
		}
	}

	/// <summary>
	/// Obtiene el volumen final de música (General * Music)
	/// </summary>
	public float GetMusicVolume() => volGeneral * volMusic;

	/// <summary>
	/// Obtiene el volumen final de efectos (General * Effects)
	/// </summary>
	public float GetSFXVolume() => volGeneral * volEffects;

	#region Music

	/// <summary>
	/// Reproduce música con fade-in. Si ya hay música, hace fade-out primero.
	/// </summary>
	public void PlayMusic(AudioClip clip, bool fade = true)
	{
		if (clip == null) return;

		if (fadeCoroutine != null)
			StopCoroutine(fadeCoroutine);

		if (fade && musicSource.isPlaying)
		{
			fadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
		}
		else if (fade)
		{
			fadeCoroutine = StartCoroutine(FadeInMusic(clip));
		}
		else
		{
			musicSource.clip = clip;
			musicSource.volume = GetMusicVolume();
			musicSource.Play();
		}
	}

	/// <summary>
	/// Reproduce música por nombre (debe estar en la lista musicClips)
	/// </summary>
	public void PlayMusic(string name, bool fade = true)
	{
		if (musicLibrary.TryGetValue(name, out AudioClip clip))
		{
			PlayMusic(clip, fade);
		}
		else
		{
			Debug.LogWarning($"Music clip '{name}' not found!");
		}
	}

	/// <summary>
	/// Detiene la música con fade-out
	/// </summary>
	public void StopMusic(bool fade = true)
	{
		if (fadeCoroutine != null)
			StopCoroutine(fadeCoroutine);

		if (fade)
		{
			fadeCoroutine = StartCoroutine(FadeOutMusic());
		}
		else
		{
			musicSource.Stop();
		}
	}

	/// <summary>
	/// Pausa/reanuda la música
	/// </summary>
	public void PauseMusic(bool pause)
	{
		if (pause)
			musicSource.Pause();
		else
			musicSource.UnPause();
	}

	private IEnumerator FadeInMusic(AudioClip clip)
	{
		musicSource.clip = clip;
		musicSource.volume = 0f;
		musicSource.Play();

		float targetVolume = GetMusicVolume();
		float elapsed = 0f;

		while (elapsed < fadeDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeDuration);
			yield return null;
		}

		musicSource.volume = targetVolume;
	}

	private IEnumerator FadeOutMusic()
	{
		float startVolume = musicSource.volume;
		float elapsed = 0f;

		while (elapsed < fadeDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
			yield return null;
		}

		musicSource.Stop();
	}

	private IEnumerator CrossfadeMusic(AudioClip newClip)
	{
		// Fade out
		float startVolume = musicSource.volume;
		float elapsed = 0f;

		while (elapsed < fadeDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
			yield return null;
		}

		// Cambiar clip y fade in
		musicSource.clip = newClip;
		musicSource.Play();

		float targetVolume = GetMusicVolume();
		elapsed = 0f;

		while (elapsed < fadeDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeDuration);
			yield return null;
		}

		musicSource.volume = targetVolume;
	}

	#endregion

	#region SFX

	/// <summary>
	/// Reproduce un efecto de sonido (se superpone a todo)
	/// </summary>
	public void PlaySFX(AudioClip clip)
	{
		if (clip == null) return;
		sfxSource.PlayOneShot(clip, GetSFXVolume());
	}

	/// <summary>
	/// Reproduce un efecto de sonido por nombre
	/// </summary>
	public void PlaySFX(string name)
	{
		if (sfxLibrary.TryGetValue(name, out AudioClip clip))
		{
			PlaySFX(clip);
		}
		else
		{
			Debug.LogWarning($"SFX clip '{name}' not found!");
		}
	}

	/// <summary>
	/// Reproduce SFX en una posición 3D
	/// </summary>
	public void PlaySFXAtPosition(AudioClip clip, Vector3 position)
	{
		if (clip == null) return;
		AudioSource.PlayClipAtPoint(clip, position, GetSFXVolume());
	}

	#endregion

	#region Volume Control

	/// <summary>
	/// Actualiza los volúmenes manualmente (llamar cuando cambien los sliders)
	/// </summary>
	public void UpdateVolumes(float general, float music, float effects)
	{
		volGeneral = Mathf.Clamp01(general);
		volMusic = Mathf.Clamp01(music);
		volEffects = Mathf.Clamp01(effects);
		ApplyMusicVolume();
	}

	/// <summary>
	/// Recarga los volúmenes desde la configuración guardada
	/// </summary>
	public void RefreshVolumeFromSettings()
	{
		LoadVolumeSettings();
	}

	#endregion
}

[System.Serializable]
public class AudioClipData
{
	public string name;
	public AudioClip clip;
}
