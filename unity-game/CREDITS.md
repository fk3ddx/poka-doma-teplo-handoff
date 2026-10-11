# Происхождение ресурсов Unity M0 / M1

## Авторские ресурсы проекта

C#-системы, шейдеры, планировка и мебель созданы для этого проекта. Мебель хранится как редактируемый `tools/art/sources/ApartmentFurniture.blend`; генератор `tools/art/build_furniture.py` экспортирует вычисленную геометрию в JSON, а редактор Unity создаёт Mesh assets и UV2. Blender 4.5.14 LTS получен с официального сайта и проверен по SHA256. Сам Blender не включается в архив игры.

Ковёр, обои, ткань и вымышленная семейная фотография сгенерированы встроенным imagegen. Полные запросы — `tools/art/generated-textures.md`. Это UV-ресурсы, а не подменённые скриншоты игры. Фотография не изображает известных или реальных родственников пользователя.

Календарь, вымышленная газета, номера квартир, надпись на лестнице и корешки книг набраны скриптами `make_prints.py` / `make_book_labels.py`. Используются установленные в Windows шрифты Arial / Times New Roman для растеризации; файлы шрифтов не распространяются. Названия авторов на книгах — простая типографика, обложки изданий не копируются. Дата и новости — реквизит художественной истории.

Экран восьмибитной приставки нарисован кодом для проекта: вымышленная лесная игра на паузе. Ресурсы Nintendo и коммерческих игр не используются.

Музыкальная миниатюра радиоточки, шум погоды, шаги и тиканье синтезированы `tools/art/make_audio.py` с нуля. В M1 нет чужих записей, сэмплов или голосов из браузерной v3 с неопределённой лицензией. Это предварительное звуковое оформление, его художественная приёмка ещё не проведена.

## Poly Haven — CC0

Официальные ресурсы, загруженные через публичный API с отдельным User-Agent. Лицензия: [Poly Haven CC0](https://polyhaven.com/license). Точные URL, MD5 и локальные пути каждого файла — `Assets/_Project/Art/CC0/provenance.json`. MD5 проверен при скачивании. Повторяемый загрузчик — `tools/art/download_resources.py`.

Использованы:

- [dark_wood](https://polyhaven.com/a/dark_wood) — дерево;
- [herringbone_parquet](https://polyhaven.com/a/herringbone_parquet) — паркет;
- [quatrefoil_jacquard_fabric](https://polyhaven.com/a/quatrefoil_jacquard_fabric) — часть тканей;
- [plastered_wall_02](https://polyhaven.com/a/plastered_wall_02) — наружные поверхности;
- [clay_plaster](https://polyhaven.com/a/clay_plaster) — штукатурка потолка;
- [ceramic_vase_01](https://polyhaven.com/a/ceramic_vase_01) — керамика;
- [covered_car](https://polyhaven.com/a/covered_car) — машины под чехлом во дворе;
- [painted_wooden_bench](https://polyhaven.com/a/painted_wooden_bench) — дворовые скамейки.

Также исследовались `mid_century_lounge_chair` и `book_encyclopedia_set_01`; они не используются в итоговой сцене M1. Кресло заменено авторской моделью, более подходящей обстановке.

## Движок и референсы

Проект создан на официальном шаблоне Unity `com.unity.template.3d-cross-platform` 17.0.14; используется Unity 6000.3.26f1, URP 17.3.0. Зависимости и их версии перечислены в `Packages/`; лицензии поставляются с пакетами Unity.

Fears to Fathom: Norwood Hitchhike / Home Alone и From the Darkness служат художественными референсами. Официальные скриншоты Steam просмотрены для анализа, локальный каталог `Reference/` исключён из Git и сборки. Страница `QA/compare.html` использует локальные исследовательские копии либо официальные URL Steam с атрибуцией. Графика, модели, звуки и персонажи этих игр не входят в игровые Assets. Полное прохождение всех референсов не заявляется.

Дополнение двора: [asphalt_02](https://polyhaven.com/a/asphalt_02) и [asphalt_snow](https://polyhaven.com/a/asphalt_snow), CC0. Источники и хеши также внесены в provenance.json.


## Дополнение M1 0.2.1
- Poly Haven `waffle_pique_cotton`, CC0: https://polyhaven.com/a/waffle_pique_cotton . Сохранены исходные Diffuse/Normal/Roughness и MD5 в provenance.json. Для белого белья используются нормали и шероховатость сканированной ткани с однородным светлым albedo; жёлтый Diffuse не используется в материале.
- Будильник, очки, покрывало, швейный уголок, чемодан и вещи прихожей: собственная параметрическая геометрия `tools/art/add_domestic_detail.py`.

- `CrochetTulle.png`: авторский процедурный узор сетки и цветочных медальонов (`ApartmentDomesticMaterials.cs`), не скачанный орнамент.
- Репродукция иконы: The Metropolitan Museum of Art, *Icon of the Virgin and Child, Hodegetria variant*, XIII век, object 831188, Public Domain / Open Access. https://www.metmuseum.org/art/collection/search/831188 . Исходное музейное изображение без перерисовки, метаданные API сохранены рядом с JPG. В квартире изображена современная домашняя репродукция. Исследовательский снимок оклада 205079 тоже Public Domain, в игровую сцену не включён.


## Дополнение M1 0.2.2
- Ящик, чаши и накидка — собственная геометрия tools/art/finish_domestic.py.
- DistantWindowCurtain.png — оригинальный процедурный рисунок занавески, ApartmentMaterialPolish.cs.
- comparison.zip содержит пять официальных кадров Steam только для атрибутированного сравнения; они не входят в игру и права на них принадлежат их авторам.


M1 0.2.3: колени/плед — собственная процедурная геометрия ApartmentSeating.cs; SleetParticle.png — оригинальная процедурная маска мягкой частицы, ApartmentWeather.cs.


M1 0.2.4: FamilySnapshotsAtlas.png (вымышленные люди) и QuietWalnutVeneer.png созданы built-in image_gen. Полные запросы и SHA-256: tools/art/personal-materials-prompts.json. Снимки настоящего плеера не редактировались.


M1 0.2.5: soft_textiles.py — собственная процедурная геометрия мягких подушек/швов; CrochetTulle.png — собственный процедурный рисунок с плавной прозрачностью. Новых внешних ассетов или генераций imagegen в этом проходе нет.
