
## Художественная партия 2026-10-11 (не вся интегрирована в сцену)

Все пять моделей Sketchfab получены через официальный Download GLB 1K, лицензия CC Attribution 4.0 проверена в окне загрузки. [Текст CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

- **Phone VEF TA 68**, strnroman / mne1pox — https://sketchfab.com/models/c848036e1d5f455797476659dfd12b06 . Изменено: масштаб, экспорт в FBX, уменьшена плотность части сетки, выделены авторские текстуры.
- **House slippers**, headless_christ — https://sketchfab.com/models/ac25fc96e7ff4c138d8503b1072b7ea3 . Изменено: масштаб, FBX, выделены текстуры. Разделение пары ещё не завершено.
- **Old Soviet Wardrobe**, dashkilya — https://sketchfab.com/models/ad0ab8f9757640d6b88fa9318d2298cb . Изменено: масштаб, FBX, выделены текстуры; сценовая интеграция не завершена.
- **Cabinet with mirror**, VVMGames — https://sketchfab.com/models/734b7c6aba6343518c89008ece0e58f0 . Изменено: масштаб, FBX, выделены текстуры; несовпадение трюмо отмечено в ASSET-SHOPPING.
- **Antique Wall Pendulum Clock**, Odin Branigan — https://sketchfab.com/models/a1399c9b33554b9a9d25ec9e5fbdd5ca . Изменено: масштаб 0.70 м, FBX, выделены текстуры; отдельные стрелки/маятник сохранены.
- **Commie Block Apartment Props**, GrawlixMcLaren — https://grawlixmclaren.itch.io/low-poly-apartment-props-ps1ps2 . **CC0**, подтверждение автора https://itch.io/post/15507819 («cc0, go crazy»), проверено 2026-10-11. Изменено: выделены существующие группы мебели, нормализован масштаб, FBX, URP-материалы. Пробный масштаб build16 требует исправления.
- **MakeHuman system basemesh, old_caucasian_female skin, low-poly eyes, ponytail01, eyebrow001** — MakeHuman Community / Data Collection AB, Joel Palmius, Jonas Hauquier. **CC0**, https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html . Адаптация: MPFB 2.0.18, параметры возраста/телосложения, рост 1.55 м, уменьшение полигонов, статическая поза, седой цвет волос через запекание материала, URP. Программный аддон MPFB распространяется отдельно под GPL; это не лицензия экспортированной модели.
- **F_Kimono / Mindfront Kimono**, Mindfront — **CC0**, https://static.makehumancommunity.org/assets/assetpacks/dress01.html . Готовая одежда для примерки: подогнана MPFB, упрощена, материал затемнён. Не финальный цветочный халат.
- **Optical glasses**, kwnet_at — **CC0**, https://static.makehumancommunity.org/assets/assetpacks/glasses01.html . Подогнаны к голове, упрощены, оправа затемнена в материале.

Примерка Нины — художественный прототип, не принятая внешность. В варианте A платок/фартук/цепочка отсутствуют; исходник с параметрами и скелетом хранится локально в ImportInbox/MPFB. Анимации и деформации ещё не проверены.


### Примерка Нины, вариант B

- **elvs_simple_60s_dress, elvs_ladies_apron**, Elvaerwyn — CC-BY, [Dress02](https://static.makehumancommunity.org/assets/assetpacks/dress02.html). Существующее платье удлинено, авторская цветочная текстура apron2 применена также к платью; сетки упрощены, фартук подогнан к платью. Не окончательный халат с застёжкой.
- **elvs_charity_veil1**, Elvaerwyn — CC-BY, [Hats03](https://static.makehumancommunity.org/assets/assetpacks/hats03.html). Готовая ткань сужена у щёк, изменён тон материала. Пока это свободное покрытие головы, не окончательный платок с узлом.
- **toigo_round_glasses_leopard**, Margaret Toigo / MRT (автор в mhclo: MRT) — CC0, [Glasses01](https://static.makehumancommunity.org/assets/assetpacks/glasses01.html). Упрощены; удалены непрозрачные линзы, оправа затемнена.
- **joepal_crude_high_socks**, Joel Palmius — CC0, [Underwear04](https://static.makehumancommunity.org/assets/assetpacks/underwear04.html). Подогнаны MPFB, упрощены, экспортированы в FBX.

Системная возрастная кожа, глаза, волосы и базовая анатомия — те же CC0 MakeHuman, что выше. В B изменены макропараметры и готовые morph targets, удалён хвост волос, восстановлено сглаживание нормалей. Это адаптация готовых авторских мешей. Лицензии одежды указаны как в исходных mhclo; номер версии CC-BY в этих заголовках не указан.


### Готовая Granny Katcho — кандидат после отказа от MPFB

[Free Characters](https://katcho.itch.io/free-characters), Katcho, CC0. Оригинальные Granny.blend, Granny.dae, granny.png не изменены, лицензия из архива сохранена рядом в art-sources/Katcho-Granny/LICENSE.txt. В игру пока не интегрированы. Иллюстрация docs/references/katcho-granny-author-preview.png — снимок авторского превью, не наш игровой кадр.

### 2026-10-11 — Katcho выбран Димой и импортирован

Granny Katcho одобрена по авторскому превью («да норм»). Исходные модель/атлас сохранены без переделки лица, одежды, UV и топологии. Перенос: FBX, URP Lit, равномерный масштаб до 1.55 м; исключена дублирующая неподвязанная трость. Сохранены 10 авторских анимаций. Код экспорта: tools/art/export_katcho.py. Отдельный Unity-prefab и Windows-просмотр; в сюжет пока не встроена. Старые MPFB-примерки выше — отвергнутый архив.
