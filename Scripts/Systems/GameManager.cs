    using UnityEngine;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public PlayerManager currentPlayer;

    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Application.persistentDataPath + "/save.json";
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayer(PlayerManager players)
    {
        currentPlayer = players;
    }

    public void SaveGame()
    {
        if (currentPlayer == null) return;

    // Save playerID as string for easier management
    SaveData data = new SaveData(currentPlayer.playerID, currentPlayer.inventory);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game Saved: " + savePath);
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // Apply lại cho player
            if (currentPlayer != null)
            {
                // SaveData stores string -> assign directly
                currentPlayer.playerID = data.playerID;
                currentPlayer.inventory = data.inventory;
            }

            Debug.Log("Game Loaded: " + savePath);
        }
        else
        {
            Debug.LogWarning("No save file found!");
        }
    }
}
