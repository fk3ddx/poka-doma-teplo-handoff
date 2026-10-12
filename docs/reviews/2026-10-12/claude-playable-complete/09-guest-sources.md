# 09 — Гостья из любого доступного источника; когти из её же руки

Sketchfab у вас заблокирован (`ERR_BLOCKED_BY_CLIENT`, защиту не трогаем). Поэтому `prepare_guest.py` теперь берёт любой скачиваемый лицензированный файл (`.glb`, `.gltf`, `.fbx`, `.dae`). Руки для крупного плана он вырезает из самого существа по весам скина: правая кисть и предплечье в bind pose. Отдельная модель руки не нужна.

```
blender --background --python tools/art/prepare_guest.py -- <файл тела> --source <url> --author <автор> --license <CC0|CC-BY-4.0> --claw from-body
```
Результат пишется в `Art/ReadyModels/Sketchfab/GuestBody|GuestClaw`, затем `Ataka.Editor.GuestInstaller.Install`. У риггованного тела трансформы, rest pose и bind poses не трогаются: масштаб задаёт Unity.

## Кандидаты с прямой загрузкой (не Sketchfab) — сверить лицензию на странице
1. **PSX Horror Entity** — puck_psx, CC BY, rigged, Creature.zip 3.2 МБ, «name your own price» (можно 0): https://puszke.itch.io/psx-horror-entity-free. PSX-стиль ближе всего к FtF. Кредит: «puck_psx».
2. **3D Horror Game Monster** — OpenGameArt, CC0, с анимациями бега, атаки и ходьбы, 3 расцветки: https://opengameart.org/content/3d-horror-game-monster (Poses.zip / Colors.zip).
3. **Katcho Free_Character.zip** (57 МБ, CC0) — тот же автор, что бабушка: https://katcho.itch.io/free-characters. Стоит открыть: там могут быть ещё персонажи, например Валя или соседи.

Решение по внешнему виду за Димой: показать ему превью 1 и 2. До установки слоты остаются **не принятыми**, в лог пишется предупреждение.
Если по превью видно, что пальцы перепутаны или правой руки нет — использовать `--claw <файл руки>`.
