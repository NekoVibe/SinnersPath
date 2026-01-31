using UnityEngine;

public class HUDCanvasPersistence : MonoBehaviour
{
	public static HUDCanvasPersistence Instance { get; private set; }

	private void Awake()
	{
		// Singleton: solo puede haber un HUD_Canvas
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject); // ✅ Hace persistente HUD_Canvas

		Debug.Log("HUD_Canvas is now persistent");
	}
}
