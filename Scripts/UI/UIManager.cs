using Unity.AppUI.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
#endif
// using System.Linq; // not needed with event-driven toggles

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject controllerPanel;  // UI gốc (máu, mana…)
    public GameObject menuPanel;        // Panel chứa các tab menu
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    public GameObject infoItemPanel;
    public GameObject infoPlayerPanel;
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
    [Header("toggle")]
    public ToggleButtonGroup toggleButtonGroup; // Không dùng API runtime; dùng sự kiện UI gọi OnToggleSelected

    [System.Serializable]
    public class ToggleKeyPanel
    {
        [Tooltip("Khóa định danh do Toggle truyền vào (ví dụ: 'Skills', 'Quests', 'Map', ...)")]
        public string key;
        public GameObject panel;
    }

    [Header("Toggle-Panel Mapping (by key)")]
    [Tooltip("Ánh xạ các Toggle khác (ngoài Inventory/Equipment/InfoItem) tới Panel tương ứng. Gọi OnToggleSelected(key) từ Toggle.")]
    public ToggleKeyPanel[] otherTogglePanels;


    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        ShowController();
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

        // Các khóa xử lý riêng
        if (key.Equals("Inventory", System.StringComparison.OrdinalIgnoreCase)
            || key.Equals("Equipment", System.StringComparison.OrdinalIgnoreCase))
        {
            ShowInventoryAndEquipment();
            return;
        }
        if (key.Equals("InfoItem", System.StringComparison.OrdinalIgnoreCase))
        {
            ShowInventoryAndInfoItem();
            return;
        }

        // Các khóa khác sử dụng ánh xạ otherTogglePanels
        controllerPanel.SetActive(false);
        menuPanel.SetActive(true);
        if (openButton != null) openButton.SetActive(false);
        if (exitButton != null) exitButton.SetActive(true);

        HideAllMenuPanels();

        if (otherTogglePanels != null)
        {
            foreach (var b in otherTogglePanels)
            {
                if (b == null || b.panel == null) continue;
                if (!string.IsNullOrEmpty(b.key) && b.key.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                {
                    b.panel.SetActive(true);
                    break;
                }
            }
        }
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

        // Đồng thời tắt các panel map qua otherTogglePanels nếu có
        if (otherTogglePanels != null)
        {
            foreach (var b in otherTogglePanels)
            {
                if (b != null && b.panel != null) b.panel.SetActive(false);
            }
        }
    }
}
