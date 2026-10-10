# Ресурсы и происхождение

## Код

- Three.js 0.180.0 и addons: MIT, текст лицензии vendor/LICENSE-three.txt. Модули сохранены локально. [Официальный репозиторий](https://github.com/mrdoob/three.js).
- Код игры, геометрия квартиры и окружения, мебельная стенка, бытовые предметы, мини-игра, текст и синтезируемый звук созданы в этом проекте.

## Модели и материалы Poly Haven

[Политика лицензирования](https://polyhaven.com/license): ресурсы распространяются по CC0. Источники каждой папки и использование перечислены в assets/polyhaven-manifest.json. Загружались варианты glTF/текстур 1K с официального api.polyhaven.com, включая зависимые .bin и карты материалов.

- Television_01, Sofa_01, CoffeeTable_01, vintage_cabinet_01 — гостиная.
- potted_plant_02 — растение на окне.
- electric_stove, vintage_electric_kettle, SchoolChair_01, wall_clock — кухня.
- wood_floor_worn, wood_table_001 — фотографические материалы пола и дерева; используются также карты нормалей.
- WoodenChair_01 — скачанный вариант, не используется в сцене; оставлен в рабочей папке, помечен used:false.

Модели масштабировались и размещались под планировку, у телевизора добавлен наш динамический экран. Материалы из игр-референсов не извлекались и не распространяются.

## Сгенерированные изображения

assets/rug-v2.png и assets/wallpaper-v2.png — отдельные текстуры, созданные инструментом imagegen для проекта. Это не кадры других игр. Ниже сохранены рабочие описания для повторения направления (восстановленные брифы, не гарантированная дословная запись запроса):

**Ковёр:** flat orthographic albedo of a complete vintage Soviet Persian-style wool rug, 3:2 proportions, faded burgundy, beige and navy, intricate vegetal motifs, understated medallion and borders, lovingly maintained, no holes, uniform diffuse illumination; no room, perspective, cast shadows, lettering or logos. В сцене используется на полу и как настенный ковёр.

**Обои:** seamless square flat albedo of carefully maintained 1980s Soviet wallpaper, small floral sprigs and vertical stems, warm ivory paper without yellow-gold cast, muted olive leaves, taupe and terracotta buds, fine paper and ink texture; no room, gradient lighting, shadows, oversized geometric pattern or vector-flat appearance. В сцене масштаб задаётся по размерам стен.

Исходные результаты генерации хранятся также в C:/Users/User/.codex/generated_images/01a12619-054a-7b43-af10-11952a2ab009/ (exec-25f686cd-c01f-49bd-8cf7-b4c3331af489.png и exec-d836b667-2ac7-42cc-b37d-ba7f5301d906.png). Для запуска достаточно копий в assets/.

## Чего здесь нет

Музыка, записи голосов, логотипы PSP/Dendy и коммерческие игровые ассеты не копировались. Портативная мини-игра и телепередача оригинальные. Ссылки и наблюдения по чужим играм находятся в исследовании; их скриншоты не встроены в распространяемую игровую сцену.
