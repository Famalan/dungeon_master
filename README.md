# 3D Roguelike Dungeon Crawler

Unity 6 / URP проект: roguelike dungeon crawler от первого лица с процедурной генерацией уровней, комнатами-аренами, ближним и дальним боем, лутом, артефактами и ростом сложности между уровнями.

Игровой цикл: начать забег -> зачистить процедурное подземелье -> собрать монеты и сундуки -> найти выход -> выбрать артефакт между уровнями -> перейти глубже. При смерти игрок возвращается на экран Game Over и может начать заново.

## Текущее состояние

- Версия Unity: `6000.4.0f1`.
- Рендер: Universal Render Pipeline `17.4.0`.
- Основная сцена: `Assets/Scenes/SampleScene.unity`.
- Основная команда подготовки сцены: `Tools > Build Game`.
- Проект закрывает требования учебного задания: нетривиальные механики, меню, оптимизация, встроенные Unity-инструменты и кастомные Editor-инструменты.

## Основные механики

### Процедурное подземелье

- `DungeonGenerator` размещает случайные комнаты на сетке с проверкой пересечений и отступами.
- Стартовая комната выбирается ближе к началу сетки, выход - как самая дальняя комната по Manhattan distance.
- Комнаты соединяются L-образными коридорами, после чего строится `FloorMap`.
- `DungeonBuilder` на лету создаёт пол, стены, потолок, факелы, выход, сундуки, декор и `NavMeshSurface`.
- Враги не спавнятся в стартовой комнате и ближайших безопасных комнатах; плотность врагов растёт с расстоянием от старта.
- У выхода появляется усиленный `ExitGuardian`.

### Запечатывание комнат

- При входе в комнату с живыми врагами `RoomSealZone` блокирует проходы временными стенами.
- Игрок не может убежать из комнаты, пока не зачистит её.
- После смерти всех врагов барьеры исчезают, а HUD показывает уведомление о зачистке.
- Стартовая комната не запечатывается.

### Движение и бой

- FPS-управление через New Input System: движение, мышь, прыжок, спринт и dash.
- Dash на `Q`: рывок на 6 юнитов за 0.15 сек, кулдаун 1.5 сек, i-frames 0.2 сек.
- Ближнее оружие через `MeleeWeaponController`: меч, топор, копьё, молот и лук.
- Переключение оружия: `1` / `2`.
- `ProjectileShooter` поддерживает режимы выстрела: стандартный снаряд, дробь, медленная сфера и луч.
- Попадания дают урон, VFX, звук, floating combat text и при необходимости AoE-взрыв.

### Враги

| Тип | Роль | Поведение |
|-----|------|-----------|
| `Grunt` | быстрый ближний враг | Патрулирует, преследует игрока, атакует в ближнем бою |
| `Ranger` | дальний враг | Держит дистанцию, отступает при сближении, стреляет снарядами |
| `Tank` | тяжёлый враг | Медленный, живучий, наносит высокий урон вблизи |

Дополнительно `EnemySpawner` может сделать врага элитным: повышается здоровье, масштаб и шанс дополнительного лута. Вероятность элиты растёт с уровнем, но ограничена сверху.

### Лут, сундуки и прогрессия

- Враги с шансом 50% роняют монеты; шанс увеличивается бонусами игрока.
- Элитные враги могут уронить дополнительную монету.
- На уровне появляется 1-3 сундука в обычных комнатах; сундуки дают монеты и учитывают бонусы артефактов.
- Между уровнями игрок выбирает 1 из 3 артефактов.
- Монеты тратятся на реролл артефактов и лечение между уровнями.

### Артефакты и синергии

Артефакты действуют только в текущем забеге и сбрасываются при новой игре. У каждого артефакта есть редкость, цвет, иконка и тег.

| Тег | 2/4 синергия | 4/4 синергия |
|-----|--------------|--------------|
| `Flame` | Бонус к урону | Бонус к радиусу взрывов |
| `Shadow` | Быстрее dash | Дольше i-frames |
| `Gold` | Больше шанс лута | Больше монет из сундуков и дешевле реролл |
| `Vitality` | Больше максимальное HP | Дополнительное лечение в начале уровня |
| `Momentum` | Выше скорость движения | Быстрее стрельба |

В проекте также остался `PerkSystem` как fallback-слой: если `ArtifactSystem` недоступен, экран выбора может показать классические перки.

## Интерфейс и обратная связь

- Главное меню: `Start Game`, `Options`, `Quit`.
- Options: чувствительность мыши и громкость, сохранение через `PlayerPrefs`.
- HUD: здоровье, уровень, монеты, готовность dash, текущий режим оружия, уведомления о запечатывании.
- Визуальная обратная связь: low HP vignette, damage flash, combat text, kill streak UI.
- Навигация: стрелка-компас к выходу, опциональная миникарта, прицел.
- Звук: музыка меню/подземелья, ambient loop, stingers, шаги, попадания, подбор монет, клики UI.

## Покрытие требований

| Требование | Реализация |
|-----------|------------|
| 1-2 нетривиальные механики | Room Sealing, dash с i-frames, система артефактов с синергиями |
| Меню игры | Главное меню, Options, Game Over, межуровневый выбор артефакта |
| Встроенные Unity-инструменты | URP, Cinemachine 3.x, Particle System, AI Navigation, New Input System |
| Кастомный инструмент | `Tools > Build Game`, `Tools > Dungeon Preview` |
| Оптимизация | Static/Dynamic Batching, `isStatic`, Occlusion Culling flags, GPU Instancing, Fog, baked GI |
| Отчётность | README описывает механику, запуск, структуру и соответствие требованиям |

## Быстрый старт

1. Открыть проект в Unity `6000.4.0f1`.
2. Открыть сцену `Assets/Scenes/SampleScene.unity`.
3. Запустить `Tools > Build Game`.
4. Дождаться настройки импортов, создания сцены, применения освещения и оптимизаций.
5. Нажать `Play`.

`Tools > Build Game` выполняет полный bootstrap: настраивает импорт FBX, Kenney UI и KayKit assets, создаёт материалы, врагов, оружие, снаряды, VFX, факелы, UI, ссылки между объектами, освещение и оптимизации. Если Unity перезагрузит скрипты после реимпорта, сборка продолжится автоматически.

## Управление

| Действие | Клавиша |
|----------|---------|
| Движение | `WASD` или стрелки |
| Камера | Мышь |
| Атака | ЛКМ или `Enter` |
| Предыдущее оружие | `1` |
| Следующее оружие | `2` |
| Прыжок | `Space` |
| Спринт | `Left Shift` |
| Dash | `Q` |

## Editor-инструменты

### `Tools > Build Game`

Полностью собирает игровую сцену: ассеты, материалы, префабы, врагов, игрока, UI, VFX, освещение и оптимизации.

### `Tools > Dungeon Preview`

Окно предпросмотра генерации подземелья без запуска Play Mode. Позволяет менять размер сетки, количество комнат, размеры комнат и padding.

### Lighting и Optimization setup

`LightingSetup` и `OptimizationSetup` вызываются из `Tools > Build Game`. Они настраивают ambient/fog/post-processing, batching, статические флаги, occlusion flags и GPU instancing.

## Структура проекта

```text
Assets/Scripts/
  AI/       EnemyAI, EnemySpawner
  Combat/   HealthSystem, Projectile, ProjectileShooter, MeleeWeaponController, loot/VFX helpers
  Core/     GameManager, LevelManager, PlayerStats, GameEvents, AudioManager, ArtifactSystem, PerkSystem
  Dungeon/  DungeonGenerator, DungeonBuilder, RoomSeal*, ExitPortalTrigger, LootChest, decor layer
  Player/   PlayerController, camera follower, dash and muzzle VFX
  UI/       GameUI, CrosshairUI, MinimapUI, ExitCompassUI, CombatTextManager, KillStreakUI
  Editor/   Build Game, Dungeon Preview, lighting/optimization setup, Edit Mode tests

Assets/ExternalAssets/
  BrokenVectorDungeon/
  KayKitFantasyWeaponsBits/
  KenneyImpact/
  KenneyMiniDungeon/
  KenneyRPG/
  KenneyUIPack/
```

## Тесты

В проекте есть Edit Mode тесты на ключевые системы:

- `ArtifactSystemEditModeTests`
- `PlayerControllerEditModeTests`
- `MeleeAndPlacementEditModeTests`
- `DungeonGenerationPolishEditModeTests`
- `DungeonBuilderAssetEditModeTests`
- `UserRequestedVisualPolishEditModeTests`

Запуск: Unity Test Runner -> Edit Mode.

## Зависимости

| Пакет | Версия |
|-------|--------|
| Unity | `6000.4.0f1` |
| Universal RP | `17.4.0` |
| Input System | `1.19.0` |
| Cinemachine | `3.1.3` |
| AI Navigation | `2.0.11` |
| Unity Test Framework | `1.6.0` |
| Visual Effect Graph | `17.4.0` |

## Asset Credits

- Ultimate Low Poly Dungeon by Broken Vector / Thane5, CC-BY 4.0: https://brokenvector.itch.io/ultimate-low-poly-dungeon and https://github.com/Thane5/dungeon-assets
- Kenney asset packs, CC0: Impact Sounds, Mini Dungeon, RPG Audio, UI Pack.
- KayKit Fantasy Weapons Bits, CC0.
