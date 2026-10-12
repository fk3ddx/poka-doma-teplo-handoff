# 05 — Готовые модели сюжетных предметов

Код: `tools/art/prepare_story_props.py` (Blender) → `Art/ReadyModels/Story/<id>/` → `Ataka.Editor.StoryPropsInstaller.Install` ставит их в сцену как `STORY <id>`. `StoryDirector` показывает, прячет и переносит предметы по ходу сцен. Если модели нет, она пропускается — игра работает.

| id | Где в сюжете | Источник | Лицензия |
|---|---|---|---|
| Kettle | 4.3: взять, налить, на огонь; 11.7 бабушка ставит чайник | «Classic Enamel Kettle», Yevhenii3d — https://sketchfab.com/3d-models/a419d3acf51c41b89137e2c2187c0efa → `classic_enamel_kettle.glb` | CC-BY 4.0 |
| Belt | 4.5 в шифоньере, 8.2 взять — на балконе лентой | «leather belt», Cyril43 — https://sketchfab.com/3d-models/8cba1fb2d0b6442cb0b7aa903025676c → `leather_belt.glb` | CC-BY 4.0 |
| JamJar | 4.3: банка на балконе, потом на столе | «Casual Jar of Raspberry Jam», incg5764 — https://sketchfab.com/3d-models/61bc4a8ef3184b169ab368946fcf2135 → `casual_jar_of_raspberry_jam.glb` | CC-BY 4.0 |
| Matchbox | 4.3: спички на ЗиЛе | «Matchbox (Коробок спичек)», poems_obj — https://sketchfab.com/3d-models/28007022e93d44848c67623ecfcc0737 → `matchbox.glb` | CC-BY 4.0 |
| Bread | 4.2: батон из сумки | «Bread Loaf / Буханка хлеба», Igrodev — https://sketchfab.com/3d-models/daf4801f72ae4f9eba9c338afbbb2926 → `bread_loaf.glb` | CC-BY 4.0 |
| Shawl | 4.5: платок в шифоньере | «Cozy Winter Shawl», AwesomeAmbz — https://sketchfab.com/3d-models/9ab54de829bb4800b80992e76af96ddc → `cozy_winter_shawl.glb` | CC-BY 4.0 |
| Bulb | 4.4: новая лампочка в бра | Poly Haven `lightbulb_01` (скрипт скачивает сам) | CC0 |
| Cans | 4.2: шпроты | Poly Haven `russian_food_cans_01` (скрипт скачивает сам) | CC0 |

Перед скачиванием проверить лицензию на странице. Каждого CC-BY-автора внести в CREDITS.md.

Проверить по кадрам: масштаб (самая длинная сторона задана в скрипте) и опору на поверхность. Позиции заданы в `StoryPropsInstaller.Places` и в `StoryDirector` (Put/Show) координатами плана; поправить по кадрам.
