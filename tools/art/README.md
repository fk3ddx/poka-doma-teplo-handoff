# Текущий персонаж: готовая Granny Katcho

`export_katcho.py` конвертирует одобренную готовую модель из `art-sources/Katcho-Granny` в FBX. Оригинальные геометрия, UV, 256px текстура, одежда и анимации не переделываются. Исключён второй неподвязанный демонстрационный экземпляр трости. При экспорте назначается action_slot (старый .blend иначе даёт неподвижные клипы) и отключается мешающая NLA-дорожка; FBX_SCALE_ALL сохраняет единицы скелета Unity. Запуск: Blender --background --factory-startup --disable-autoexec --python tools/art/export_katcho.py.

Unity: `Ataka.Editor.KatchoGrannyBuilder.Build` создаёт отдельную сцену просмотра, prefab и Windows-билд. Основная Flat.unity не перезаписывается.

## Архив отвергнутого направления

`create_nina.py` и описанная ниже MPFB-примерка **отвергнуты Димой**, не продолжать её и не подменять ею Katcho.

# Художественные исходники

`create_nina.py` — воспроизводимая MPFB-примерка, не генератор мебели из примитивов. Создаёт существующую MakeHuman-топологию, подгоняет готовую одежду/глаза/волосы/очки, запекает позу и экспортирует FBX.

Нужно: Blender 4.5.x; официальный MPFB 2.0.18 в `ImportInbox/MPFB/mpfb`; пакеты MakeHuman System Assets, Dress01, Glasses01 распакованы в `ImportInbox/MPFB/assetdata`. Источники и лицензии в CREDITS.md. Полные входные архивы не версионируются.

Запускать Blender с отдельным BLENDER_USER_RESOURCES, указывающим на `ImportInbox/MPFB/blender-profile`, и параметрами `--background --factory-startup --disable-autoexec --python tools/art/create_nina.py`. Скрипт не сохраняет пользовательские настройки Blender. Экспорт уже лежит в Assets, для обычной Unity-сборки Blender/MPFB не требуются.

`prepare_commie.py` и `prepare_sketchfab.py` — журнал локальной подготовки готовых предметов: пути U:/ соответствуют корню проекта на этой машине. Они пока требуют адаптации путей на другой машине; для Unity нужны только уже экспортированные FBX/материалы.

Все нейтральные Cycles-рендеры являются технической примеркой. Доказательством игрового вида служат кадры Windows-билда в Screenshots/nina, а не Cycles.
