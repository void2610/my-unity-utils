using UnityEngine;
using UnityToolbarExtender;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class SceneSwitchLeftButton
{
    public const string TITLE_SCENE_PATH = "Assets/Scenes/TitleScene.unity";
    public const string MAIN_SCENE_PATH = "Assets/Scenes/MainScene.unity";
	
    static SceneSwitchLeftButton()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUILeft);
    }

    private static void OnToolbarGUILeft()
    {
        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("TITLE", "")))
            EditorSceneManager.OpenScene(TITLE_SCENE_PATH, OpenSceneMode.Single);
        if (GUILayout.Button(new GUIContent("MAIN", "")))
        	EditorSceneManager.OpenScene(MAIN_SCENE_PATH, OpenSceneMode.Single);
    }
}