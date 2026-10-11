# Исполнение review05 — промежуточный статус

- Оригинал README/изображений сохранён из ZIP с проверенным SHA618c4a4d2c40b10300c1df776d05fb49d6c3dead56afb42fadbf6b173afd460c.
- Изолированная погоня после исправления setup прошла: Codex d5b95ee, bedroom05-chase-setupfix.
- Расстановка тумбочки выполнена, SCENARIO обновлён. Это прежний временный Kenney; художественный пакет06 ещё не установлен.
- Guard двери на OverlapBox не прошёл runtime-проверку неподвижного героя, несмотря на4 зелёных EditMode-теста. Исходные кадры/лог: Screenshots/scenario/bedroom06-door-probe. Не выдавать за исправленный P0.
- Следующая реализация должна считать расстояние до створки непосредственно из текущих transform капсулы/двери, с2см зазором и проверкой промежуточных углов.
- Runtime-прогоны с GUI отложены по прямому запрету Димы на перехват фокуса: он играет. Все новые визуальные проверки только после разрешения либо доказанного способа без вмешательства.
- Пакет06: красный угол/разрешённая простая лампадка, кружево/A-B без него, мягкие подушки, мягкий мокрый след, предметы на тумбочке. Пока не выполнено.

## Продолжение без окон
- Guard переведён с broadphase OverlapBox на прямое расстояние живой капсулы до ориентированного параллелепипеда створки, запас2см, проверка углов с шагом<=1°. Collider синхронизируется после движения. Компиляция Runtime отдельным csc без запуска Unity прошла; физика в игре НЕ перепроверена.
- Добавлен tools/qa/compile_without_editor.py: использует реальный последний Unity.rsp, отдельный вывод QA/NoWindowCompile, никогда не заменяет DLL в Library/Bee. Это только компиляция исходников.
- Подготовлен скрипт скругления подушек4см, выпуклость5см, маленькое кружево только на верхней части большой задней подушки. FBX переэкспортирован фоновым Blender через CREATE_NO_WINDOW, без GUI; размеры записаны в BedTextiles-geometry.json. Импорт и картинка в Unity НЕ проверены.
- Добавлена диагностическая камера17 без кружева, те же координаты/свет, что16. Пока НЕ снята.
- Проверка исходника LaceRGBA: RGB210–232, у непрозрачных пикселей минимум210; alpha38–255, при cutoff0.4 непрозрачно74.35%. В самой текстуре чёрных нитей нет. Гипотеза, что тёмные пиксели прямо записаны в её албедо, не подтверждается. Причину нужно разделить A/B-кадрами без кружева/проверкой нормалей и света; не объявлять её исправленной по изменению цвета.


## Package06 prepared offline, not installed/accepted
- Downloaded official CC0 alarm_clock_01 FBX, diffuse/normal/ARM; all provider sizes/MD5 verified. Author mesh measured 13.17 cm wide, 17.42 cm tall, so no guessed 20 cm scale. Repacked metallic/smoothness at512 px.
- Exported the explicitly permitted lampada (6 × 7 cm cup, brass rim, three thin supports); geometry manifest saved. Added an explicit ApplyPreparedArt menu command for the corner/clock: shelf top1.62, adjacent icons at45°, flame1.75, halo0.018/range0.5, spot inner40 with accepted color/intensity unchanged. This operation has NOT run. Water glass, spectacles and pills are still absent.
- Wet mark code now uses a multiplicative URP shader with soft irregular edge and three1–2cm satellite drops, leaving the impact ring. Falling drop keeps a separate Lit material. C# compilation passed; shader compilation/rendering not tested.
- Runtime and Editor console compiler reports are attached under offline-checks. They do not replace Unity import, scene/build validation or the failed runtime door probe.
- Browser access to the existing Claude tab still returns HTML where the tool expects JSON; no new browser windows were opened, no fallback native clicks used. FOLLOWUP-PENDING.md is a draft, NOT a delivered message.
