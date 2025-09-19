# Entities Layer

## 📋 Mục đích
Entities layer chứa các MonoBehaviours gắn trực tiếp với các game objects trong world. Đây là layer kết nối giữa Unity engine và game systems, xử lý input, rendering, physics và các Unity-specific behaviors.

## 🏗️ Cấu trúc thư mục

### 📁 Player/
**Mục đích**: Tất cả components liên quan đến player character
- Player controller và movement
- Input handling
- Player-specific behaviors
- Camera following

**Thành phần chính**:
- `PlayerController.cs` - Main player logic
- `PlayerMovement.cs` - Movement mechanics
- `PlayerInput.cs` - Input processing
- `PlayerAnimator.cs` - Animation control
- `PlayerHealth.cs` - Health management

### 📁 Enemies/
**Mục đích**: AI entities và enemy behaviors
- Enemy AI controllers
- Pathfinding integration
- Enemy-specific mechanics
- Combat behaviors

**Thành phần chính**:
- `EnemyController.cs` - Base enemy logic
- `EnemyAI.cs` - AI behavior trees
- `EnemyPatrol.cs` - Patrol patterns
- `EnemyCombat.cs` - Combat integration
- Specific enemy types (Goblin, Dragon, etc.)

### 📁 Items/
**Mục đích**: Vật phẩm có thể tương tác trong world
- Collectible items
- Interactive objects
- Equipment visuals
- Item drop mechanics

**Thành phần chính**:
- `WorldItem.cs` - Items trong world
- `Collectible.cs` - Collectible behavior
- `InteractableObject.cs` - Interactive objects
- `ItemVisualizer.cs` - Item representation

### 📁 Environment/
**Mục đích**: Environmental objects và interactive elements
- Doors, chests, switches
- Environmental hazards
- Platforms và moving objects
- Weather effects integration

**Thành phần chính**:
- `Door.cs` - Door mechanics
- `Chest.cs` - Container objects
- `Switch.cs` - Activatable objects
- `Platform.cs` - Moving platforms
- `Hazard.cs` - Environmental dangers

## ⚡ Đặc điểm chính

### ✅ Unity Integration
- MonoBehaviour-based
- Unity lifecycle methods
- Component-based architecture
- Scene integration

### ✅ GameSystems Bridge
- Delegates logic to GameSystems
- Translates Unity events to domain events
- Handles Unity-specific concerns

### ✅ Composition over Inheritance
- Modular component design
- Configurable behaviors
- Easy to extend và customize

## 📐 Quy tắc thiết kế

### ✅ Nên làm:
- Delegate business logic to GameSystems
- Use Unity events để communicate
- Implement proper component lifecycle
- Cache component references trong Awake()

### ❌ Không nên:
- Chứa complex business logic
- Directly couple với other entities
- Ignore Unity performance best practices
- Create memory leaks với events

## 🔗 Dependencies
- **Depends on**: GameSystems, Foundation, Unity APIs
- **Used by**: Presentation layer
- **Communicates with**: Infrastructure layer

## 📊 Architecture Pattern

```
Unity Scene Objects
        ↓
Entity MonoBehaviours (this layer)
        ↓
GameSystems (business logic)
        ↓
Foundation (utilities)
```

## 📝 Ví dụ sử dụng

```csharp
// Player Controller
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMovement movement;
    
    private void Awake()
    {
        // Initialize components
        playerStats = new PlayerStats();
        movement = GetComponent<PlayerMovement>();
    }
    
    private void Update()
    {
        // Handle input and delegate to systems
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var jumpResult = movement.TryJump();
            if (jumpResult.Success)
            {
                EventBus.Publish(new PlayerJumpedEvent());
            }
        }
    }
}

// Enemy AI
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyStats enemyStats;
    [SerializeField] private NavMeshAgent agent;
    
    private void Start()
    {
        enemyStats = new EnemyStats();
        agent = GetComponent<NavMeshAgent>();
    }
    
    private void Update()
    {
        // AI logic delegation
        var aiDecision = enemyAI.GetNextAction(transform.position, playerPosition);
        ExecuteAction(aiDecision);
    }
}

// Interactive Item
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData itemData;
    
    public void Interact(GameObject interactor)
    {
        var playerInventory = interactor.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            var addResult = playerInventory.TryAddItem(itemData);
            if (addResult.Success)
            {
                EventBus.Publish(new ItemCollectedEvent(itemData));
                Destroy(gameObject);
            }
        }
    }
}
```

## 🔧 Component Patterns

### 🎯 Controller Pattern
- Main entity logic
- Coordinates other components
- Handles state management

### 🎯 Component Composition
- Separate concerns into components
- Configurable behavior mixing
- Runtime component addition/removal

### 🎯 Event Integration
- Publish domain events
- Subscribe to system events
- Unity event integration

## ⚡ Performance Considerations
- Object pooling cho frequently spawned entities
- Component caching để avoid GetComponent calls
- Efficient Update() patterns
- Memory management cho dynamic objects

## 🧪 Testing Strategy
- **Integration Tests**: Entity behavior với GameSystems
- **Scene Tests**: Multi-entity interactions
- **Performance Tests**: Large numbers of entities
- **Manual Tests**: Player feel và responsiveness

