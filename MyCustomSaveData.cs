using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using CodeStage.AntiCheat.ObscuredTypes;

[Serializable]
public class MyCustomSaveDataElement
{
    public ObscuredString Key { get; set; }
    public ObscuredString Value { get; set; }
}

public static class SaveLoadManager
{
    public static void Save()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        // Custom Switch saving
        if (DDOL_Switch.instance)
        {
            DDOL_Switch.instance.Save();
        }
#endif

#if UNITY_STANDALONE || UNITY_EDITOR && !UNITY_XBOXONE && !UNITY_SWITCH
        string dataPathBaseString = Application.persistentDataPath;

        // Create Save Data in a custom directory per user for Steam
        // Check if we already have created a directory for the specific SteamID
        dataPathBaseString += "/" + Steamworks.SteamClient.SteamId.ToString();

        if (!Directory.Exists(dataPathBaseString))
        {
            Directory.CreateDirectory(dataPathBaseString); // Creates the Unique Steam User's save directory
        }

        MyCustomSaveData dataToSave = DDOL_MAIN.instance.myCustomSaveData;

        BinaryFormatter bf = new BinaryFormatter();
        MyCustomSaveData data = dataToSave;

        byte[] encryptedData;
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, data);
            encryptedData = ms.ToArray();
            ObscuredByte.Encrypt(encryptedData, 42);
            
        }

        FileStream stream = new FileStream(dataPathBaseString + "/MyCustomSaveData.dat", FileMode.Create);
        bf.Serialize(stream, encryptedData);
        stream.Close();
        
        // Update Steam Stats and Achievements after saving
        DDOL_Steam.instance.UpdateSteamStatsAndAchievements();
#endif
    }

    public static MyCustomSaveData Load()
    {
        MyCustomSaveData loadedSaveData = null;

        string dataPathBaseString = Application.persistentDataPath;

#if UNITY_STANDALONE || UNITY_EDITOR && !UNITY_XBOXONE && !UNITY_SWITCH
        // Load Save Data from custom directory per user for Steam
        dataPathBaseString += "/" + Steamworks.SteamClient.SteamId.ToString();
#endif

        if (File.Exists(dataPathBaseString + "/MyCustomSaveData.dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(dataPathBaseString + "/MyCustomSaveData.dat", FileMode.Open);

            byte[] decryptedBytes = bf.Deserialize(stream) as byte[];
            ObscuredByte.Decrypt(decryptedBytes, 42);
            
            Stream decryptedStream = new MemoryStream(decryptedBytes);

            loadedSaveData = bf.Deserialize(decryptedStream) as MyCustomSaveData;

            stream.Close();
        }

        return loadedSaveData;
    }
}

[Serializable]
public class MyCustomSaveData
{
    // REWIRED information
    public List<MyCustomSaveDataElement> myCustomSaveDataElementKeys = new List<MyCustomSaveDataElement>();

    public void SetKey(string key, string value)
    {
        bool keyFound = false;

        foreach (MyCustomSaveDataElement dataElement in myCustomSaveDataElementKeys)
        {
            if( key == dataElement.Key)
            {
                dataElement.Value = value;
                keyFound = true;
                break;
            }
        }

        if(!keyFound)
        {
            myCustomSaveDataElementKeys.Add(new MyCustomSaveDataElement() { Key = key, Value = value });
        }
    }

    public string GetKey(string key)
    {
        string returnValue = null;

        foreach (MyCustomSaveDataElement dataElement in MyCustomSaveDataElementKeys)
        {
            if (key == dataElement.Key)
            {
                returnValue = dataElement.Value;
                break;
            }
        }

        return returnValue;
    }

    public bool HasKey(string key)
    {
        bool hasKey = false;

        foreach (MyCustomSaveDataElement dataElement in myCustomSaveDataElementKeys)
        {
            if (key == dataElement.Key)
            {
                hasKey = true;
                break;
            }
        }

        return hasKey;
    }
}
