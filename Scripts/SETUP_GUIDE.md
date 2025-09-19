# 🎮 Clean Architecture Setup Guide

## 📋 Tổng quan hệ thống

Dự án này sử dụng **Clean Architecture** với các layer:

```
📁 Assets/Scripts/
├── 🏗️ Foundation/     - Core utilities, events, architecture
├── 🎯 GameSystems/    - Game logic (Stats, Inventory, Combat)
├── 👤 Entities/       - Game objects (Player, Enemy)
├── 🎨 Features/       - Game features (Quest, Crafting)
├── 🖥️ Presentation/  - UI, Audio, Visual effects
└── 🔧 Infrastructure/ - Data, Managers, Scene management
```

---

## 🚀 Cách Setup và Sử dụng

### 1. 🎯 Setup Scene chính

#### A. Tạo GameManager GameObject:
1. **Tạo Empty GameObject** tên `GameManager`
2. **Add Component**: `GameManager` (từ `Assets/Scripts/Infrastructure/Scene/GameManager.cs`)
3. **Assign các Manager** trong Inspector:
   - Audio Manager
   - Input Manager  
   - UI Manager

#### B. Tạo các Manager GameObjects:
1. **AudioManager GameObject**:
   - Add Component: `AudioManager`
   - Assign vào GameManager

2. **InputManager GameObject**:
   - Add Component: `InputManager`
   - Assign vào GameManager

3. **UIManager GameObject**:
   - Add Component: `UIManager`
   - Assign vào GameManager

---

### 2. 👤 Setup Player

#### A. Tạo Player GameObject:
1. **Tạo GameObject** tên `Player`
2. **Add Components** theo thứ tự:

```csharp
// Core Components
- PlayerController (MonoBehaviour)
- PlayerMovement (MonoBehaviour) 
- PlayerHealth (MonoBehaviour)
- PlayerInventory (MonoBehaviour)
- PlayerInput (MonoBehaviour)

// Unity Components
- Rigidbody2D (hoặc Rigidbody)
- Collider2D (hoặc Collider)
- SpriteRenderer
```

#### B. Setup PlayerController:
```csharp
[Header("Player Components")]
[SerializeField] private PlayerMovement movement;
[SerializeField] private PlayerHealth health;
[SerializeField] private PlayerInventory inventory;
[SerializeField] private PlayerInput input;

[Header("Player Settings")]
[SerializeField] private float moveSpeed = 5f;
[SerializeField] private int maxHealth = 100;
```

#### C. Auto-assign Components:
```csharp
private void Awake()
{
    // Auto-find components if not assigned
    if (movement == null) movement = GetComponent<PlayerMovement>();
    if (health == null) health = GetComponent<PlayerHealth>();
    if (inventory == null) inventory = GetComponent<PlayerInventory>();
    if (input == null) input = GetComponent<PlayerInput>();
}
```

---

### 3. 📦 Tạo ScriptableObjects

#### A. Tạo Item Data Assets:

**Consumable Items:**
1. **Right-click** trong Project → `Create` → `Item Data` → `Consumable Item Data`
2. **Setup properties**:
   ```
   Item ID: "health_potion"
   Display Name: "Health Potion"
   Description: "Restores 50 HP"
   Rarity: Common
   Gold Value: 25
   Icon: [Drag sprite]
   Heal Amount: 50
   Mana Amount: 0
   ```

**Equipment Items:**
1. **Right-click** → `Create` → `Item Data` → `Equipment Item Data`
2. **Setup properties**:
   ```
   Item ID: "iron_sword"
   Display Name: "Iron Sword"
   Description: "A sturdy iron sword"
   Rarity: Uncommon
   Gold Value: 100
   Icon: [Drag sprite]
   Item Type: Weapon
   Stat Modifiers: [Add modifiers]
   ```

**Material Items:**
1. **Right-click** → `Create` → `Item Data` → `Material Item Data`
2. **Setup properties**:
   ```
   Item ID: "iron_ore"
   Display Name: "Iron Ore"
   Description: "Raw iron ore"
   Rarity: Common
   Gold Value: 5
   Icon: [Drag sprite]
   Material Category: "Metal"
   Tier: 1
   ```

#### B. Tạo Stat Data Assets:
1. **Right-click** → `Create` → `Stat Data`
2. **Setup properties**:
   ```
   Stat Name: "Strength"
   Base Value: 10
   Min Value: 1
   Max Value: 100
   Description: "Physical power"
   ```

---

### 4. 🎮 Sử dụng trong Code

#### A. Tạo Item từ ScriptableObject:
```csharp
public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private ConsumableItemData healthPotionData;
    
    public void SpawnHealthPotion()
    {
        // Tạo item từ ScriptableObject
        var item = healthPotionData.CreateItem();
        
        // Add vào inventory
        var playerInventory = FindObjectOfType<PlayerInventory>();
        playerInventory.AddItem(item);
    }
}
```

#### B. Sử dụng Event System:
```csharp
public class HealthBarUI : MonoBehaviour
{
    private void Start()
    {
        // Subscribe to health events
        EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
    }
    
    private void OnHealthChanged(PlayerHealthChangedEvent healthEvent)
    {
        // Update health bar
        UpdateHealthBar(healthEvent.CurrentHealth, healthEvent.MaxHealth);
    }
}
```

#### C. Sử dụng Stat System:
```csharp
public class PlayerStats : MonoBehaviour
{
    private StatCollection stats;
    
    private void Start()
    {
        stats = new StatCollection();
        stats.AddStat("Strength", 10);
        stats.AddStat("Agility", 8);
        
        // Apply modifiers
        stats.AddModifier("Strength", new StatModifier(5, StatModifierType.Flat));
    }
}
```

---

### 5. 🎨 Setup UI

#### A. Tạo Canvas:
1. **Right-click** → `UI` → `Canvas`
2. **Add UIManager component**
3. **Setup UI elements**:
   - Health Bar
   - Inventory Panel
   - Menu Panel

#### B. Setup Health Bar:
```csharp
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;
    
    public void UpdateHealthBar(int current, int max)
    {
        healthSlider.value = (float)current / max;
        healthText.text = $"{current}/{max}";
    }
}
```

---

### 6. 🔧 Manager Setup

#### A. AudioManager:
```csharp
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip buttonClick;
}
```

#### B. InputManager:
```csharp
public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private bool enableInput = true;
    [SerializeField] private float inputDeadzone = 0.1f;
}
```

---

### 7. 📁 File Structure Example

```
📁 Assets/
├── 📁 Scripts/           - All code
├── 📁 Prefabs/           - Player, UI prefabs
├── 📁 Data/              - ScriptableObject assets
│   ├── 📁 Items/         - Item data assets
│   ├── 📁 Stats/         - Stat data assets
│   └── 📁 Game/          - Game configuration
├── 📁 Sprites/           - Icons, UI sprites
├── 📁 Audio/             - Music, SFX
└── 📁 Scenes/            - Game scenes
```

---

### 8. 🚀 Quick Start Checklist

- [ ] ✅ Tạo GameManager GameObject với GameManager component
- [ ] ✅ Tạo Player GameObject với tất cả Player components
- [ ] ✅ Tạo các Manager GameObjects (Audio, Input, UI)
- [ ] ✅ Tạo ScriptableObject assets cho items
- [ ] ✅ Setup Canvas và UI elements
- [ ] ✅ Assign references trong Inspector
- [ ] ✅ Test game functionality

---

### 9. 🎯 Best Practices

1. **Luôn assign references** trong Inspector thay vì FindObjectOfType
2. **Sử dụng ScriptableObjects** cho data thay vì hardcode
3. **Subscribe/Unsubscribe events** đúng cách trong OnEnable/OnDisable
4. **Sử dụng EventBus** cho communication giữa systems
5. **Follow Clean Architecture** - không mix logic giữa layers

---

### 10. 🐛 Troubleshooting

**Lỗi thường gặp:**
- ❌ Missing references → Assign trong Inspector
- ❌ Null reference → Check component setup
- ❌ Event không fire → Check EventBus subscription
- ❌ ScriptableObject không load → Check asset path

**Debug tips:**
- Sử dụng Debug.Log để track flow
- Check Console cho errors
- Verify component setup trong Inspector
