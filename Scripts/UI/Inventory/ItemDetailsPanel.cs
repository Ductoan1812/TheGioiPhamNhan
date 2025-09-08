using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;

/// Panel hiển thị thông tin item khi click SlotItem.
public class ItemDetailsPanel : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    [Header("Actions")]
    public Button useButton;
    public Button splitButton;
    public TMP_InputField splitQuantityInput;
    [Header("Nguồn dữ liệu")]
    public ItemDatabaseSO itemDB; // nếu null sẽ dùng ItemDatabaseSO.Instance
    

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public async void Show(InventoryItem item)
    {
        if (item == null || item.quantity <= 0)
        {
            Hide();
            return;
        }

    _current = item;
    WireButtons();

        if (icon != null)
        {
            var sp = await Xianxia.Items.ItemAssets.LoadIconSpriteAsync(item.addressIcon);
            icon.sprite = sp;
            icon.enabled = sp != null;
        }
        if (nameText != null) nameText.text = item.name ?? item.id;
        if (descText != null) descText.text = BuildDesc(item);

        gameObject.SetActive(true);
    }

    private InventoryItem _current;
    private bool _wired;
    private void WireButtons()
    {
        if (_wired) return;
        if (useButton != null) useButton.onClick.AddListener(OnClickUse);
        if (splitButton != null) splitButton.onClick.AddListener(OnClickSplit);
        _wired = true;
    }

    // Tạo mô tả: cộng gộp chỉ số từ DB và từ InventoryItem (coi giá trị trong InventoryItem là delta)
    private string BuildDesc(InventoryItem inv)
    {
        var db = itemDB != null ? itemDB : ItemDatabaseSO.Instance;
        var def = db != null ? db.GetById(inv.id) : null;

        var sb = new StringBuilder();
        const string deltaColor = "#00FF66"; // xanh lá cho phần delta

        // 1) Thông tin chung (trừ ID/Name)
    if (def != null)
        {
            AppendLine(sb, "Loại", def.category.ToString());
            AppendLine(sb, "Phẩm chất", def.rarity.ToString());
            AppendLine(sb, "Ngũ hành", def.element.ToString());
            AppendLine(sb, "Cảnh giới yêu cầu", def.realmRequirement.ToString());
            AppendLine(sb, "Ràng buộc", def.bindType.ToString());
            AppendInt(sb, "Cấp", def.level, inv.level, deltaColor);
            // Stack & số lượng
            if (def.maxStack > 0)
                AppendLine(sb, "Stack tối đa", def.maxStack.ToString());
            if (inv.quantity > 0)
        AppendLine(sb, "Số lượng", inv.quantity.ToString());

            // Base stats chính
        AppendStat(sb, "Tấn công", def.baseStats.atk, inv.baseStats.atk, deltaColor);
        AppendStat(sb, "Phòng thủ", def.baseStats.defense, inv.baseStats.defense, deltaColor);
        AppendStat(sb, "Sinh lực", def.baseStats.hp, inv.baseStats.hp, deltaColor);
        AppendStat(sb, "Khí", def.baseStats.qi, inv.baseStats.qi, deltaColor);
        AppendStat(sb, "Tốc độ di chuyển", def.baseStats.moveSpd, inv.baseStats.moveSpd, deltaColor);
        AppendPercentStat(sb, "Tỉ lệ chí mạng", def.baseStats.critRate, inv.baseStats.critRate, deltaColor);
        AppendPercentStat(sb, "Sát thương chí mạng", def.baseStats.critDmg, inv.baseStats.critDmg, deltaColor);
        AppendStat(sb, "Xuyên giáp", def.baseStats.penetration, inv.baseStats.penetration, deltaColor);
        AppendStat(sb, "Hút khí", def.baseStats.lifestealQi, inv.baseStats.lifestealQi, deltaColor);

            // Kháng (resist)
            if (def.baseStats.res != null && inv.baseStats.res != null)
            {
                AppendStat(sb, "Kháng Kim", def.baseStats.res.kim, inv.baseStats.res.kim, deltaColor);
                AppendStat(sb, "Kháng Mộc", def.baseStats.res.moc, inv.baseStats.res.moc, deltaColor);
                AppendStat(sb, "Kháng Thủy", def.baseStats.res.thuy, inv.baseStats.res.thuy, deltaColor);
                AppendStat(sb, "Kháng Hỏa", def.baseStats.res.hoa, inv.baseStats.res.hoa, deltaColor);
                AppendStat(sb, "Kháng Thổ", def.baseStats.res.tho, inv.baseStats.res.tho, deltaColor);
                AppendStat(sb, "Kháng Lôi", def.baseStats.res.loi, inv.baseStats.res.loi, deltaColor);
                AppendStat(sb, "Kháng Âm", def.baseStats.res.am, inv.baseStats.res.am, deltaColor);
                AppendStat(sb, "Kháng Dương", def.baseStats.res.duong, inv.baseStats.res.duong, deltaColor);
            }

            // Sockets
            AppendInt(sb, "Sockets", def.sockets, inv.sockets, deltaColor);

            // Phụ tố/Affix
            if (def.affixes != null && def.affixes.Length > 0)
                AppendLine(sb, "Phụ tố", FormatAffixes(def.affixes));
            if (inv.affixes != null && inv.affixes.Length > 0)
                AppendLine(sb, "Phụ tố (bổ sung)", FormatAffixes(inv.affixes));

            // Hiệu ứng dùng
            if (def.useEffect != null && !string.IsNullOrEmpty(def.useEffect.type))
            {
                var ue = def.useEffect;
                var ueText = new StringBuilder()
                    .Append(ue.type);
                if (ue.magnitude != 0) ueText.Append($", magnitude: {FormatNumber(ue.magnitude)}");
                if (ue.duration != 0) ueText.Append($", duration: {FormatNumber(ue.duration)}s");
                if (!string.IsNullOrEmpty(ue.spellId)) ueText.Append($", spell: {ue.spellId}");
                AppendLine(sb, "Hiệu ứng dùng", ueText.ToString());
            }

            // Tài nguyên (địa chỉ Addressables)
            if (!string.IsNullOrEmpty(def.addressIcon))
                AppendLine(sb, "Icon", def.addressIcon);
            if (!string.IsNullOrEmpty(def.addressTexture))
                AppendLine(sb, "Texture", def.addressTexture);
        }
        else
        {
            // Không có DB -> hiển thị thẳng theo InventoryItem như total, và (delta=0)
            AppendStatOnly(sb, "Tấn công", inv.baseStats.atk);
            AppendStatOnly(sb, "Phòng thủ", inv.baseStats.defense);
            AppendStatOnly(sb, "Sinh lực", inv.baseStats.hp);
            AppendStatOnly(sb, "Khí", inv.baseStats.qi);
            AppendStatOnly(sb, "Tốc độ di chuyển", inv.baseStats.moveSpd);
            AppendPercentOnly(sb, "Tỉ lệ chí mạng", inv.baseStats.critRate);
            AppendPercentOnly(sb, "Sát thương chí mạng", inv.baseStats.critDmg);
            AppendStatOnly(sb, "Xuyên giáp", inv.baseStats.penetration);
            AppendStatOnly(sb, "Hút khí", inv.baseStats.lifestealQi);
            AppendLine(sb, "Sockets", inv.sockets.ToString());
            if (inv.affixes != null && inv.affixes.Length > 0)
                AppendLine(sb, "Phụ tố", FormatAffixes(inv.affixes));
        }

        // Thêm flavor/desc ở cuối nếu có
        if (!string.IsNullOrEmpty(inv.flavor))
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(inv.flavor);
        }

        return sb.ToString();
    }

    private void OnClickUse()
    {
        if (_current == null) return;
        var inv = FindFirstObjectByType<PlayerInventory>();
        if (inv == null) return;
        // Mặc định dùng 1 đơn vị
        inv.UseItem(_current, 1);
        // Làm mới chi tiết và inventory
        RefreshAfterAction(inv);
    }

    private void OnClickSplit()
    {
        if (_current == null) return;
        int qty = 0;
        if (splitQuantityInput != null)
        {
            int.TryParse(splitQuantityInput.text, out qty);
        }
        qty = Mathf.Clamp(qty, 1, _current.quantity - 1);
        if (qty <= 0) return; // không thể tách nếu không đủ
        var inv = FindFirstObjectByType<PlayerInventory>();
        if (inv == null) return;
        inv.SplitStack(_current, qty);
        RefreshAfterAction(inv);
    }

    private void RefreshAfterAction(PlayerInventory inv)
    {
        var ui = FindFirstObjectByType<InventoryUIManager>();
        ui?.RefreshFromCurrentData();
        // Làm mới lại thông tin cho item (có thể đã thay đổi số lượng)
        if (_current != null && _current.quantity > 0) Show(_current); else Hide();
    }

    private static void AppendLine(StringBuilder sb, string label, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(label).Append(": ").Append(value);
    }

    private static void AppendStat(StringBuilder sb, string label, float baseVal, float delta, string deltaColor)
    {
        // Ẩn dòng nếu tổng = 0 để gọn hơn
        float total = baseVal + delta;
        if (Mathf.Approximately(total, 0f)) return;
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(label).Append(": ").Append(FormatNumber(total));
        if (!Mathf.Approximately(delta, 0f))
        {
            sb.Append(' ').Append('<').Append("color=").Append(deltaColor).Append('>')
              .Append('(').Append(FormatSigned(delta)).Append(')').Append("</color>");
        }
    }

    private static void AppendInt(StringBuilder sb, string label, int baseVal, int deltaVal, string deltaColor)
    {
        int delta = deltaVal - baseVal;
        int total = baseVal + delta;
        if (total == 0) return; // ẩn nếu tổng = 0
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(label).Append(": ").Append(total);
        if (delta != 0)
        {
            sb.Append(' ').Append('<').Append("color=").Append(deltaColor).Append('>')
              .Append('(').Append(delta > 0 ? "+" : "").Append(delta).Append(')')
              .Append("</color>");
        }
    }

    private static void AppendStatOnly(StringBuilder sb, string label, float value)
    {
        if (Mathf.Approximately(value, 0f)) return;
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(label).Append(": ").Append(FormatNumber(value));
    }

    // Hiển thị phần trăm (x100) và ẩn nếu tổng = 0
    private static void AppendPercentStat(StringBuilder sb, string label, float baseVal, float delta, string deltaColor)
    {
        float total = baseVal + delta;
        if (Mathf.Approximately(total, 0f)) return;
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(label).Append(": ").Append(FormatPercent(total));
        if (!Mathf.Approximately(delta, 0f))
        {
            sb.Append(' ').Append('<').Append("color=").Append(deltaColor).Append('>')
              .Append('(').Append(FormatPercentSigned(delta)).Append(')').Append("</color>");
        }
    }

    private static void AppendPercentOnly(StringBuilder sb, string label, float value)
    {
        if (Mathf.Approximately(value, 0f)) return;
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(label).Append(": ").Append(FormatPercent(value));
    }

    private static string FormatNumber(float v)
    {
        return v % 1f == 0f ? ((int)v).ToString() : v.ToString("0.##");
    }

    private static string FormatSigned(float v)
    {
        string s = v % 1f == 0f ? ((int)v).ToString() : v.ToString("0.##");
        if (v > 0) return "+" + s;
        return s; // đã có dấu '-' nếu âm
    }

    private static string FormatPercent(float v)
    {
        float pct = v * 100f;
        return (pct % 1f == 0f ? ((int)pct).ToString() : pct.ToString("0.##")) + "%";
    }

    private static string FormatPercentSigned(float v)
    {
        float pct = v * 100f;
        string s = pct % 1f == 0f ? ((int)pct).ToString() : pct.ToString("0.##");
        if (pct > 0) return "+" + s + "%";
        return s + "%";
    }

    private static string FormatAffixes(AffixEntry[] arr)
    {
        if (arr == null || arr.Length == 0) return string.Empty;
        var sb = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
        {
            var a = arr[i];
            if (a == null) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append('[').Append(a.id).Append(':').Append(' ').Append(FormatNumber(a.value))
              .Append(" (tier ").Append(a.tier).Append(")]");
        }
        return sb.ToString();
    }
}
