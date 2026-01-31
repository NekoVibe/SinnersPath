using UnityEngine;

public class ObjectPersistanceScript : MonoBehaviour
{
	public static ObjectPersistanceScript Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		Debug.Log("HUD_Canvas is now persistent");
	}
}
