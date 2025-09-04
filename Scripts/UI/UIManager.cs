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
        settingsPanel.SetActive(false);

        // Bật panel tương ứng
        switch (numberState)
        {
            case 0:
                inventoryPanel.SetActive(true);
                break;
            case 1:
                equipmentPanel.SetActive(true);
                break;
            case 2:
                settingsPanel.SetActive(true);
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
        settingsPanel.SetActive(false);

    }
}
