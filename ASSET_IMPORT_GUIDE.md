# Tiny Swords Asset Pack - Import Guide

## Overview

This document describes the correct import settings for the Tiny Swords asset pack in Unity 6.

## Critical Settings

### 1. Max Texture Size

**Problem:** Unity defaults to `maxTextureSize = 2048`. Many Tiny Swords sprite sheets exceed this:
- `Lancer_Idle.png`: 3840x320 (12 frames)
- `Tree1.png`: 1536x256 (6 frames)
- Other large animations may also exceed 2048px width

**Solution:** Set `maxTextureSize = 8192` for all sprite sheets.

In Unity:
1. Select the sprite in Project window
2. Inspector > Max Size > 8192
3. Apply

In code (TextureImporter):
```csharp
importer.maxTextureSize = 8192;

// Also set for specific platforms:
TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Standalone");
settings.maxTextureSize = 8192;
settings.overridden = true;
importer.SetPlatformTextureSettings(settings);
```

### 2. Sprite Import Mode

**Setting:** Multiple (for sprite sheets)

All animation sprite sheets must be set to:
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Multiple

### 3. Grid Slicing

**Tiny Swords uses these cell sizes:**

| Asset Type | Cell Size | Examples |
|-----------|-----------|----------|
| All units (Pawn, Archer, Warrior, Lancer, Monk) | 320x320 | Lancer_Idle.png, Warrior_Run.png |
| Trees | 256x256 | Tree1.png - Tree4.png |
| Duck | 32x32 | Rubber duck.png |
| Some effects | 192x192 | Various effect sprites |
| Small sprites | 128x128, 64x64 | Smaller decorations |

**How to slice in Unity:**
1. Select sprite sheet
2. Open Sprite Editor (button in Inspector)
3. Slice > Grid by Cell Size
4. Set Cell Size to appropriate size
5. Apply

### 4. Pixels Per Unit (PPU)

**Setting:** 100 (Unity default)

With PPU=100:
- A 320x320 sprite = 3.2 Unity units
- Scale factor of 4x makes units appear appropriately sized in game

### 5. Filter Mode

**Setting:** Point (no filter)

Critical for pixel art to avoid blurry sprites:
- Filter Mode: Point (no filter)

### 6. Compression

**Setting:** None (Uncompressed)

For clean pixel art:
- Compression: None
- Format: RGBA 32 bit (or automatic)

## Sprite Sheet Dimensions

### Blue Units (Enemies)

| Character | Sprite | Dimensions | Frames | Cell Size |
|-----------|--------|------------|--------|-----------|
| Pawn | Pawn_Idle.png | 1920x320 | 6 | 320x320 |
| Pawn | Pawn_Run.png | 1920x320 | 6 | 320x320 |
| Archer | Archer_Idle.png | 1920x320 | 6 | 320x320 |
| Archer | Archer_Run.png | 1920x320 | 6 | 320x320 |
| Warrior | Warrior_Idle.png | 1920x320 | 6 | 320x320 |
| Warrior | Warrior_Run.png | 1920x320 | 6 | 320x320 |
| Lancer | Lancer_Idle.png | 3840x320 | 12 | 320x320 |
| Lancer | Lancer_Run.png | 1920x320 | 6 | 320x320 |
| Monk | Idle.png | 1920x320 | 6 | 320x320 |
| Monk | Run.png | 1920x320 | 6 | 320x320 |

### Decorations

| Asset | Sprite | Dimensions | Frames | Cell Size |
|-------|--------|------------|--------|-----------|
| Tree 1-4 | Tree1.png - Tree4.png | 1536x256 | 6 | 256x256 |
| Duck | Rubber duck.png | 96x32 | 3 | 32x32 |

### Enemy Pack (Spider, Gnoll, etc.)

| Character | Sprite | Typical Size | Cell Size |
|-----------|--------|--------------|-----------|
| Spider | Spider_Idle.png | Varies | 320x320 |
| Spider | Spider_Run.png | Varies | 320x320 |
| Spider | Spider_Attack.png | Varies | 320x320 |

## Common Issues

### Issue: Sprite appears as blue/pink square
**Cause:** Sprite not sliced, or wrong import settings
**Fix:**
1. Check maxTextureSize >= sprite width
2. Set Sprite Mode to Multiple
3. Open Sprite Editor and slice by grid (correct cell size)
4. Apply changes

### Issue: Sprite appears blurry
**Cause:** Filter Mode set to Bilinear or Trilinear
**Fix:** Set Filter Mode to Point (no filter)

### Issue: Sprite has compression artifacts
**Cause:** Texture compression enabled
**Fix:** Set Compression to None

### Issue: Animation shows wrong frames or flickers
**Cause:** Sprites not sorted correctly, or duplicate frame at loop point
**Fix:**
1. Ensure sprites are named with numeric suffix (_0, _1, _2...)
2. Sort by natural number order in animation clip
3. Add extra keyframe at end to hold last frame before loop

### Issue: Sprite scaled incorrectly
**Cause:** PPU doesn't match expected size
**Fix:**
- Use PPU=100 for standard sizing
- Apply transform.localScale for visual scaling (e.g., 4x for enemies)

## File Structure

```
Assets/Sprites/
├── Tiny Swords (Free Pack)/
│   └── Tiny Swords (Free Pack)/
│       ├── Units/
│       │   ├── Blue Units/     <- Enemies
│       │   │   ├── Pawn/
│       │   │   ├── Archer/
│       │   │   ├── Warrior/
│       │   │   ├── Lancer/
│       │   │   └── Monk/
│       │   └── Red Units/      <- Not used
│       ├── Buildings/
│       │   ├── Blue Buildings/
│       │   └── Red Buildings/
│       └── Terrain/
│           ├── Decorations/
│           │   ├── Bushes/
│           │   ├── Rocks/
│           │   └── Rubber Duck/
│           └── Resources/
│               ├── Meat/
│               ├── Wood/
│               │   └── Trees/   <- Animated trees
│               └── Gold/
│
├── Tiny Swords (Enemy Pack)/
│   └── Tiny Swords (Enemy Pack)/
│       └── Enemy Pack/
│           ├── Spider/         <- Player
│           ├── Gnoll/          <- Friendly unit
│           ├── Gnome/          <- Friendly unit
│           ├── Shaman/         <- Friendly unit
│           └── Enemy Avatars/  <- UI icons
│
└── Tiny Swords/
    └── Tiny Swords (Update 010)/
        ├── Factions/
        │   └── Knights/
        │       └── Troops/
        │           └── Dead/   <- Skull sprites
        └── Resources/
            └── Gold Mine/
```

## Quick Reference: Import Settings Summary

| Setting | Value |
|---------|-------|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Multiple |
| Pixels Per Unit | 100 |
| Filter Mode | Point (no filter) |
| Compression | None |
| Max Size | 8192 |

### Cell Sizes by Asset Type

| Asset Type | Cell Size |
|------------|-----------|
| Blue Units (enemies) | 320x320 |
| Trees | 256x256 |
| Duck | 32x32 |

## Automation

Use the `LevelEditor` editor window:
1. Window > Puppet Master > Level Editor
2. Go to "Tools" tab
3. Click "Fix Unit Sprites (320x320)" to auto-configure enemy sprites
4. Click "Fix Decoration Sprites" to auto-configure Trees (256x256) and Duck (32x32)

This will automatically:
- Set maxTextureSize = 8192
- Set Sprite Mode = Multiple
- Slice sprites by correct cell size
- Set Filter Mode = Point
- Reimport the assets
