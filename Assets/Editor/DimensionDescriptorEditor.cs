using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using Monke_Dimensions.Models;
using Monke_Dimensions.Behaviours.Addons;

[CustomEditor(typeof(DimensionDescriptor))]
public class DimensionDescriptorEditor : Editor
{
    SerializedProperty dimensionName, dimensionAuthor, dimensionDescription, playerSpawnPosition, terminalSpawnPosition, dimensionImage;

    void OnEnable()
    {
        #region Setup SerializedProperties
        dimensionName = serializedObject.FindProperty("Name");
        dimensionAuthor = serializedObject.FindProperty("Author");
        dimensionDescription = serializedObject.FindProperty("Description");
        playerSpawnPosition = serializedObject.FindProperty("SpawnPosition");
        terminalSpawnPosition = serializedObject.FindProperty("TerminalPosition");
        dimensionImage = serializedObject.FindProperty("Photo");
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
        EditorGUILayout.PropertyField(dimensionImage);

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

        foreach (TeleportPlayer i in GameObject.FindObjectsOfType<TeleportPlayer>())
        {
            if (i.TeleportDestination.GetComponent<MeshRenderer>() != null)
            {
                MeshRenderer meshRenderer = i.TeleportDestination.GetComponent<MeshRenderer>();
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

        var dimensionDescriptor = new
        {
            descriptor.Name,
            descriptor.Author,
            descriptor.Description,
        };

        string jsonContent = JsonConvert.SerializeObject(dimensionDescriptor, Formatting.Indented);
        File.WriteAllText(Path.Combine(tempPrefabDirectory, "Package.json"), jsonContent);
        #endregion

        #region Texture2D Export
        // shout out to my friend Chad(gpt)
        Texture2D texture = descriptor.Photo;

        string texturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        textureImporter.isReadable = true;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        AssetDatabase.ImportAsset(texturePath);

        Texture2D decompressedTexture = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 1);

        Graphics.CopyTexture(texture, decompressedTexture);

        byte[] textureBytes = decompressedTexture.EncodeToPNG();
        string textureFileName = Path.Combine(tempPrefabDirectory, $"{UnityEngine.Random.Range(10000000, 99999999)}.png");
        File.WriteAllBytes(textureFileName, textureBytes);
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
            zipArchive.CreateEntryFromFile(textureFileName, Path.GetFileName(textureFileName));
        }

        Directory.Delete(tempPrefabDirectory, true);
        AssetDatabase.Refresh();
        #endregion
    }
}