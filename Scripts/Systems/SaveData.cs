[System.Serializable]
public class SaveData
{
    public string playerID;
    public Inventory inventory;

    public SaveData(string id, Inventory inv)
    {
        playerID = id;
        inventory = inv;
    }
}
