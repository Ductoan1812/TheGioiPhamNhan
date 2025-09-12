using UnityEngine;
// Mỗi toggle đã có script riêng (MenuToggleKey). Ta chỉ cần map key -> panel ở đây.

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject controllerPanel;  // UI gốc (máu, mana…)
    public GameObject menuPanel;        // Panel chứa các tab menu
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    public GameObject infoItemPanel;
    public GameObject infoPlayerPanel;  // bổ sung các panel khác sử dụng
    public GameObject skillPanel;
    public GameObject questPanel;
    public GameObject mapPanel;
    public GameObject creatingPanel;
    public GameObject achievementPanel;
    public GameObject socialPanel;
    public GameObject petPanel;
    public GameObject settingsPanel;

    [Header("Buttons")]
    public GameObject openButton;   // Nút mở menu
    public GameObject exitButton;   // Nút thoát menu
    [System.Serializable]
    public class PanelMapping
    {
        public string key;           // Khóa giống với key trong MenuToggleKey
        public GameObject panel;     // Panel cần bật khi toggle bật
    }

    [Header("Panel Mappings (key -> panel)")]
    public PanelMapping[] panelMappings; // Không cần tham chiếu Toggle nữa

    private GameObject _currentSinglePanel; // Panel đơn hiện tại (không phải cặp inventory combo)

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        ShowController();
        // Không auto wire toggle vì mỗi toggle tự gọi OnToggleSelected qua MenuToggleKey
        // Nếu muốn panel mặc định nào đó, có thể bật toggle tương ứng trong Inspector
    }
    void Update() { }

    public void SwitchState(int numberState)
    {
        // Hàm cũ: giữ lại cho tương thích, nhưng logic hiển thị nay do Toggle điều khiển.
        // Bạn có thể gọi trực tiếp ShowInventoryAndEquipment/ShowInventoryAndInfoItem hoặc thiết lập toggle tương ứng.
        HideAllMenuPanels();
        switch (numberState)
        {
            case 0:
                ShowInventoryAndEquipment();
                break;
            case 1:
                ShowInventoryAndEquipment();
                break;
            case 2:
                if (settingsPanel != null) settingsPanel.SetActive(true);
                break;
            default:
                break;
        }
    }

    public void OpenMenu()
    {
        // Ẩn controller, ẩn openButton → hiện exitButton
        controllerPanel.SetActive(false);
        menuPanel.SetActive(true);
        openButton.SetActive(false);
        exitButton.SetActive(true);
    // Đảm bảo tắt sạch mọi panel cũ trước khi mở mặc định
    HideAllMenuPanels();
    // Mở tab mặc định = Inventory/Equipment
    ShowInventoryAndEquipment();
    RefreshInventoryUI();
    }

    public void ExitMenu()
    {
        ShowController();
    }

    private void ShowController()
    {
        // Chỉ hiển thị UI gốc và nút Open
        controllerPanel.SetActive(true);
        menuPanel.SetActive(false);
        openButton.SetActive(true);
        exitButton.SetActive(false);

    // Tắt hết menu tab
    HideAllMenuPanels();

    }

    // Hiển thị đồng thời Inventory + Equipment
    public void ShowInventoryAndEquipment()
    {
        controllerPanel.SetActive(false);
        menuPanel.SetActive(true);
        openButton.SetActive(false);
        exitButton.SetActive(true);
    // Tắt toàn bộ panel trước để tránh chồng (trường hợp mở lại menu)
    HideAllMenuPanels();
    if (inventoryPanel != null) inventoryPanel.SetActive(true);
    if (equipmentPanel != null) equipmentPanel.SetActive(true);
    if (infoItemPanel != null) infoItemPanel.SetActive(false);
    if (settingsPanel != null) settingsPanel.SetActive(false);
    // Không refresh khi đang kéo để tránh rebuild làm hỏng thao tác drag
    }

    // Hiển thị Inventory + InfoItem
    public void ShowInventoryAndInfoItem()
    {
        controllerPanel.SetActive(false);
        menuPanel.SetActive(true);
        openButton.SetActive(false);
        exitButton.SetActive(true);
    // Tắt toàn bộ panel trước để tránh chồng
    HideAllMenuPanels();
    if (inventoryPanel != null) inventoryPanel.SetActive(true);
    if (equipmentPanel != null) equipmentPanel.SetActive(false);
    if (infoItemPanel != null) infoItemPanel.SetActive(true);
    if (settingsPanel != null) settingsPanel.SetActive(false);
    // Không refresh ở đây để tránh rebuild vào lúc click/drag. Inventory đã được refresh khi mở menu hoặc đổi tab.
    }

    private void RefreshInventoryUI()
    {
        var inv = FindFirstObjectByType<InventoryUIManager>(FindObjectsInactive.Include);
        inv?.RefreshFromCurrentData();
    }

    private void RefreshEquipmentUI()
    {
        var eq = FindFirstObjectByType<EquipmentUIManager>(FindObjectsInactive.Include);
        eq?.RefreshAllSlots();
    }

    // ----------------------
    // Public API for UI Toggles
    // ----------------------

    // Gắn hàm này vào sự kiện OnValueChanged(bool) của mỗi ToggleButton.
    // Truyền tham số key khác nhau cho từng toggle.
    public void OnToggleSelected(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        // Chuẩn bị trạng thái chung khi vào một menu
        controllerPanel.SetActive(false);
        menuPanel.SetActive(true);
        if (openButton) openButton.SetActive(false);
        if (exitButton) exitButton.SetActive(true);

        // Tổ hợp đặc biệt
        if (key.Equals("Inventory", System.StringComparison.OrdinalIgnoreCase)
            || key.Equals("Equipment", System.StringComparison.OrdinalIgnoreCase))
        {
            HideAllMenuPanels();
            ShowInventoryAndEquipment();
            _currentSinglePanel = null; // đang ở chế độ combo
            return;
        }
        if (key.Equals("InfoItem", System.StringComparison.OrdinalIgnoreCase))
        {
            HideAllMenuPanels();
            ShowInventoryAndInfoItem();
            _currentSinglePanel = null;
            return;
        }
        if (key.Equals("InfoPlayer", System.StringComparison.OrdinalIgnoreCase))
        {
            HideAllMenuPanels();
            if (infoPlayerPanel) infoPlayerPanel.SetActive(true);
            _currentSinglePanel = infoPlayerPanel;
            // Gọi refresh động
            var info = infoPlayerPanel != null ? infoPlayerPanel.GetComponentInChildren<Xianxia.UI.InfoPlayer.InfoManager>(true) : null;
            info?.RefreshNow();
            return;
        }

        // Panel đơn còn lại (InfoPlayer, Skill, Quest, Map, Creating, Achievement, Social, Pet, Settings ... )
        HideAllMenuPanels();
        var panel = FindPanelByKey(key);
        if (panel != null)
        {
            panel.SetActive(true);
            _currentSinglePanel = panel;
            return;
        }

        // Không tìm thấy panel -> không làm gì (giữ trạng thái trước đó)
    }

    private GameObject FindPanelByKey(string key)
    {
        if (panelMappings == null) return null;
        for (int i = 0; i < panelMappings.Length; i++)
        {
            var pm = panelMappings[i];
            if (pm == null || pm.panel == null) continue;
            if (!string.IsNullOrEmpty(pm.key) && pm.key.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                return pm.panel;
            // Fallback: so sánh với tên panel nếu key không matches
            if (pm.panel.name.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                return pm.panel;
        }
        return null;
    }

    private void HideAllMenuPanels()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
        if (infoItemPanel != null) infoItemPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPlayerPanel != null) infoPlayerPanel.SetActive(false);
        if (skillPanel != null) skillPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (creatingPanel != null) creatingPanel.SetActive(false);
        if (achievementPanel != null) achievementPanel.SetActive(false);
        if (socialPanel != null) socialPanel.SetActive(false);
        if (petPanel != null) petPanel.SetActive(false);
    }

    // ----------------------
    // Inventory special-case external triggers
    // ----------------------

    // Gọi khi bắt đầu/ kết thúc kéo item để bật/tắt equipmentPanel nếu inventory đang mở.
    public void OnInventoryItemDragState(bool dragging)
    {
        // Chỉ tác động nếu inventory toggle đang là menu hiện hành (inventoryPanel active)
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            if (equipmentPanel != null)
                equipmentPanel.SetActive(dragging); // hiện khi dragging
        }
    }

    // Gọi khi click item để hiển thị info item panel (inventory vẫn mở)
    public void ShowInventoryItemInfoPanel()
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            if (infoItemPanel != null)
            {
                infoItemPanel.SetActive(true);
                if (equipmentPanel != null) equipmentPanel.SetActive(false); // tránh chồng chéo nếu muốn
            }
        }
    }

    public void HideInventoryItemInfoPanel()
    {
        if (infoItemPanel != null) infoItemPanel.SetActive(false);
    }

    // EnforceSingleToggle removed – rely on Unity ToggleGroup if cần độc quyền
}
