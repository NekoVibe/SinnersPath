using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper class to reference scenes in the Inspector via drag-and-drop.
/// Stores scene path and provides methods to load the scene.
/// </summary>
[System.Serializable]
public class SceneReference
{
	#if UNITY_EDITOR
	[SerializeField] private Object sceneAsset;
	#endif

	[SerializeField] private string scenePath = "";

	public string ScenePath => scenePath;

	public string SceneName
	{
		get
		{
			if (string.IsNullOrEmpty(scenePath))
				return "";

			// Extract scene name from path (e.g., "Assets/Scenes/Level1.unity" -> "Level1")
			string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
			return name;
		}
	}

	public bool IsValid => !string.IsNullOrEmpty(scenePath);

	#if UNITY_EDITOR
	private void OnValidate()
	{
		if (sceneAsset != null)
		{
			string assetPath = AssetDatabase.GetAssetPath(sceneAsset);

			// Verify it's actually a scene file
			if (assetPath.EndsWith(".unity"))
			{
				scenePath = assetPath;
			}
			else
			{
				Debug.LogWarning("The assigned asset is not a scene file!");
				sceneAsset = null;
				scenePath = "";
			}
		}
		else
		{
			scenePath = "";
		}
	}
	#endif

	// Implicit conversion to string (returns scene name)
	public static implicit operator string(SceneReference sceneReference)
	{
		return sceneReference?.SceneName ?? "";
	}

	public override string ToString()
	{
		return SceneName;
	}
}
