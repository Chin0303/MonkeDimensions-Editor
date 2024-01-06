using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Security.AccessControl;
using Unity.VisualScripting.YamlDotNet.Serialization;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DimensionDescriptor))]
public class DimensionDescriptorEditor : Editor
{
    SerializedProperty dimensionName;
    SerializedProperty dimensionAuthor;
    SerializedProperty dimensionDescription;

    SerializedProperty playerSpawnPosition;
    SerializedProperty terminalSpawnPosition;

    void OnEnable()

    {
        dimensionName = serializedObject.FindProperty("Name");
        dimensionAuthor = serializedObject.FindProperty("Author");
        dimensionDescription = serializedObject.FindProperty("Description");
        playerSpawnPosition = serializedObject.FindProperty("SpawnPosition");
        terminalSpawnPosition = serializedObject.FindProperty("TerminalPosition");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(dimensionName);
        EditorGUILayout.PropertyField(dimensionAuthor);
        EditorGUILayout.PropertyField(dimensionDescription);
        EditorGUILayout.PropertyField(playerSpawnPosition);
        EditorGUILayout.PropertyField(terminalSpawnPosition);
        if (GUILayout.Button("Export Dimension"))
            ExportAssetBundleWithJSON();
        serializedObject.ApplyModifiedProperties();
    }

    private void ExportAssetBundleWithJSON()
    {
        GameObject selectedObject = FindObjectOfType<DimensionDescriptor>().gameObject;

        if (selectedObject == null)
        {
            Debug.LogError("No dimensions found! Make sure the Descriptor is in the scene!");
            return;
        }

        DimensionDescriptor descriptor = selectedObject.GetComponent<DimensionDescriptor>();

        MeshRenderer meshRendererTerminal = descriptor.TerminalPosition.GetComponent<MeshRenderer>();
        MeshRenderer meshRendererMonke = descriptor.SpawnPosition.GetComponent<MeshRenderer>();
        DestroyImmediate(meshRendererTerminal);
        DestroyImmediate(meshRendererMonke);

        string tempPrefabDirectory = "Assets/IGNORE/";
        if (!Directory.Exists(tempPrefabDirectory))
        {
            Directory.CreateDirectory(tempPrefabDirectory);
        }
        // idk a different way to do it :P
        string tempPrefabPath = $"{tempPrefabDirectory}{descriptor.Name}.prefab";
        GameObject tempPrefab = PrefabUtility.SaveAsPrefabAsset(selectedObject, tempPrefabPath);

        string assetBundleName = descriptor.Name.ToLower();
        AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tempPrefab)).SetAssetBundleNameAndVariant(assetBundleName, "");

        BuildPipeline.BuildAssetBundles("Assets/IGNORE", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        string jsonContent = $"{{\"Name\":\"{descriptor.Name}\",\"Author\":\"{descriptor.Author}\",\"Description\":\"{descriptor.Description}\",\"SpawnPoint\":\"{descriptor.SpawnPosition.name}\",\"TerminalPoint\":\"{descriptor.TerminalPosition.name}\"}}";
        File.WriteAllText("Assets/IGNORE/Package.json", jsonContent);

        string diemnsionName = $"{assetBundleName}.dimension";
        string dimesnionPath = EditorUtility.SaveFilePanel("Export Dimension", "", diemnsionName, "dimension");

        string dimensioExportDirectory = Path.GetDirectoryName(dimesnionPath);
        if (!Directory.Exists(dimensioExportDirectory))
        {
            Directory.CreateDirectory(dimensioExportDirectory);
        }

        using (ZipArchive zipArchive = ZipFile.Open(dimesnionPath, ZipArchiveMode.Create))
        {
            zipArchive.CreateEntryFromFile($"Assets/IGNORE/{assetBundleName}", assetBundleName);
            zipArchive.CreateEntryFromFile("Assets/IGNORE/Package.json", "Package.json");
        }

        // clean up everything
        Directory.Delete("Assets/IGNORE", true);
        AssetDatabase.DeleteAsset(tempPrefabPath);
        AssetDatabase.Refresh();
    }
}
