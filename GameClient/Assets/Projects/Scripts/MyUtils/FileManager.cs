using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileManager : MonoBehaviour
{
    Dictionary<Type, string> lastSaveData = new Dictionary<Type, string>();

    public void SaveData<T>(string directoryPath, string fileName, T targetObject)
    {
        var objectType = targetObject.GetType();
        var fullPath = $"{Application.persistentDataPath}/{directoryPath}/{fileName}";
        var objectJson = JsonConvert.SerializeObject(targetObject);

        if (lastSaveData.ContainsKey(objectType))
        {
            if (lastSaveData[objectType].Equals(objectJson))
            {
                Debug.Log("SaveData() Same data saved already");
                return;
            }
        }

        lastSaveData[objectType] = objectJson;

        try
        {
            Directory.CreateDirectory($"{Application.persistentDataPath}/{directoryPath}");
            File.WriteAllText(fullPath, objectJson);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public bool TryLoadData<T>(string filePath, out T loadObject)
    {
        var fullPath = $"{Application.persistentDataPath}/{filePath}";
        if (File.Exists(fullPath))
        {
            try
            {
                var fileData = File.ReadAllText(fullPath);
                loadObject = JsonConvert.DeserializeObject<T>(fileData);
                return true;
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        loadObject = default;
        return false;
    }
}
