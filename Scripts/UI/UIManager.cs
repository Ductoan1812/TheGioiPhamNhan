using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject controllerPanel;  // UI gốc (máu, mana…)
    public GameObject menuPanel;        // Panel chứa các tab menu
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    public GameObject infoItemPanel;
    public GameObject settingsPanel;

    [Header("Buttons")]
    public GameObject openButton;   // Nút mở menu
    public GameObject exitButton;   // Nút thoát menu

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        ShowController();
    }

    public void SwitchState(int numberState)
    {

        inventoryPanel.SetActive(false);
        equipmentPanel.SetActive(false);
    if (infoItemPanel != null) infoItemPanel.SetActive(false);
    if (settingsPanel != null) settingsPanel.SetActive(false);

        // Bật panel tương ứng
        switch (numberState)
        {
            case 0:
                inventoryPanel.SetActive(true);
                RefreshInventoryUI();
                break;
            case 1:
                equipmentPanel.SetActive(true);
                RefreshEquipmentUI();
                break;
            case 2:
                if (settingsPanel != null) settingsPanel.SetActive(true);
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

        // Mở tab mặc định = Inventory
        SwitchState(0);
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
        inventoryPanel.SetActive(false);
        equipmentPanel.SetActive(false);
    if (infoItemPanel != null) infoItemPanel.SetActive(false);
    if (settingsPanel != null) settingsPanel.SetActive(false);

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
}
