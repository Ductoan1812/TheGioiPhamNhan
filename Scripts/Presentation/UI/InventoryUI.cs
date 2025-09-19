using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using GameSystems.Inventory;
using Foundation.Events;

namespace Presentation.UI
{
    /// <summary>
    /// Inventory UI component
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private TextMeshProUGUI inventoryTitle;
        [SerializeField] private Button closeButton;

        [Header("Item Detail Panel")]
        [SerializeField] private GameObject itemDetailPanel;
        [SerializeField] private Image itemDetailIcon;
        [SerializeField] private TextMeshProUGUI itemDetailName;
        [SerializeField] private TextMeshProUGUI itemDetailDescription;
        [SerializeField] private Button useItemButton;
        [SerializeField] private Button dropItemButton;

        // State
        private readonly List<InventorySlotUI> slotUIs = new();
        private Inventory currentInventory;
        private ItemStack selectedItemStack;

        public void Initialize()
        {
            // Setup close button
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventory);
            }

            // Setup item detail buttons
            if (useItemButton != null)
            {
                useItemButton.onClick.AddListener(UseSelectedItem);
            }

            if (dropItemButton != null)
            {
                dropItemButton.onClick.AddListener(DropSelectedItem);
            }

            // Subscribe to events
            EventBus.Subscribe<Entities.Player.PlayerInventoryChangedEvent>(OnInventoryChanged);

            // Hide detail panel initially
            if (itemDetailPanel != null)
            {
                itemDetailPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<Entities.Player.PlayerInventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnInventoryChanged(Entities.Player.PlayerInventoryChangedEvent inventoryEvent)
        {
            RefreshInventory(inventoryEvent.Data);
        }

        /// <summary>
        /// Refresh inventory display
        /// </summary>
        public void RefreshInventory(Inventory inventory)
        {
            currentInventory = inventory;
            
            if (inventory == null) return;

            // Update title
            if (inventoryTitle != null)
            {
                inventoryTitle.text = $"Inventory ({inventory.UsedSlots}/{inventory.Capacity})";
            }

            // Create or update slot UIs
            EnsureSlotCount(inventory.Capacity);

            // Update each slot
            for (int i = 0; i < inventory.Capacity; i++)
            {
                var itemStack = inventory.GetSlot(i);
                if (i < slotUIs.Count)
                {
                    slotUIs[i].UpdateSlot(itemStack, i);
                }
            }
        }

        private void EnsureSlotCount(int requiredSlots)
        {
            // Remove excess slots
            while (slotUIs.Count > requiredSlots)
            {
                var lastSlot = slotUIs[slotUIs.Count - 1];
                slotUIs.RemoveAt(slotUIs.Count - 1);
                
                if (lastSlot != null && lastSlot.gameObject != null)
                {
                    Destroy(lastSlot.gameObject);
                }
            }

            // Add missing slots
            while (slotUIs.Count < requiredSlots)
            {
                CreateSlotUI(slotUIs.Count);
            }
        }

        private void CreateSlotUI(int slotIndex)
        {
            if (itemSlotPrefab == null || itemContainer == null) return;

            var slotObj = Instantiate(itemSlotPrefab, itemContainer);
            var slotUI = slotObj.GetComponent<InventorySlotUI>();
            
            if (slotUI == null)
            {
                slotUI = slotObj.AddComponent<InventorySlotUI>();
            }

            slotUI.Initialize(slotIndex, OnSlotClicked);
            slotUIs.Add(slotUI);
        }

        private void OnSlotClicked(int slotIndex, ItemStack itemStack)
        {
            selectedItemStack = itemStack;
            ShowItemDetail(itemStack);
        }

        private void ShowItemDetail(ItemStack itemStack)
        {
            if (itemDetailPanel == null) return;

            if (itemStack == null || itemStack.IsEmpty)
            {
                itemDetailPanel.SetActive(false);
                return;
            }

            itemDetailPanel.SetActive(true);

            var item = itemStack.Item;

            // Update item detail UI
            if (itemDetailIcon != null)
            {
                itemDetailIcon.sprite = item.Icon;
                itemDetailIcon.gameObject.SetActive(item.Icon != null);
            }

            if (itemDetailName != null)
            {
                var nameText = item.DisplayName;
                if (itemStack.Quantity > 1)
                {
                    nameText += $" x{itemStack.Quantity}";
                }
                itemDetailName.text = nameText;
            }

            if (itemDetailDescription != null)
            {
                itemDetailDescription.text = item.Description;
            }

            // Update buttons
            if (useItemButton != null)
            {
                useItemButton.gameObject.SetActive(item.IsConsumable);
                useItemButton.interactable = item.IsConsumable;
            }

            if (dropItemButton != null)
            {
                dropItemButton.interactable = true;
            }
        }

        private void UseSelectedItem()
        {
            if (selectedItemStack?.Item != null)
            {
                EventBus.Publish(new UseItemRequestEvent(selectedItemStack.Item.Id));
            }
        }

        private void DropSelectedItem()
        {
            if (selectedItemStack?.Item != null)
            {
                EventBus.Publish(new DropItemRequestEvent(selectedItemStack.Item.Id, 1));
                
                // Hide detail panel after dropping
                if (itemDetailPanel != null)
                {
                    itemDetailPanel.SetActive(false);
                }
                
                selectedItemStack = null;
            }
        }

        private void CloseInventory()
        {
            EventBus.Publish(new Entities.Player.InventoryUIToggleEvent());
        }

        /// <summary>
        /// Show/hide inventory
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            
            if (!visible)
            {
                // Hide detail panel when closing inventory
                if (itemDetailPanel != null)
                {
                    itemDetailPanel.SetActive(false);
                }
                selectedItemStack = null;
            }
        }
    }

    /// <summary>
    /// Individual inventory slot UI
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button slotButton;
        [SerializeField] private Image backgroundImage;

        [Header("Visual Settings")]
        [SerializeField] private Color emptySlotColor = Color.gray;
        [SerializeField] private Color filledSlotColor = Color.white;
        [SerializeField] private Color selectedSlotColor = Color.yellow;

        // State
        private int slotIndex;
        private ItemStack currentItemStack;
        private System.Action<int, ItemStack> onSlotClicked;
        private bool isSelected;

        public void Initialize(int index, System.Action<int, ItemStack> clickCallback)
        {
            slotIndex = index;
            onSlotClicked = clickCallback;

            if (slotButton == null) slotButton = GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(OnClick);
            }

            UpdateVisuals();
        }

        public void UpdateSlot(ItemStack itemStack, int index)
        {
            slotIndex = index;
            currentItemStack = itemStack;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            var isEmpty = currentItemStack?.IsEmpty ?? true;
            var item = currentItemStack?.Item;

            // Update icon
            if (itemIcon != null)
            {
                itemIcon.sprite = item?.Icon;
                itemIcon.gameObject.SetActive(!isEmpty && item?.Icon != null);
            }

            // Update quantity text
            if (quantityText != null)
            {
                if (isEmpty || currentItemStack.Quantity <= 1)
                {
                    quantityText.text = "";
                }
                else
                {
                    quantityText.text = currentItemStack.Quantity.ToString();
                }
            }

            // Update background color
            if (backgroundImage != null)
            {
                var targetColor = isEmpty ? emptySlotColor : filledSlotColor;
                if (isSelected) targetColor = selectedSlotColor;
                backgroundImage.color = targetColor;
            }
        }

        private void OnClick()
        {
            onSlotClicked?.Invoke(slotIndex, currentItemStack);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisuals();
        }
    }

    /// <summary>
    /// UI request events
    /// </summary>
    public class UseItemRequestEvent : GameEvent<string>
    {
        public string ItemId => Data;

        public UseItemRequestEvent(string itemId) : base(itemId)
        {
        }
    }

    public class DropItemRequestEvent : GameEvent<DropItemData>
    {
        public string ItemId => Data.ItemId;
        public int Quantity => Data.Quantity;

        public DropItemRequestEvent(string itemId, int quantity) 
            : base(new DropItemData(itemId, quantity))
        {
        }
    }

    [System.Serializable]
    public class DropItemData
    {
        public string ItemId { get; }
        public int Quantity { get; }

        public DropItemData(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}
