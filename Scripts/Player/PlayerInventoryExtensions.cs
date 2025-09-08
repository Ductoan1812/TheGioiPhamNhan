using Xianxia.PlayerDataSystem;

public static class PlayerInventoryExtensions
{
    // Wrapper để đảm bảo có thể gọi từ UI ngay cả khi IDE/chỉ mục chưa thấy method mới
    public static bool UseItem(this PlayerInventory inv, InventoryItem item, int quantity)
    {
        if (inv == null) return false;
        return inv.UseItem(item, quantity);
    }

    public static bool SplitStack(this PlayerInventory inv, InventoryItem item, int quantity)
    {
        if (inv == null) return false;
        return inv.SplitStack(item, quantity);
    }
}