using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnEntry
    {
        public string itemId;
        public int quantity;
        public Vector3 position;
    }

    [Header("Prefab có ItemPrefab")]
    public GameObject itemPrefab;

    [Header("Spawn sẵn khi Start (tùy chọn)")]
    public SpawnEntry[] initialSpawns;

    private void Start()
    {
        if (initialSpawns != null)
        {
            foreach (var e in initialSpawns)
            {
                Spawn(e.itemId, Mathf.Max(1, e.quantity), e.position);
            }
        }
    }

    public GameObject Spawn(string itemId, int quantity, Vector3 position)
    {
        if (itemPrefab == null)
        {
            Debug.LogError("ItemSpawner: itemPrefab chưa gán.");
            return null;
        }

        var go = Instantiate(itemPrefab, position, Quaternion.identity);
        var view = go.GetComponent<ItemPrefab>();
        if (view == null)
        {
            Debug.LogError("ItemSpawner: Prefab không có ItemPrefab component.");
            return go;
        }

        view.Setup(itemId, quantity);
        return go;
    }
}