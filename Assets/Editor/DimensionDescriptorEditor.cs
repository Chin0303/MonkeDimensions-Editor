using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System;

[CustomEditor(typeof(DimensionDescriptor))]
public class DimensionDescriptorEditor : Editor
{
    SerializedProperty dimensionName, dimensionAuthor, dimensionDescription, playerSpawnPosition, terminalSpawnPosition, slipperyObjects, extraTerminals;

    void OnEnable()
    {
        #region Setup SerializedProperties
        dimensionName = serializedObject.FindProperty("Name");
        dimensionAuthor = serializedObject.FindProperty("Author");
        dimensionDescription = serializedObject.FindProperty("Description");
        playerSpawnPosition = serializedObject.FindProperty("SpawnPosition");
        terminalSpawnPosition = serializedObject.FindProperty("TerminalPosition");
        slipperyObjects = serializedObject.FindProperty("Addons.SlipperyObjects");
        extraTerminals = serializedObject.FindProperty("Addons.ExtraTerminals");
        #endregion
    }

    public override void OnInspectorGUI()
    {
        #region Not Sure What To Name This
        serializedObject.Update();

        EditorGUILayout.PropertyField(dimensionName);
        EditorGUILayout.PropertyField(dimensionAuthor);
        EditorGUILayout.PropertyField(dimensionDescription);
        EditorGUILayout.PropertyField(playerSpawnPosition);
        EditorGUILayout.PropertyField(terminalSpawnPosition);

        EditorGUILayout.PropertyField(slipperyObjects);
        EditorGUILayout.PropertyField(extraTerminals);

        if (GUILayout.Button("Export Dimension"))
            ExportDimension();

        serializedObject.ApplyModifiedProperties();
        #endregion
    }

    private void ExportDimension()
    {
        #region Prepare
        GameObject selectedObject = FindObjectOfType<DimensionDescriptor>().gameObject;

        DimensionDescriptor descriptor = selectedObject.GetComponent<DimensionDescriptor>();
        MeshRenderer meshRendererTerminal = descriptor.TerminalPosition.GetComponent<MeshRenderer>();
        MeshRenderer meshRendererMonke = descriptor.SpawnPosition.GetComponent<MeshRenderer>();
        DestroyImmediate(meshRendererTerminal);
        DestroyImmediate(meshRendererMonke);

        foreach (GameObject gameObject in descriptor.Addons.ExtraTerminals)
        {
            if (gameObject.GetComponent<MeshRenderer>() != null)
            {
                MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                DestroyImmediate(meshRenderer);
            }
        }
        #endregion

        #region Setup Directory/Paths
        string tempPrefabDirectory = Path.Combine(Path.GetTempPath(), "DimensionExport");
        if (!Directory.Exists(tempPrefabDirectory))
        {
            Directory.CreateDirectory(tempPrefabDirectory);
        }

        string tempPrefabPathInAssets = $"Assets/{descriptor.Name}.prefab";
        GameObject tempPrefab = PrefabUtility.SaveAsPrefabAsset(selectedObject, tempPrefabPathInAssets);
        string assetBundleName = descriptor.Name.ToLower();
        AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tempPrefab)).SetAssetBundleNameAndVariant(assetBundleName, "");
        BuildPipeline.BuildAssetBundles(tempPrefabDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        AssetDatabase.DeleteAsset(tempPrefabPathInAssets);
        #endregion

        #region JSON Stuff
        var triggerEventsData = new List<object>();
        foreach (TriggerEvents.Events eventType in Enum.GetValues(typeof(TriggerEvents.Events)))
        {
            string eventTypeString = eventType.ToString();
            List<object> triggerEventObjects = new List<object>();

            foreach (TriggerEvents triggerEvents in selectedObject.GetComponentsInChildren<TriggerEvents>())
            {
                if (triggerEvents.selectedEvent == eventType && triggerEvents.associatedComponent != null && triggerEvents.associatedComponent.gameObjectField != null)
                {
                    GameObject triggerObject = triggerEvents.gameObject;
                    GameObject associatedObject = triggerEvents.associatedComponent.gameObjectField;
                    string triggerObjectName = triggerObject.name;
                    string triggerEventObjectName = associatedObject.name;

                    if (eventType == TriggerEvents.Events.TeleportPlayer)
                    {
                        var goMesh = GameObject.Find(triggerEventObjectName).GetComponent<MeshRenderer>();
                        if (goMesh != null) DestroyImmediate(goMesh);
                    }
                    triggerEventObjects.Add(new
                    {
                        TriggerObjectName = triggerObjectName,
                        TriggerEventObjectName = triggerEventObjectName
                    });
                }
            }

            triggerEventsData.Add(new
            {
                EventType = eventTypeString,
                TriggerEvent = triggerEventObjects
            });
        }

        var dimensionDescriptor = new
        {
            Name = descriptor.Name,
            Author = descriptor.Author,
            Description = descriptor.Description,
            SpawnPoint = descriptor.SpawnPosition.name,
            TerminalPoint = descriptor.TerminalPosition.name,
            Addons = new
            {
                SlipperyObjects = descriptor.Addons.SlipperyObjects.Select(go => go.name).ToList(),
                ExtraTerminals = descriptor.Addons.ExtraTerminals.Select(go => go.name).ToList(),
                TriggerEvents = triggerEventsData
            }
        };

        string jsonContent = JsonConvert.SerializeObject(dimensionDescriptor, Formatting.Indented);
        File.WriteAllText(Path.Combine(tempPrefabDirectory, "Package.json"), jsonContent);
        #endregion

        #region Create Dimension
        string dimensionName = $"{assetBundleName}.dimension";
        string dimensionPath = EditorUtility.SaveFilePanel("Export Dimension", "", dimensionName, "dimension");
        string dimensionExportDirectory = Path.GetDirectoryName(dimensionPath);
        if (!Directory.Exists(dimensionExportDirectory))
        {
            Directory.CreateDirectory(dimensionExportDirectory);
        }

        using (ZipArchive zipArchive = ZipFile.Open(dimensionPath, ZipArchiveMode.Create))
        {
            zipArchive.CreateEntryFromFile(Path.Combine(tempPrefabDirectory, assetBundleName), assetBundleName);
            zipArchive.CreateEntryFromFile(Path.Combine(tempPrefabDirectory, "Package.json"), "Package.json");
        }

        Directory.Delete(tempPrefabDirectory, true);
        AssetDatabase.Refresh();
        #endregion
    }

    public List<string> formatList(List<GameObject> list)
    {
        return list.Select(go => go.name).ToList();
    }
}