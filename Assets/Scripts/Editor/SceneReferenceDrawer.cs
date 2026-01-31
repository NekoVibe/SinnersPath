using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		// Get the sceneAsset property
		SerializedProperty sceneAssetProp = property.FindPropertyRelative("sceneAsset");
		SerializedProperty scenePathProp = property.FindPropertyRelative("scenePath");

		// Draw the scene asset field (drag-and-drop)
		EditorGUI.BeginChangeCheck();
		Object newScene = EditorGUI.ObjectField(position, label, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);

		if (EditorGUI.EndChangeCheck())
		{
			sceneAssetProp.objectReferenceValue = newScene;

			// Update the path when scene changes
			if (newScene != null)
			{
				string path = AssetDatabase.GetAssetPath(newScene);
				if (path.EndsWith(".unity"))
				{
					scenePathProp.stringValue = path;
				}
				else
				{
					sceneAssetProp.objectReferenceValue = null;
					scenePathProp.stringValue = "";
					Debug.LogWarning("Please assign a Scene asset (.unity file)");
				}
			}
			else
			{
				scenePathProp.stringValue = "";
			}
		}

		EditorGUI.EndProperty();
	}
}
