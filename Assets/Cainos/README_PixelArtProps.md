# Pixel Art Top Down Props - Usage Guide

## Overview
This asset pack provides cemetery/graveyard themed props for the spider base in Puppet Master.

## Accessing Props in Level Editor
1. Open **Window > Puppet Master > Level Editor**
2. Click the **"Cemetery Props"** tab (6th tab)
3. Click any prop to place it at the scene view center

## Game Buildings (with special mechanics)

### Storage (Chest)
**Prefab:** `PF Props - Chest 01.prefab`

**Behavior:**
- Opens automatically when spider approaches WITH resources
- Resources fly out of spider into chest with arc animation
- Closes when spider has no resources or leaves
- Pressing interact also triggers deposit

**Code:** `Assets/Scripts/Buildings/Storage.cs`

**Parameters in Inspector:**
- `autoDeposit`: Enable automatic resource transfer (default: true)
- `depositInterval`: Time between each resource flying (default: 0.15s)
- `flyDuration`: How long resource takes to fly to chest (default: 0.6s)
- `arcHeight`: Height of the arc trajectory (default: 1.5)

### WorkBench (Altar)
**Prefab:** `PF Props - Altar 01.prefab`

**Behavior:**
- Runes glow cyan when spider approaches (built into prefab)
- Opens crafting/upgrade menu
- Used for creating units and purchasing farms

**Glow Effect:**
The altar has 4 child rune sprites that fade in/out based on trigger collider.
This effect is handled by `PropsAltar.cs` script (included in prefab).

**Code:**
- `Assets/Cainos/Pixel Art Top Down - Basic/Script/PropsAltar.cs` - Glow effect
- `Assets/Scripts/Buildings/Workbench.cs` - Game functionality

## Decorative Props (no special effects)

### Gravestones
- `Gravestone 01`, `02`, `03` - Different tombstone designs
- `Stone Coffin H`, `V` - Horizontal/Vertical coffins

### Pillars & Ruins
- `Pillar 01`, `02` - Stone pillars
- `Rune Pillar X3` - Tall rune pillar
- `Rune Pillar X2` - Short rune pillar
- `Rune Pillar Broken` - Destroyed pillar

### Gates & Structures
- `Gate 01`, `02` - Stone gates
- `Wooden Gate 01` - Closed wooden gate
- `Wooden Gate 01 Opened` - Open wooden gate

### Stones & Rubble
- `Stone 01` through `Stone 07` - Various stone decorations
- `Brick 01`, `02`, `03` - Brick rubble
- `Stone Cube 01` - Stone block

### Containers
- `Barrel 01` - Wooden barrel
- `Crate 01`, `02` - Wooden crates
- `Well 01` - Stone well

### Furniture & Signs
- `Stone Bench E/S/W` - Benches facing different directions
- `Statue 01` - Stone statue
- `Stone Lantern 01` - Decorative lantern
- `Pot 01`, `02`, `03` - Clay pots
- `Road Sign E/W` - Directional signs

### Plants
- `Tree 01`, `02`, `03` - Trees
- `Bush 01` through `06` - Bushes
- `Grass 01` through `15` - Grass patches
- `Flower 01` - Flowers

## Presets (Quick Placement)

### Graveyard Cluster
Places 3 gravestones and 2 stones in a natural arrangement.

### Spider Base
Places Storage, WorkBench, 2 Rune Pillars, and 2 Lanterns as a starting base.

### Ruined Temple Area
Places Altar with pillars, broken pillar, and brick rubble.

## Adding Props via Code

```csharp
// Place a prefab instance
LevelEditor.CreatePrefabInstance("Props/PF Props - Gravestone 01.prefab", position);

// Create Storage with full functionality
LevelEditor.CreateStorageFromPrefab(position);

// Create WorkBench with glow effect
LevelEditor.CreateWorkBenchFromPrefab(position);
```

## Sprite Source
All sprites come from `Assets/Cainos/Pixel Art Top Down - Basic/Texture/`:
- `TX Props.png` - Most props
- `TX Props with Shadow.png` - Props with shadows
- `TX Shadow.png` - Shadow sprites
- `TX Plant.png` - Plants and trees

## Notes
- Props use Sorting Layer "Props" with different orders for proper layering
- Shadow children are on a separate "Shadow" layer
- Some props have custom materials for glow/additive effects
- Colliders are set as triggers by default
