# Presentation Layer

## 📋 Mục đích
Presentation layer chịu trách nhiệm về tất cả feedback đến người chơi - UI, audio, visual effects, và animations. Layer này tách biệt hoàn toàn game logic và chỉ focus vào việc hiển thị thông tin và phản hồi của system.

## 🏗️ Cấu trúc thư mục

### 📁 UI/
**Mục đích**: User Interface và HUD elements
- Game UI panels và screens
- HUD elements (health bars, minimaps)
- Menu systems
- Responsive UI layouts

**Thành phần chính**:
- `UIManager.cs` - Central UI coordination
- `GameHUD.cs` - In-game HUD controller
- `MenuSystem.cs` - Menu navigation
- `HealthBar.cs` - Health display component
- `InventoryUI.cs` - Inventory interface

### 📁 Audio/
**Mục đích**: Sound effects và music management
- Audio event handling
- Music transitions
- 3D spatial audio
- Audio mixing và volume control

**Thành phần chính**:
- `AudioManager.cs` - Central audio coordination
- `SFXPlayer.cs` - Sound effects player
- `MusicController.cs` - Background music
- `AudioMixer.cs` - Audio mixing logic
- `SpatialAudio.cs` - 3D audio positioning

### 📁 VFX/
**Mục đích**: Visual effects và particle systems
- Combat effects
- Environmental effects
- UI transitions
- Particle system management

**Thành phần chính**:
- `VFXManager.cs` - Effect coordination
- `CombatEffects.cs` - Combat visual feedback
- `EnvironmentVFX.cs` - Environmental effects
- `ParticleController.cs` - Particle management
- `ScreenEffects.cs` - Screen-space effects

### 📁 Animation/
**Mục đích**: Animation control và state machines
- Character animations
- UI animations
- Camera movements
- Tween sequences

**Thành phần chính**:
- `AnimationController.cs` - Animation coordination
- `CharacterAnimator.cs` - Character animation
- `UIAnimator.cs` - UI animation sequences
- `CameraAnimator.cs` - Camera movements
- `TweenManager.cs` - Tween animations

## ⚡ Đặc điểm chính

### ✅ Event-Driven
- React to game system events
- No direct coupling với business logic
- Clean separation of concerns

### ✅ Responsive Design
- Adaptable UI layouts
- Performance-conscious rendering
- Accessibility support

### ✅ Modular Components
- Reusable UI components
- Configurable effects
- Easy customization

## 📐 Quy tắc thiết kế

### ✅ Nên làm:
- Listen to events từ GameSystems
- Use data binding cho UI updates
- Implement proper cleanup cho effects
- Cache expensive UI operations

### ❌ Không nên:
- Directly modify game state
- Perform business logic calculations
- Create tight coupling với Entities
- Ignore performance implications

## 🔗 Dependencies
- **Depends on**: Foundation (Events), Unity UI, Unity Audio
- **Listens to**: GameSystems events
- **Independent from**: Game business logic

## 📊 Event Flow

```
GameSystems → Foundation Events → Presentation Layer
                                       ↓
                              UI + Audio + VFX + Animation
```

## 📝 Ví dụ sử dụng

```csharp
// UI Manager
public class UIManager : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private InventoryUI inventoryUI;
    
    private void Start()
    {
        EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        EventBus.Subscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
    }
    
    private void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        healthBar.UpdateHealth(evt.CurrentHealth, evt.MaxHealth);
    }
    
    private void OnInventoryUpdated(InventoryUpdatedEvent evt)
    {
        inventoryUI.RefreshInventory(evt.InventoryData);
    }
}

// Audio Manager
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] combatSounds;
    [SerializeField] private AudioSource sfxSource;
    
    private void Start()
    {
        EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        EventBus.Subscribe<PlayerLevelUpEvent>(OnLevelUp);
    }
    
    private void OnDamageDealt(DamageDealtEvent evt)
    {
        var soundIndex = Random.Range(0, combatSounds.Length);
        sfxSource.PlayOneShot(combatSounds[soundIndex]);
    }
}

// VFX Manager
public class VFXManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem damageEffect;
    [SerializeField] private ParticleSystem healingEffect;
    
    private void Start()
    {
        EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
    }
    
    private void OnDamageDealt(DamageDealtEvent evt)
    {
        var effect = Instantiate(damageEffect, evt.Position, Quaternion.identity);
        StartCoroutine(DestroyAfterDuration(effect.gameObject, 2f));
    }
}

// Animation Controller
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    
    private void Start()
    {
        EventBus.Subscribe<PlayerMovementEvent>(OnMovement);
        EventBus.Subscribe<PlayerAttackEvent>(OnAttack);
    }
    
    private void OnMovement(PlayerMovementEvent evt)
    {
        animator.SetFloat("Speed", evt.MovementSpeed);
        animator.SetBool("IsMoving", evt.IsMoving);
    }
    
    private void OnAttack(PlayerAttackEvent evt)
    {
        animator.SetTrigger("Attack");
    }
}
```

## 🎨 UI Architecture

### 🎯 MVVM Pattern
```csharp
public class InventoryViewModel : MonoBehaviour
{
    public ObservableCollection<ItemData> Items { get; private set; }
    
    private void Start()
    {
        EventBus.Subscribe<InventoryUpdatedEvent>(OnInventoryChanged);
    }
    
    private void OnInventoryChanged(InventoryUpdatedEvent evt)
    {
        Items.Clear();
        foreach (var item in evt.Items)
        {
            Items.Add(item);
        }
    }
}
```

### 🎯 Component-Based UI
```csharp
public class UIPanel : MonoBehaviour
{
    public virtual void Show() { gameObject.SetActive(true); }
    public virtual void Hide() { gameObject.SetActive(false); }
    public virtual void Initialize() { }
}

public class InventoryPanel : UIPanel
{
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemPrefab;
    
    public override void Initialize()
    {
        EventBus.Subscribe<InventoryUpdatedEvent>(RefreshUI);
    }
}
```

## 🎵 Audio Architecture

### 🎯 Audio Event System
```csharp
public enum AudioEventType
{
    SFX_Combat,
    SFX_UI,
    Music_Background,
    Music_Combat
}

public class AudioEvent
{
    public AudioEventType Type { get; set; }
    public AudioClip Clip { get; set; }
    public Vector3 Position { get; set; }
    public float Volume { get; set; } = 1f;
}
```

## ✨ VFX Architecture

### 🎯 Effect Pool System
```csharp
public class VFXPool : MonoBehaviour
{
    private Dictionary<string, Queue<ParticleSystem>> effectPools = new();
    
    public ParticleSystem GetEffect(string effectName)
    {
        if (effectPools.TryGetValue(effectName, out var pool) && pool.Count > 0)
        {
            return pool.Dequeue();
        }
        
        return CreateNewEffect(effectName);
    }
    
    public void ReturnEffect(string effectName, ParticleSystem effect)
    {
        if (!effectPools.ContainsKey(effectName))
            effectPools[effectName] = new Queue<ParticleSystem>();
        
        effect.Stop();
        effect.Clear();
        effectPools[effectName].Enqueue(effect);
    }
}
```

## ⚡ Performance Optimizations

### 🚀 UI Performance
- Object pooling cho dynamic UI elements
- Canvas grouping để minimize draw calls
- Efficient layout calculations
- Texture atlasing cho UI sprites

### 🚀 Audio Performance
- Audio compression settings
- 3D audio culling
- Audio source pooling
- Streaming cho large audio files

### 🚀 VFX Performance
- Particle LOD systems
- GPU-based particle systems
- Effect culling based on distance
- Shared material instances

## 🧪 Testing Strategy
- **Visual Tests**: Screenshot comparison
- **Audio Tests**: Audio output validation
- **Performance Tests**: Frame rate impact
- **Accessibility Tests**: UI accessibility compliance

