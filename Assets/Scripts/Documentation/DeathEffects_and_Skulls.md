# Система эффектов смерти и сбора черепов

## Обзор

При убийстве вражеского юнита:
1. Юнит мгновенно уничтожается
2. На его месте одновременно появляются **пыльный взрыв** (анимация Dust_01.png) и **черепок** (Dead.png, кадр 10)
3. Пыль рисуется поверх черепка (sortingOrder 1000), поэтому когда она рассеивается — черепок уже на месте
4. Игрок (паук) может собрать черепок, встав рядом и **стоя неподвижно** 3 секунды

Черепа — ресурс для покупки юнитов в алтаре. Никаких других ресурсов враги при смерти не роняют (кроме крестьянина, несущего ресурс).

---

## Архитектура: Какой файл за что отвечает

### Цепочка смерти

```
Враг получает урон → EnemyBase.TakeDamage() → если HP <= 0 → Die()
                                                     ↓
                                          HumanEnemy.Die() / Peasant.Die()
                                                     ↓
                               ┌─────────────────────┼─────────────────────┐
                               ↓                     ↓                     ↓
                    EffectManager            EffectManager           EventManager
                  .SpawnDeathEffect()     .SpawnSkullPickup()     .OnEnemyDied()
                               ↓                     ↓
                     DeathEffectPlayer          SkullPickup
                    (пыльный взрыв)       (черепок на земле)
```

### Цепочка сбора черепа

```
SkullCollector.Update() (на игроке)
        ↓
   Игрок стоит? (velocity < 0.05)
        ↓ да
   FindNearestSkull() — ищет SkullPickup в радиусе 4.5
        ↓ найден
   StartCollection(skull) → SkullPickup.StartCollection(transform)
        ↓
   SkullPickup.CollectionAnimation() — 3 секунды пульсирующая анимация
        ↓
   Если игрок пошёл → SkullCollector.CancelCollection() → SkullPickup.CancelCollection()
   Если дособрал   → SkullPickup.CompleteCollection() → GameManager.AddSkull(1)
```

---

## Файлы и их роли

### 1. `Assets/Scripts/Effects/EffectManager.cs` — Менеджер эффектов (Singleton)

**Ответственность:** Хранит ссылки на спрайтовые массивы, создаёт эффекты смерти и черепки.

**Ключевые поля (заполняются через DeathEffectSetup):**
- `deathDustFrames` — 18 спрайтов из Dust_01.png (64×64 grid)
- `tntExplosionFrames` — 37 спрайтов из Explosion_01.png (192×192 grid)
- `skullPickupFrames` — 14 спрайтов из Dead.png (128×128 grid)

**Ключевые методы:**
- `SpawnDeathEffect(Vector3 pos, bool useTntExplosion)` → создаёт DeathEffectPlayer с пылью. Масштаб: 8f для обычных, 6f для TNT
- `SpawnSkullPickup(Vector3 pos)` → создаёт SkullPickup, инициализирует спрайтами
- `SpawnSkullPickupDelayed(Vector3 pos, float delay)` → отложенный спавн (сейчас не используется, но доступен)

**Важно:** Если `deathDustFrames` пустой — пыль не появится. В консоли будет предупреждение "No death dust sprites assigned". Решение: запустить `Puppet Master → Setup Death Effects`.

---

### 2. `Assets/Scripts/Effects/DeathEffectPlayer.cs` — Анимация пыльного взрыва

**Ответственность:** Проигрывает покадровую анимацию пыли при смерти. Создаётся динамически.

**Параметры:**
| Поле | Значение | Описание |
|------|----------|----------|
| `dustFrameRate` | 15 fps | Скорость анимации пыли |
| `dustScale` | 8f | Масштаб пыли (передаётся через Setup) |
| `totalLifetime` | 3f | Через сколько секунд объект уничтожится |

**Как работает:**
1. `EffectManager.SpawnDeathEffect()` создаёт GameObject, добавляет `DeathEffectPlayer`
2. Вызывает `Setup(dustSprites, null, scale)` — скелет-спрайты не передаются (skull отдельный объект)
3. Вызывает `Play(position)` → запускает корутину `DeathSequence()`
4. `DeathSequence()` создаёт дочерний GameObject "DeathDust" с SpriteRenderer (sortingOrder=1000)
5. Покадрово проигрывает все dustFrames
6. После анимации скрывает рендерер, ждёт `totalLifetime`, уничтожает себя

**sortingOrder = 1000** — гарантирует что пыль рисуется поверх всего.

---

### 3. `Assets/Scripts/Resources/SkullPickup.cs` — Черепок на земле

**Ответственность:** Визуализация черепка, анимация сбора, начисление ресурса.

**Параметры:**
| Поле | Значение | Описание |
|------|----------|----------|
| `collectionRange` | 4.5 | Радиус, в котором начинается сбор |
| `collectionDuration` | 3 сек | Длительность анимации сбора |
| `skullScale` | 0.70 | Масштаб черепка (Dead.png при PPU 16 большой) |
| `sortingOrder` | -100 | Смещение для YSortingRenderer (за игроком) |
| `decayTime` | 30 сек | Через сколько черепок исчезнет сам |

**Idle-состояние:**
- Показывает кадр index 9 (Dead_9 = 10-й кадр, череп лежит на земле)
- Масштаб = skullScale (0.70)
- Используется YSortingRenderer с sortingOffset = -100 → всегда рисуется ЗА игроком и юнитами

**Анимация сбора (CollectionAnimation):**
Пульсирующая анимация из 5 сегментов, каждый по 0.6 сек (всего 3 сек):

```
Сегмент 1: кадр 9 → кадр 2   (череп → начало свечения)
Сегмент 2: кадр 2 → кадр 9   (обратно к черепу)
Сегмент 3: кадр 9 → кадр 1   (глубже к свечению)
Сегмент 4: кадр 1 → кадр 9   (обратно)
Сегмент 5: кадр 9 → кадр 0   (полное исчезновение)
```

В последнем сегменте, когда достигает кадра 0, масштаб плавно увеличивается до 2× от базового.

**При отмене сбора:** масштаб сбрасывается, показывается idle-кадр.

**При завершении:** `GameManager.AddSkull(1)`, `EventManager.OnSkullCollectionCompleted(1)`, объект уничтожается.

---

### 4. `Assets/Scripts/Player/SkullCollector.cs` — Сборщик черепков (компонент игрока)

**Ответственность:** Обнаружение черепков, запуск/отмена сбора, контроль правил.

**Параметры:**
| Поле | Значение | Описание |
|------|----------|----------|
| `collectionRange` | 4.5 | Радиус обнаружения черепков |
| `autoCollect` | true | Автоматический сбор при остановке |
| `movementThreshold` | 0.05 | Макс. скорость для "стоит на месте" |

**Правила сбора:**
1. **Стоять неподвижно** — `Rigidbody2D.linearVelocity.magnitude` должен быть < 0.05
2. **Один череп за раз** — `isCollecting` блокирует новые сборы
3. **Движение отменяет сбор** — если паук пошёл, вызывается `CancelCollection()`
4. **Автоматический** — не нужно нажимать кнопку, достаточно встать рядом

**Как находит черепки:**
```csharp
FindObjectsByType<SkullPickup>(FindObjectsSortMode.None)
```
Перебирает все SkullPickup в сцене, выбирает ближайший в радиусе `collectionRange` который не собирается и не собран.

---

### 5. `Assets/Scripts/Enemies/HumanEnemy.cs` — Смерть обычных врагов

**Метод `Die()` (override EnemyBase):**
```
1. isDead = true
2. StopMovement()
3. Звук смерти (config.deathSound)
4. EffectManager.SpawnDeathEffect(deathPos)     ← пыль
5. EffectManager.SpawnSkullPickup(deathPos)      ← черепок
6. EventManager.OnEnemyDied(gameObject)
7. Destroy(gameObject)                           ← мгновенное удаление
```

**Ресурсы НЕ выпадают** — `DropLoot()` не вызывается.

---

### 6. `Assets/Scripts/Enemies/Peasant.cs` — Смерть крестьянина

**Метод `Die()` (override EnemyBase):**
```
1. isDead = true
2. Если нёс ресурс → DropCarriedResourceFar()   ← ресурс отлетает на 10-12 единиц
3. StopMovement()
4. EffectManager.SpawnDeathEffect(deathPos)       ← пыль
5. EffectManager.SpawnSkullPickup(deathPos)        ← черепок
6. EventManager.OnEnemyDied(gameObject)
7. Destroy(gameObject)
```

**DropCarriedResourceFar():** Ресурс отлетает на 10-12 единиц в случайном направлении. Это **за пределами** магнита ресурсов (magnetRadius = 9), поэтому игрок увидит ресурс прежде чем его притянет.

---

### 7. `Assets/Scripts/Enemies/EnemyBase.cs` — Базовый класс (виртуальный Die)

**Метод `Die()` (virtual):**
- Только пыль (`SpawnDeathEffect`), **без черепка**
- Подклассы (HumanEnemy, Peasant) переопределяют и добавляют черепок

---

### 8. `Assets/Scripts/Resources/ResourceSpawner.cs` — Спавнер ресурсов

**Обработчик `OnEnemyDied`:** **ОТКЛЮЧЁН**. Раньше при смерти любого врага спавнились случайные ресурсы. Теперь обработчик пустой — враги не роняют ресурсы, только черепа (через EffectManager).

---

### 9. `Assets/Scripts/Editor/DeathEffectSetup.cs` — Настройка в редакторе

**Меню:** `Puppet Master → Setup Death Effects`

**Что делает при запуске:**
1. Нарезает спрайтшиты на кадры:
   - `Dust_01.png` → 64×64 grid → 18 кадров
   - `Explosion_01.png` → 192×192 grid → 37 кадров
   - `Dead.png` → 128×128 grid → 14 кадров (принудительная перенарезка)
2. Загружает кадры в массивы
3. Находит/создаёт EffectManager в сцене
4. Присваивает массивы через SerializedObject:
   - `deathDustFrames` ← Dust_01
   - `tntExplosionFrames` ← Explosion_01
   - `skullPickupFrames` ← Dead
5. Проверяет/добавляет SkullCollector на Player
6. **Принудительно обновляет** `collectionRange = 4.5` на SkullCollector

**Когда запускать:** После изменений в коде, при добавлении новой сцены, или если пыль/черепки не работают.

---

## Спрайтовые ассеты

| Файл | Путь | Grid | PPU | Кадров | Назначение |
|------|------|------|-----|--------|------------|
| Dust_01.png | Assets/Sprites/Effects/ | 64×64 | — | 18 | Пыльный взрыв при смерти |
| Explosion_01.png | Assets/Sprites/Effects/ | 192×192 | — | 37 | Огненный взрыв (смерть TNT юнита) |
| Dead.png | Assets/Sprites/Tiny Swords/.../Knights/Troops/Dead/ | 128×128 | 16 | 14 | Анимация черепа (появление/исчезновение) |

### Кадры Dead.png (128×128 grid, 7 столбцов × 2 ряда):
```
Кадр 0 (Dead_0): Свечение — начало появления
Кадр 1 (Dead_1): Свечение усиливается
Кадр 2 (Dead_2): Свечение → контур черепа
Кадр 3 (Dead_3): Череп формируется
...
Кадр 9 (Dead_9): Череп лежит на земле ← IDLE-КАДР (используется пока черепок ждёт сбора)
Кадры 10-13: Затухание черепа (не используются в текущей механике)
```

---

## Sorting (порядок отрисовки)

| Объект | Метод сортировки | sortingOrder |
|--------|-----------------|--------------|
| Пыльный взрыв | Фиксированный | 1000 (всегда сверху) |
| Игрок (паук) | YSortingRenderer | (-Y × 100) + 0 |
| Враги | YSortingRenderer | (-Y × 100) + 0 |
| Черепок | YSortingRenderer | (-Y × 100) − 100 |

Черепок **всегда** рисуется за игроком/врагами на той же Y позиции благодаря `sortingOffset = -100`.

---

## Диаграмма жизненного цикла черепка

```
Враг умирает
       ↓
EffectManager.SpawnSkullPickup(deathPos)
       ↓
new GameObject("SkullPickup")
+ SkullPickup компонент
+ SpriteRenderer
       ↓
Initialize(frames)
  → scale = 0.70
  → YSortingRenderer (offset -100)
  → sprite = deadFrames[9]   ← idle-кадр
  → Destroy(gameObject, 30)  ← таймер жизни
       ↓
  [Ожидание на земле]
       ↓                              ↓
  30 сек прошло               Игрок встал рядом (r < 4.5)
  → авто-уничтожение          и стоит неподвижно
                                       ↓
                              SkullCollector.StartCollection()
                              SkullPickup.StartCollection()
                                       ↓
                              CollectionAnimation() — 3 сек
                                  /              \
                          Игрок пошёл         Анимация завершена
                                ↓                     ↓
                        CancelCollection()    CompleteCollection()
                        → reset scale          → GameManager.AddSkull(1)
                        → idle-кадр            → EventManager.OnSkullCollectionCompleted
                        → ждём снова           → Destroy(gameObject)
```

---

## Частые проблемы и решения

| Проблема | Причина | Решение |
|----------|---------|---------|
| Нет пыли при смерти | `deathDustFrames` не заполнен | Запустить `Puppet Master → Setup Death Effects` |
| Нет черепка при смерти | `skullPickupFrames` не заполнен | Запустить `Puppet Master → Setup Death Effects` |
| Череп перед игроком | YSortingRenderer не добавлен или sortingOffset не -100 | Проверить Initialize() в SkullPickup |
| Сбор не работает | `collectionRange` старое значение в Inspector | Запустить Setup Death Effects (принудительно ставит 4.5) |
| Ресурсы падают при смерти | ResourceSpawner.OnEnemyDied не отключён | Проверить что обработчик пустой |
| Сбор черепа мгновенный | SkullCollector не проверяет velocity | Проверить IsPlayerMoving() |
