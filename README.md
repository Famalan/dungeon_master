# 3D Roguelike Dungeon Crawler

Unity 6 (URP) проект -- roguelike dungeon crawler с процедурной генерацией подземелий, тремя типами врагов, системой лута и перков.

## Реализованные механики

### 1. Процедурная генерация подземелий
- Алгоритм Random Room Placement: случайное размещение комнат на сетке с проверкой пересечений (AABB)
- L-образные коридоры между комнатами (3 клетки шириной)
- Стартовая комната ближе к углу (0,0), выход -- максимально далеко (Manhattan distance)
- Перегенерация если расстояние между стартом и выходом < gridWidth/3
- Враги спавнятся по зонам:
  - Стартовая комната: 0 врагов
  - Ближние (dist < 15): 0-1 врагов
  - Средние (15-30): 2-3 врага
  - Дальние (30+): 3-5 врагов
  - Выход: 4-6 врагов
- Сложность растёт с каждым уровнем

### 2. Запечатывание комнат (Room Sealing) -- нетривиальная механика
- При входе в комнату с врагами проходы блокируются красными стенами
- Игрок вынужден сражаться -- убежать невозможно
- После убийства всех врагов стены исчезают и проходы открываются
- Стартовая комната не запечатывается
- Реализовано через триггер-зоны (`BoxCollider` + `RoomSealZone`) с отслеживанием живых врагов

### 3. Dash с неуязвимостью (i-frames) -- нетривиальная механика
- Клавиша **Q** -- рывок в направлении движения (или вперёд если стоишь)
- Дистанция: 6 юнитов за 0.15 сек
- Во время рывка игрок неуязвим (i-frames = 0.2 сек)
- Кулдаун: 1.5 сек
- `HealthSystem.TakeDamage()` проверяет `PlayerController.IsInvulnerable` перед нанесением урона
- Позволяет уклоняться от снарядов Rangers и AoE атак

### 4. Боевая система
- Прямые снаряды -- летят по `transform.forward` от точки выстрела
- При попадании -- взрыв с Area of Effect уроном
- VFX: частицы взрыва, подсветка, тряска камеры через Cinemachine Impulse
- Система владельца снаряда: игрок не вредит себе, враги-Rangers стреляют в игрока

### 3. Три типа врагов
| Тип | Цвет | HP | Скорость | Урон | Поведение |
|-----|-------|-----|---------|------|-----------|
| Grunt | Красный | 30 | 4 | 8 | Ближний бой |
| Ranger | Фиолетовый | 20 | 3 | 12 | Стреляет на расстоянии 8 |
| Tank | Зелёный | 80 | 2 | 20 | Ближний бой, размер 1.4x |

- Выбор типа по расстоянию от старта: ближние -- Grunt, средние -- Grunt/Ranger, дальние -- все типы
- AI: патруль → обнаружение → преследование/атака → потеря цели
- Ranger отступает при сближении и стреляет снарядами

### 4. Система лута
- Враги дропают монеты (золотые сферы) при смерти с шансом 50%
- Монеты поднимаются при приближении к ним (radius 2)
- Визуальный эффект: покачивание + вращение
- Перк "Scavenger" увеличивает шанс дропа

### 5. Система перков
Между уровнями игрок выбирает 1 из 3 случайных перков:
| Перк | Эффект |
|------|--------|
| Thick Skin | +20 Max HP |
| Quick Hands | +25% Fire Rate |
| Power Shot | +30% Damage |
| Swift Feet | +15% Move Speed |
| Scavenger | +25% Loot Chance |
| Regeneration | +10 HP per level |

Перки стакаются -- можно выбрать один и тот же несколько раз.

## Встроенные инструменты Unity

### Particle System / VFX
- Эффект взрыва при попадании снаряда (частицы, свет, ударная волна)
- Подсветка летящего снаряда (Point Light)

### Cinemachine 3.x
- `CinemachineCamera` -- виртуальная камера от первого лица
- `CinemachineBrain` -- управление основной камерой
- `CinemachineImpulseSource` + `CinemachineImpulseListener` -- тряска камеры при взрывах
- Пакет: `com.unity.cinemachine` (v3.1.3)

### Запекание освещения
- Mixed Lighting: запечённый ambient + realtime точечные источники от факелов
- Факелы с кронштейнами прикреплены к стенам комнат (не летают в воздухе)
- Освещение коридоров: факелы каждые 8 клеток
- Fog для глубины подземелья
- Volume с Bloom, Vignette, Color Adjustments

### AI Navigation
- NavMeshSurface строится процедурно после генерации подземелья
- NavMeshAgent на врагах для патрулирования и преследования
- Пакет: `com.unity.ai.navigation`

## Кастомные Editor-инструменты

### Tools > Dungeon Preview
Окно для предпросмотра генерации подземелий без запуска игры. Позволяет настраивать параметры (размер сетки, количество комнат, размеры) и мгновенно видеть результат.

### Tools > Setup Game Scene
Создаёт все необходимые GameObject'ы в сцене одним нажатием кнопки "DO EVERYTHING": GameManager, Player с Cinemachine камерой, UI Canvas с главным меню, HUD, настройками, панелью перков и Game Over.

### Tools > Setup Lighting
Настраивает атмосферное освещение подземелья: тёмный ambient, fog, Volume с пост-обработкой.

### Tools > Optimization Setup
Инструмент для оптимизации производительности:
- **Static Batching** -- объединение статических мешей для уменьшения draw calls
- **Dynamic Batching** -- объединение мелких динамических объектов
- **Occlusion Culling** -- пропуск рендеринга объектов за стенами
- **GPU Instancing** -- включение на всех материалах
- Кнопка "APPLY ALL" для применения всех оптимизаций одним нажатием

## Оптимизация

| Техника | Реализация |
|---------|-----------|
| Static Batching | Стены, полы, потолки помечены `isStatic = true` |
| Dynamic Batching | Включён в Player Settings |
| Occlusion Culling | Флаги Occluder/Occludee на статических объектах |
| Frustum Culling | Встроен в Unity (включён по умолчанию) |
| GPU Instancing | Включён на материалах подземелья |
| Light Baking | Mixed Lighting + baked ambient |
| Fog | Exponential fog для оптимизации отдалённых объектов |

## Быстрый старт

### 1. Открыть проект в Unity 6
Версия: Unity 6000.4.0f1

### 2. Подождать импорт пакетов
Cinemachine и AI Navigation установятся автоматически из `manifest.json`.

### 3. Настроить сцену (один клик)
1. **Tools > Setup Game Scene > DO EVERYTHING** -- создаст материалы, 3 типа префабов врагов, снаряд, факел, взрыв, объекты сцены и назначит все ссылки автоматически
2. **Ctrl+S** для сохранения сцены

### 4. Настроить освещение
1. **Tools > Setup Lighting > Apply Dungeon Lighting Settings**
2. **Tools > Setup Lighting > Create Ambient Volume**

### 5. Применить оптимизации
1. **Tools > Optimization Setup > APPLY ALL OPTIMIZATIONS**

### 6. Запустить
Play -- появится главное меню. Кнопки: Start Game, Options, Quit.

## Управление

| Действие | Клавиша |
|----------|---------|
| Движение | WASD |
| Камера | Мышь |
| Стрельба | ЛКМ |
| Прыжок | Пробел |
| Спринт | Shift |
| Рывок (Dash) | Q |

## Настройки (Options)

В главном меню доступна кнопка **Options**:
- **Mouse Sensitivity** -- чувствительность мыши (0.5 - 10.0)
- **Volume** -- громкость звука (0% - 100%)

Настройки сохраняются между сессиями через PlayerPrefs.

## Структура проекта

```
Assets/Scripts/
  Core/        -- GameManager, PlayerStats, PerkSystem
  Dungeon/     -- DungeonGenerator, DungeonBuilder, RoomData, CorridorData, DungeonTorch, RoomSealManager, RoomSealZone
  Player/      -- PlayerController (FPS движение + стрельба)
  Combat/      -- Projectile, ProjectileShooter, HealthSystem, ExplosionVFXTrigger, LootDrop, EnemyDeathHandler
  AI/          -- EnemyAI (NavMesh: 3 типа врагов), EnemySpawner
  UI/          -- GameUI (меню + настройки + перки), CrosshairUI, MinimapUI
  Editor/      -- DungeonPreviewWindow, GameSceneSetup, LightingSetup, OptimizationSetup
```

## Технологии

- Unity 6000.4 (URP 17.4)
- New Input System
- NavMesh (AI Navigation)
- Particle System
- Cinemachine 3.x (CinemachineCamera, Impulse)
- Mixed Lighting + Light Baking
- Static/Dynamic Batching + Occlusion Culling + GPU Instancing
