# Enemy Configs

All enemy stats are defined in `Assets/Scripts/Enemies/EnemyConfig.cs` via static factory methods.
Stats are applied in `HumanEnemy.InitializeFromConfig()` (line ~114).

## Config Values Table

| Stat | PawnUnarmed | PawnAxe | Lancer | Archer | Warrior | Monk | Miner |
|------|-------------|---------|--------|--------|---------|------|-------|
| **HP** | 20 | 30 | 50 | 40 | 80 | 40 | 30 |
| **Damage** | 5 | 10 | 15 | 10 | 20 | 8 | 10 |
| **Attack Speed** | 1.0 | 0.8 | 0.8 | 0.4 | 0.6 | 0.73 | 0.8 |
| **Attack Cooldown** | 1.0s | 1.25s | 1.25s | 2.5s | 1.67s | 1.37s | 1.25s |
| **Attack Range** | 1.2 | 1.5 | 2.0 | 30.0 | 1.5 | 5.0 | 1.5 |
| **Move Speed** | 2.0 | 2.5 | 2.5 | 2.5 | 1.8 | 2.0 | 2.5 |
| **isRanged** | no | no | no | yes | no | no | no |
| **Behavior** | Coward | Aggressive | Defensive | Ranged | Aggressive | Defensive | Guard |
| **Detection Range** | 16 | 6 | 5 | 35 | 6 | 8 | 6 |
| **Chase Range** | 16 | 10 | 8 | 35 | 10 | 10 | 8 |
| **Flee Range** | 16 | - | - | - | - | - | - |
| **Preferred Range** | - | - | - | 20 | - | - | - |
| **Fear Range** | - | - | - | 10 | - | - | - |
| **XP Reward** | 10 | 20 | 35 | 40 | 60 | 75 | 25 |
| **Frame Size** | 192 | 192 | 320 | 192 | 192 | 192 | 192 |

## Loot Table

| Loot | PawnUnarmed | PawnAxe | Lancer | Archer | Warrior | Monk | Miner |
|------|-------------|---------|--------|--------|---------|------|-------|
| **Meat** | 5-10 | 8-15 | 12-20 | 10-18 | 20-35 | 15-25 | 5-10 |
| **Wood** | 1-3 (20%) | 2-5 (30%) | 5-10 (50%) | 8-15 (60%) | 15-25 (80%) | 20-30 (90%) | 0-2 (20%) |
| **Gold** | 0 (0%) | 0 (0%) | 1 (5%) | 1 (10%) | 1-2 (25%) | 2-3 (35%) | 1-3 (50%) |

## Tower Stats

Defined in `Assets/Scripts/Buildings/Watchtower.cs` → `SetupTowerType()` (line ~189).

| Stat | Basic Tower | Advanced Tower |
|------|-------------|----------------|
| **HP** | 150 | 150 |
| **Damage** | 12 | 10 |
| **Attack Speed** | 0.5 | 1.0 |
| **Attack Range** | 36 | 10 |
| **Projectile Speed** | 10 | 12 |
| **Guards** | 5 | 5 |
| **Wood Drop** | 4-6 | 4-6 |
| **Gold Drop** | 1-5 | 1-5 |

Tower projectile (TowerArrow.prefab): scale 0.42, speed 21.6, arcHeight 5.

## Where Stats Are Applied

- `EnemyConfig.cs` → factory methods (`CreateArcherConfig()`, etc.)
- `HumanEnemy.InitializeFromConfig()` → copies config to EnemyBase fields
- `EnemyBase.cs` → base class with `maxHealth`, `damage`, `moveSpeed`, `attackRange`, `attackCooldown`, etc.
- `Watchtower.SetupTowerType()` → tower-specific stats
- Prefab files: `ArcherArrow.prefab` (scale 0.35, speed 24), `TowerArrow.prefab` (scale 0.42, speed 21.6)
