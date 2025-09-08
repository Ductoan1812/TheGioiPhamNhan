using System;
using UnityEngine;
using Xianxia.PlayerDataSystem; // để dùng PlayerData

/// <summary>
/// Quản lý thông tin player (lưu playerId, load PlayerData).
/// Các hệ thống khác có thể lấy PlayerData từ PlayerManager.Instance.
/// </summary>
[DefaultExecutionOrder(-10000)] // Chạy rất sớm
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [SerializeField] private string playerId = "User_001";
    private PlayerData currentPlayerData;

    public event Action<PlayerData> OnPlayerDataLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadPlayer(playerId);
    }


    public string PlayerId => playerId;


    public PlayerData Data => currentPlayerData;


    public void LoadPlayer(string newId)
    {
        if (string.IsNullOrWhiteSpace(newId))
        {
            Debug.LogWarning("[PlayerManager] newId is null or empty, cannot load player.");
            return;
        }

        playerId = newId;
        currentPlayerData = PlayerData.LoadForPlayer(playerId);

        if (currentPlayerData != null)
        {
            Debug.Log($"[PlayerManager] Successfully loaded data for playerId={playerId}");
            OnPlayerDataLoaded?.Invoke(currentPlayerData);
        }
        else
        {
            Debug.LogError($"[PlayerManager] Failed to load data for playerId={playerId}");
        }
    }

  
    public void SavePlayer()
    {
        if (currentPlayerData == null)
        {
            Debug.LogWarning("[PlayerManager] No player data to save.");
            return;
        }

    string path = PlayerData.GetPathForPlayer(playerId);
    currentPlayerData.SaveForPlayer(playerId, prettyPrint: true);
    Debug.Log($"[PlayerManager] Saved data for playerId={playerId} at: {path}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
