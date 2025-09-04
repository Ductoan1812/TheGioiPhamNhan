using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public string playerID;
    public int slotInventory;
    public Inventory inventory = new Inventory();

    private void Start()
    {
        slotInventory = 20;
        GameManager.Instance.RegisterPlayer(this);
    }
    public Inventory GetInventory(string ID)
    {
        if (ID == this.playerID.ToString())
        {
            return inventory;
        }
        Debug.LogWarning("Player ID không phù hợp");
        return null;
    }
}
