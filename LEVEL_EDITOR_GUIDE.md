# Puppet Master - Level Editor Guide

## Overview

Level Editor - это инструмент Unity Editor для создания и редактирования игровых уровней в Puppet Master. Организован по вкладкам для удобной навигации.

## Доступ

```
Window > Puppet Master > Level Editor
```

## Вкладки

### 1. Player & Core
- **Create Player (Spider)** - создаёт игрока
- **Core Systems** - GameManager, EventManager, EffectManager, ResourceSpawner, QuestSystem, UpgradeSystem
- **Camera** - настройка камеры и follow

### 2. Enemies
**Melee:**
- Pawn (Unarmed) - безоружный крестьянин
- Pawn (Axe) - крестьянин с топором
- Warrior - рыцарь в тяжёлой броне
- Lancer - копейщик

**Ranged:**
- Archer - лучник (стреляет стрелами по дуге)

**Support:**
- Monk - монах-целитель
- Miner - шахтёр с киркой

**Группы:**
- Patrol Group - 3 Pawn в линию
- Mixed Squad - Warrior + 2 Archer + 2 Pawn
- Defense Squad - 3 Lancer + Monk

### 3. Buildings
**Enemy (Blue):**
- House 1, House 2 - дома для спавна врагов
- Tower - оборонительная башня
- Castle - замок

**Player (Red):**
- Storage - хранилище ресурсов
- Red House - дом игрока

**Пресеты:**
- Enemy Base - House + Tower + Guards
- Player Base Starter - Storage + House

### 4. Resources
**Одиночные:**
- Meat, Wood, Gold

**Кластеры:**
- Meat/Wood Cluster (5 шт)
- Gold Cluster (3 шт)
- Mixed Resources (10 шт)

### 5. Decorations
**Trees (анимированные):**
- Tree 1-4 - деревья с анимацией покачивания (256x256, 6 кадров)

**Stumps:**
- Stump 1-4 - пеньки

**Bushes:**
- Bushe 1-4 - кусты

**Rocks:**
- Rock 1-4 - камни

**Water Decorations:**
- Duck - резиновая утка с анимацией (32x32, 3 кадра)
- Water Rock 1-4 - камни в воде

**Пресеты:**
- Forest Patch - группа деревьев
- Rocky Area - скалистая местность

### 6. Tilemap
**One-Click Setup:**
- CREATE COMPLETE TILEMAP SYSTEM - создаёт всю систему за один клик

**Components:**
- Create Grid + Tilemaps - создаёт Grid с 4 слоями (Water, Ground, Elevation, UpperGround)
- Create Tile Palette - создаёт палитру с уже добавленными тайлами (готовую к рисованию!)
- Setup Organized Hierarchy - организует иерархию сцены

**Sprite Import:**
- Fix Tileset Sprite Imports - нарезает тайлсеты на тайлы 64x64

**Как рисовать:**
1. Нажмите "CREATE COMPLETE TILEMAP SYSTEM"
2. Откройте Window > 2D > Tile Palette
3. Выберите "TinySwords_Terrain" в выпадающем списке палитры
4. Выберите инструмент кисть (Brush)
5. Кликните на тайл в палитре
6. Рисуйте на сцене!

**Слои и Sorting Order:**
| Слой | Sorting Order | Назначение |
|------|---------------|------------|
| Water | -10 | Вода (под землёй) |
| Ground | 0 | Основная земля |
| Elevation | 5 | Стены ярусов (с коллайдером) |
| UpperGround | 10 | Верхний ярус |
| Characters | 100+ | Игрок, враги, NPC |

**Категории тайлов в палитре:**
- Grass - зелёная трава
- GrassDark - тёмная трава
- Dirt - земля
- Sand - песок
- Snow - снег
- Flat - плоские тайлы (Update 010)
- Elevation - тайлы возвышенностей (Update 010)

### 7. Tools
**Sprite Import (для AI-агентов):**
- Fix Unit Sprites - автоматическая нарезка спрайтов Blue Units (320x320)
- Fix Decoration Sprites - автоматическая нарезка Trees (256x256) и Duck (32x32)

**Selection:**
- Select All Enemies/Resources/Decorations

**Cleanup:**
- Delete All Enemies/Resources

**Debug:**
- Toggle Collider Visualizers
- Size Reference Circles

**Quick Start:**
- CREATE COMPLETE TEST SCENE - создаёт готовую тестовую сцену

## Использование для AI-агентов

### Автоматическая нарезка спрайтов

**ВАЖНО:** Перед созданием анимированных объектов запустите автонарезку спрайтов:

1. Откройте Level Editor: `Window > Puppet Master > Level Editor`
2. Перейдите на вкладку "Tools"
3. Нажмите "Fix Unit Sprites (320x320)" для врагов
4. Нажмите "Fix Decoration Sprites" для деревьев и утки

Это автоматически:
- Установит maxTextureSize = 8192
- Установит Sprite Mode = Multiple
- Нарежет спрайты на кадры нужного размера
- Установит Filter Mode = Point (для пиксель-арта)

### Создание объектов программно

Все методы создания объектов статические и могут вызываться из других скриптов:

```csharp
// Player
GameObject player = LevelEditor.CreatePlayer(Vector3.zero);

// Enemies
GameObject pawn = LevelEditor.CreateEnemy(EnemyType.PawnAxe, new Vector3(5, 0, 0));
GameObject archer = LevelEditor.CreateEnemy(EnemyType.Archer, new Vector3(0, 5, 0));

// Buildings
GameObject house = LevelEditor.CreateBuilding("House1", position, isEnemy: true);
GameObject storage = LevelEditor.CreateStorage(new Vector3(-10, 0, 0));

// Resources
LevelEditor.CreateResource(ResourcePickup.ResourceType.Gold, position);

// Decorations (с автоматической анимацией)
LevelEditor.CreateDecoration("Tree1", position);  // Анимированное дерево
LevelEditor.CreateDecoration("Duck", position);   // Анимированная утка
LevelEditor.CreateDecoration("Rock1", position);  // Статичный камень
```

### Константы

```csharp
// Масштабирование
UNIT_SCALE = 4f;          // Враги и стрелы
PLAYER_SCALE = 1f;        // Паук (игрок)
BUILDING_SCALE = 1f;      // Здания
RESOURCE_SCALE = 0.33f;   // Ресурсы
DECORATION_SCALE = 1f;    // Декорации
TREE_SCALE = 1f;          // Деревья

// Коллайдеры
PLAYER_COLLIDER_RADIUS = 0.5f;
ENEMY_COLLIDER_RADIUS = 0.3f;
```

### Типы врагов (EnemyType enum)

```csharp
public enum EnemyType
{
    PawnUnarmed,    // Безоружный крестьянин
    PawnAxe,        // Крестьянин с топором
    PawnMiner,      // Шахтёр с киркой
    Miner,          // Алиас для PawnMiner
    Archer,         // Лучник
    Warrior,        // Рыцарь
    Lancer,         // Копейщик
    Monk            // Монах-целитель
}
```

### Типы ресурсов

```csharp
public enum ResourceType
{
    Meat,   // Мясо - еда
    Wood,   // Дерево - строительство
    Gold,   // Золото - улучшения
    Skull   // Черепа - от убитых врагов
}
```

### Типы декораций

```csharp
// Анимированные (автоматически создаётся AnimationClip и AnimatorController)
"Tree1", "Tree2", "Tree3", "Tree4"  // 256x256, 6 кадров
"Duck"                               // 32x32, 3 кадра

// Статичные
"Stump1", "Stump2", "Stump3", "Stump4"
"Bushe1", "Bushe2", "Bushe3", "Bushe4"
"Rock1", "Rock2", "Rock3", "Rock4"
"WaterRock1", "WaterRock2", "WaterRock3", "WaterRock4"
```

## Структура файлов

```
Assets/Scripts/Editor/
├── LevelEditor.cs       # Основной редактор уровней
└── ...

Assets/Animations/       # Сгенерированные контроллеры анимаций
├── Player_Spider_Controller.controller
├── Enemy_PawnAxe_Controller.controller
├── Enemy_Archer_Controller.controller
├── Decoration_Tree1_Controller.controller
├── Decoration_Duck_Controller.controller
└── ...

Assets/Prefabs/         # Сгенерированные префабы
└── Arrow.prefab        # Стрела для лучника
```

## Workflow для создания уровня

1. Откройте Level Editor: `Window > Puppet Master > Level Editor`
2. Перейдите на вкладку "Tools" и нажмите "Fix Unit Sprites" и "Fix Decoration Sprites"
3. Перейдите на вкладку "Player & Core"
4. Нажмите "Create All Core Systems"
5. Нажмите "Create Player"
6. Перейдите на вкладку "Enemies" и расставьте врагов
7. Перейдите на вкладку "Buildings" и добавьте здания
8. Перейдите на вкладку "Resources" и разбросайте ресурсы
9. Перейдите на вкладку "Decorations" и добавьте деревья, камни, кусты
10. Сохраните сцену

## Решение проблем

### Враг/декорация отображается как синий квадрат
1. Откройте Level Editor > Tools
2. Нажмите "Fix Unit Sprites" или "Fix Decoration Sprites"
3. Пересоздайте объект

### Спрайт обрезан/неправильного размера
Проверьте в Inspector спрайта:
- Max Size: 8192 (не 2048!)
- Sprite Mode: Multiple
- Grid slice: правильный размер ячейки

### Анимация не работает
1. Удалите контроллер из Assets/Animations/
2. Запустите "Fix Decoration Sprites" в Tools
3. Пересоздайте объект через Level Editor

### Размеры ячеек для нарезки спрайтов

| Тип | Размер ячейки |
|-----|---------------|
| Blue Units (враги) | 320x320 |
| Trees | 256x256 |
| Duck | 32x32 |

## Расширение для новых сущностей

Для добавления нового типа врага:

1. Добавьте тип в enum `EnemyType`:
```csharp
public enum EnemyType { ..., NewEnemy }
```

2. Добавьте спрайт в `GetEnemySprite()`:
```csharp
EnemyType.NewEnemy => $"{SPRITES_ROOT}/{BLUE_UNITS}/NewEnemy/NewEnemy_Idle.png",
```

3. Добавьте конфиг в `CreateEnemyConfig()`:
```csharp
EnemyType.NewEnemy => EnemyConfig.CreateNewEnemyConfig(),
```

4. Добавьте кнопку в `DrawEnemiesTab()`:
```csharp
DrawEntityButton("New Enemy", EnemyType.NewEnemy);
```

5. Добавьте анимации в `SetupEnemyAnimator()` если нужно кастомные анимации.

## Интеграция с другими системами

Level Editor использует следующие компоненты:
- `ColliderVisualizer` - debug визуализация коллайдеров
- `HumanEnemy` - компонент врага
- `EnemyConfig` - ScriptableObject с параметрами
- Animation system - автогенерация контроллеров и клипов
