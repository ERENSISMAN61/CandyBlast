using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom editor window for creating and managing levels
/// Access via: Window > CandyBlast > Level Editor
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private LevelData[] allLevels;
    private LevelData selectedLevel;
    private string newLevelName = "New Level";
    private int newLevelNumber = 1;
    private string searchFilter = "";

    private bool showCreateSection = true;
    private bool showLevelListSection = true;
    private bool showSelectedLevelSection = true;

    [MenuItem("Window/CandyBlast/Level Editor")]
    public static void ShowWindow()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshLevelList();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        EditorGUILayout.Space(10);

        DrawCreateNewLevelSection();
        EditorGUILayout.Space(10);

        DrawLevelListSection();
        EditorGUILayout.Space(10);

        DrawSelectedLevelSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField("🍬 CandyBlast Level Editor", titleStyle, GUILayout.Height(30));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
        {
            RefreshLevelList();
        }
        if (GUILayout.Button("Open Levels Folder", GUILayout.Height(25)))
        {
            string path = "Assets/Resources/Levels";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");
            }
            EditorUtility.RevealInFinder(path);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawCreateNewLevelSection()
    {
        showCreateSection = EditorGUILayout.BeginFoldoutHeaderGroup(showCreateSection, "Create New Level");

        if (showCreateSection)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("New Level Settings", EditorStyles.boldLabel);

            newLevelName = EditorGUILayout.TextField("Level Name", newLevelName);
            newLevelNumber = EditorGUILayout.IntField("Level Number", newLevelNumber);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Create New Level", GUILayout.Height(30)))
            {
                CreateNewLevel();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawLevelListSection()
    {
        showLevelListSection = EditorGUILayout.BeginFoldoutHeaderGroup(showLevelListSection, $"All Levels ({(allLevels != null ? allLevels.Length : 0)})");

        if (showLevelListSection)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Search filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (allLevels == null || allLevels.Length == 0)
            {
                EditorGUILayout.HelpBox("No levels found. Create a new level to get started!", MessageType.Info);
            }
            else
            {
                var filteredLevels = string.IsNullOrEmpty(searchFilter)
                    ? allLevels
                    : allLevels.Where(l => l.LevelName.ToLower().Contains(searchFilter.ToLower())
                                          || l.LevelNumber.ToString().Contains(searchFilter)).ToArray();

                foreach (var level in filteredLevels)
                {
                    DrawLevelItem(level);
                }
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawLevelItem(LevelData level)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        // Level info
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField($"#{level.LevelNumber}: {level.LevelName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{level.Rows}x{level.Columns}, {level.ColorCount} colors", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Buttons
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            selectedLevel = level;
            Selection.activeObject = level;
        }

        if (GUILayout.Button("Edit", GUILayout.Width(60)))
        {
            Selection.activeObject = level;
            EditorGUIUtility.PingObject(level);
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("Delete Level",
                $"Are you sure you want to delete '{level.LevelName}'?",
                "Delete", "Cancel"))
            {
                DeleteLevel(level);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSelectedLevelSection()
    {
        showSelectedLevelSection = EditorGUILayout.BeginFoldoutHeaderGroup(showSelectedLevelSection, "Selected Level Details");

        if (showSelectedLevelSection)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (selectedLevel == null)
            {
                EditorGUILayout.HelpBox("No level selected. Select a level from the list above.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Level Details", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                // Display level info
                EditorGUILayout.LabelField($"Name: {selectedLevel.LevelName}");
                EditorGUILayout.LabelField($"Number: {selectedLevel.LevelNumber}");
                EditorGUILayout.LabelField($"Grid: {selectedLevel.Rows}x{selectedLevel.Columns}");
                EditorGUILayout.LabelField($"Colors: {selectedLevel.ColorCount}");
                EditorGUILayout.LabelField($"Thresholds: A={selectedLevel.ThresholdA}, B={selectedLevel.ThresholdB}, C={selectedLevel.ThresholdC}");
                EditorGUILayout.LabelField($"Target Score: {selectedLevel.TargetScore}");

                if (selectedLevel.MaxMoves > 0)
                    EditorGUILayout.LabelField($"Max Moves: {selectedLevel.MaxMoves}");

                if (selectedLevel.TimeLimit > 0)
                    EditorGUILayout.LabelField($"Time Limit: {selectedLevel.TimeLimit}s");

                EditorGUILayout.Space(5);

                if (!string.IsNullOrEmpty(selectedLevel.Description))
                {
                    EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(selectedLevel.Description, EditorStyles.wordWrappedLabel);
                }

                EditorGUILayout.Space(10);

                if (GUILayout.Button("Open in Inspector", GUILayout.Height(25)))
                {
                    Selection.activeObject = selectedLevel;
                    EditorGUIUtility.PingObject(selectedLevel);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Duplicate Level", GUILayout.Height(25)))
                {
                    DuplicateLevel(selectedLevel);
                }
                if (GUILayout.Button("Test Level", GUILayout.Height(25)))
                {
                    TestLevel(selectedLevel);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void RefreshLevelList()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelData");
        allLevels = new LevelData[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            allLevels[i] = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        }

        // Sort by level number
        allLevels = allLevels.OrderBy(l => l.LevelNumber).ToArray();

        Debug.Log($"Loaded {allLevels.Length} levels");
    }

    private void CreateNewLevel()
    {
        string folderPath = "Assets/Resources/Levels";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");
        }

        // Create the level asset
        LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();

        string fileName = $"Level_{newLevelNumber:000}_{newLevelName.Replace(" ", "_")}.asset";
        string path = $"{folderPath}/{fileName}";

        // Make sure the path is unique
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        // Refresh the list
        RefreshLevelList();

        // Select the new level
        selectedLevel = newLevel;
        Selection.activeObject = newLevel;
        EditorGUIUtility.PingObject(newLevel);

        Debug.Log($"Created new level: {path}");
    }

    private void DeleteLevel(LevelData level)
    {
        if (level == selectedLevel)
        {
            selectedLevel = null;
        }

        string path = AssetDatabase.GetAssetPath(level);
        AssetDatabase.DeleteAsset(path);

        RefreshLevelList();

        Debug.Log($"Deleted level: {level.LevelName}");
    }

    private void DuplicateLevel(LevelData level)
    {
        string originalPath = AssetDatabase.GetAssetPath(level);
        string directory = System.IO.Path.GetDirectoryName(originalPath);
        string fileName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
        string extension = System.IO.Path.GetExtension(originalPath);

        string newPath = $"{directory}/{fileName}_Copy{extension}";
        newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

        AssetDatabase.CopyAsset(originalPath, newPath);
        AssetDatabase.SaveAssets();

        RefreshLevelList();

        LevelData duplicatedLevel = AssetDatabase.LoadAssetAtPath<LevelData>(newPath);
        selectedLevel = duplicatedLevel;
        Selection.activeObject = duplicatedLevel;

        Debug.Log($"Duplicated level: {newPath}");
    }

    private void TestLevel(LevelData level)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Not Playing",
                "Enter Play Mode to test the level.",
                "OK");
            return;
        }

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            // This would require adding a method to LevelManager to load a specific LevelData
            Debug.Log($"Testing level: {level.LevelName}");
            EditorUtility.DisplayDialog("Test Level",
                $"Level testing feature coming soon!\n\nLevel: {level.LevelName}\nYou can manually assign this level to the LevelManager for now.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No LevelManager",
                "Could not find LevelManager in the scene.",
                "OK");
        }
    }
}
