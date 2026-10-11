# Дополнение к разбору 01: только готовые ассеты (11.10.2026)

Это уточнение к §3 README. Правило Димы — **никакой самодельной мебели**.

| Метка | Что значит |
|---|---|
| ✅ ГОТОВЫЙ | авторская модель с источником и лицензией: Poly Haven CC0, Sketchfab CC-BY/CC0, itch CC0. Можно ставить в квартиру. |
| 🧭 ОРИЕНТИР | процедурные меши M1 (`Art/Authored/ApartmentFurniture.meshdata.json`, `Art/M1Meshes`) и мои v3 (`game/src/world/props/*.js`). **Только для композиции**: что где стоит, плотность, ракурс. Не переносить и не объявлять принятыми. |
| ❌ | не годится по стилю, эпохе или пропорциям |
| ⚠ | годится с конкретной оговоркой |
| 🔎 КАНДИДАТ | ещё не скачан. Оценён только по превью Sketchfab/Poly Haven, лицензия разрешает коммерческое использование. Проверить в Unity по кадру. |

**Как оценивал.**
- Скачанные модели я сам отрендерил из файлов репозитория — листы `assets-A`, `assets-B`.
- Кандидаты — по официальным превью, листы `assets-C`, `assets-D`.
- Размеры — по bbox после нормализации в manifest: Ш × В × Г, метры.
- CC-BY требует указать автора в CREDITS.

---

## 1. Уже скачано — что ставить (лист `assets-A`, `assets-B`)

| Предмет по SCENARIO | Источник в репо | Вердикт | Куда и как |
|---|---|---|---|
| Шифоньер (спальня) | `Art/ReadyModels/Sketchfab/Wardrobe` — dashkilya, CC-BY, «Old Soviet Wardrobe», 796 граней | ⚠ | Мой рендер в 4 поворотах: **фасад с ручками — длинная сторона, 1.71 × 1.90 × 0.95**. Чтобы вписать в 1.0 × 0.55 на перегородке (центр 2.505, 2.3), фасад придётся сжать по горизонтали в 0.58 раза — ручки и фаски вытянутся в 1.7 раза по вертикали. Принимать **только по кадру спереди с 1.5 м**. Если видно растяжение — 🔎 Skilletik ниже. |
| Трюмо (прихожая А, x=6.6, z 3.0–4.4) | `Sketchfab/Dresser` — VVMGames, CC-BY, 1.2 × 1.8 × 0.45 | ⚠ | Заменяет `cabinetBedDrawer` и торчащее «зеркало-плавник» (`bathroomMirror`). Тумба с высоким зеркалом — по смыслу трюмо. Зеркальная панель сейчас синяя (текстура-заглушка): нужен материал зеркала (тёмный, отражающий через reflection probe). Ставить вплотную к стене x=6.62, лицом в +x. |
| Красный телефон (на трюмо) | `Sketchfab/VEFPhone` — strnroman, CC-BY | ✅ | Настоящий VEF (рижский завод), красная трубка. Заменяет кубик. Высота 0.15 — верно. |
| Тапки (прихожая) и **один тапок в §7.5** | `Sketchfab/Slippers` — headless_christ, CC-BY | ✅ | Пара 0.32 × 0.08 × 0.40. Для §7.5 разделить пару на два меша, если они раздельные в FBX. Если меш один — второй экземпляр, у которого скрыт один тапок. Убрать «подушку-тапок» (`ScenarioBuilder.cs:164`). |
| Настенные часы с маятником (зал, как в v3) | `Sketchfab/WallClock` — Odin Branigan, CC-BY, 0.38 × 0.70 | ⚠ | Белая панель на месте стекла в моём рендере — проверить материал стекла. Не путать с дедовыми напольными часами. |
| Дедовы часы с боем (прихожая Б) | `ScenarioProxies/PolyHaven/vintage_grandfather_clock_01`, CC0 | ✅ | Лучший объект в кадрах, оставить. |
| «Рубин» | `Art/CC0/Television_01` — Poly Haven, Gabriel Radić, CC0, 0.60 × 0.46 × 0.47 | ✅ | Ставить в нишу стенки на салфетку. Заменяет `televisionVintage`, `cabinetTelevision` и временный экран. |
| Диван-книжка (у x=2.9) | `ReadyModels/CommieBlock/SovietSofa` — GrawlixMcLaren, CC0, 2.39 × 0.85 × 1.0 | ⚠ | Форма и эпоха верные. Текстуры этого набора кислотно-оранжевые: **снизить насыщенность на ~30% и яркость на ~15%** в текстурах. Иначе вернётся «рыжая» картинка. Длина 2.39 помещается в z 0.3–2.7. |
| Кресло у окна | `CommieBlock/SovietArmchair`, CC0 | ⚠ | Та же правка цвета. Плед — 🔎 или по решению Димы. |
| Журнальный столик | `CommieBlock/SovietCoffeeTable`, CC0, 1.67 × 0.45 × 0.92 | ⚠ | Для зала велик. Масштаб по плану 0.65–0.7 при прежней высоте (для стола искажение незаметно) → ~1.1 × 0.45 × 0.62. Правка цвета. |
| Стул (спальня у окна / кухня) | `CommieBlock/SovietDiningChair`, CC0 | ⚠ | Правка цвета. |
| Ниша с книгами в стенке | `CommieBlock/SovietShelf` (CC0, 1.87 × 1.9 × 0.52) + `Art/CC0/book_encyclopedia_set_01` (Poly Haven CC0) | ⚠ | Стеллаж пыльно-коричневый, нормальный. Заполнить книгами. |
| Письменный стол | `CommieBlock/SovietDesk` | ❌ | В SCENARIO нет. |
| ТВ CommieBlock | `CommieBlock/SovietTV` | ❌ | Грубее, чем Poly Haven Television_01. |
| Железная кровать | `game-v2/src/assets/models/old_bed_frame.glb` — Poly Haven, CC0, 0.90 × 1.20 × 2.00 | ⚠ | Рама верная, но голая: нет матраса и постели. Ставить у стены x=0, центр (0.5, 0, 1.8). Матрас, покрывало, горка подушек — готового нет (§3). |
| Чемодан на шифоньере | `game-v2/.../vintage_suitcase.glb` — Poly Haven CC0 | ✅ | Один из двух. |
| Будильник на тумбочке | `alarm_clock_01` — Poly Haven CC0 | ✅ | |
| Каминные часы (стенка) | `mantel_clock_01` — Poly Haven CC0 | ✅ | |
| Кухонная мелочь | `pot_enamel_01`, `tea_set_01`, `jug_01`, `russian_food_cans_01`, `wicker_basket_01` — Poly Haven CC0 | ✅ | На плиту, стол, подоконник. Рисунок пламени на `pot_enamel_01` проверить на близком кадре. |
| Резиновые сапоги (прихожая) | `rubber_boots` — Poly Haven CC0 | ✅ | |
| Герань и цветы | `potted_plant_01/04` (v3), `horror-project/.../potted_plant_02` — Poly Haven CC0 | ✅ / ⚠ | `_02` слишком большой (0.84) — только на пол. На подоконник — `_04`. |
| Ваза (столик) | `Art/CC0/ceramic_vase_01` — Poly Haven CC0 | ✅ | |
| Плита (кухня) | `horror-project/.../electric_stove` — Poly Haven CC0, белая эмаль | ⚠ | Корпус верный, но конфорки **электрические**, а в SCENARIO газ: спичка, синее пламя, жёлтая труба (§4.3). Временно годится. Лучше 🔎 газовая ниже. |
| Электрочайник | `vintage_electric_kettle` — Poly Haven CC0 | ❌ | В SCENARIO свистящий чайник на газу. |
| Дедова лампочка / патрон | — | 🔎 | См. §2. |
| Иконы в красном углу | `Art/OpenAccessMet/Icon205079`, `Icon831188` — Met Open Access, public domain | ✅ | Это изображения. Рамки — 🔎 `fancy_picture_frame_01`. |
| Ткань, паркет, штукатурка | `Art/CC0/*` — Poly Haven CC0 | ✅ | Ткань `quatrefoil_jacquard` — масштаб узора уменьшить в 3–4 раза. |
| **Не годится по стилю** | `Sofa_01`, `CoffeeTable_01` (барокко), `WoodenChair_01` (готика), `SchoolChair_01`, `mid_century_lounge_chair`, `painted_wooden_bench`, `wall_clock` (офисные) | ❌ | В квартиру не ставить. |
| Ковёр и обои | `horror-project/game/assets/rug-v2.png`, `wallpaper-v2.png` | ⚠ | Это **AI-генерация** (`horror-project/art/PROMPTS.md:99`), не фото и не CC0-скан. Использовать только с согласия Димы — вопрос ниже. |

**Убрать из квартиры полностью:**
- все 28 вызовов `Prop("…")` из Kenney Furniture Kit (`ScenarioBuilder.cs:150–165`);
- временный экран ТВ, «подушку-тапок», Quaternius-прокси.

Kenney формально CC0 и «готовый», но визуально отвергнут: игрушечные блоки. В финальной квартире его быть не должно.

---

## 2. Готового в репо нет — кандидаты Poly Haven, CC0 (лист `assets-C`)

| Нужно | Кандидат | Вердикт |
|---|---|---|
| **Табуретка** — вечерняя сцена с лампочкой и §7.5 | https://polyhaven.com/a/painted_wooden_stool — белая крашеная, облупленная | 🔎 Лучший вариант: советская кухонная табуретка. Запасной — `wooden_stool_01`. |
| Патрон с лампочкой и крюк (спальня §7.5, прихожая) | https://polyhaven.com/a/pull_chain_light_socket + https://polyhaven.com/a/lightbulb_01 | 🔎 Для «люстры с крючком» в спальне или голой лампочки. |
| Люстра в зале | https://polyhaven.com/a/Chandelier_02 — латунь, белые абажуры | 🔎 Ближе всего к «рожковой», но рожков 6, а не 3. Можно скрыть лишние, если меши раздельные. `Chandelier_03` (хрусталь) — ❌. |
| Кухонный стол с клеёнкой в клетку | https://polyhaven.com/a/dining_table | 🔎 На превью стол со скатертью в клетку — прямо по SCENARIO. Проверить размер ≤ 1.0 × 0.65. |
| Кухонная тумба (в ящике свечи) | https://polyhaven.com/a/painted_wooden_cabinet — белая облупленная | 🔎 |
| Кухонный шкафчик и буфет | https://polyhaven.com/a/painted_wooden_cabinet_02 — голубой | 🔎 |
| Рамки: портрет деда (траурный и военный), фото | https://polyhaven.com/a/hanging_picture_frame_02, https://polyhaven.com/a/fancy_picture_frame_01 | 🔎 Фото внутри — public domain или выдуманное. Решает Дима, как было с M1. |
| Комод или тумбочка у кровати | https://polyhaven.com/a/vintage_wooden_drawer_01 | 🔎 На превью картотечный шкаф — скорее ❌. Нужна тумбочка ~0.45 × 0.55 × 0.4: готовой хорошей нет. |
| Свет на площадке | https://polyhaven.com/a/industrial_wall_lamp — плафон в решётке | 🔎 Подъезд. |
| ❌ по стилю | `ArmChair_01`, `sofa_02/03`, `television_02`, `throw_pillows_01`, `WoodenTable_01/03`, `modern_wooden_cabinet`, `drawer_cabinet`, `vintage_radio_transceiver` | ❌ |

---

## 3. Кандидаты Sketchfab с коммерческой лицензией (лист `assets-D`)

Номера — позиция на листе слева направо, сверху вниз. Варианты с NonCommercial, NoDerivs и Free Standard я отсеял.

| # | Нужно | Модель | Лицензия, автор | Вердикт |
|---|---|---|---|---|
| 1 | **ЗиЛ «Москва»** | «USSR Fridge» https://sketchfab.com/3d-models/none-5a0ba464174e4dbe94aecfe1a036ca6a | CC-BY, LongLight, 9.3k граней | 🔎 Белый, скруглённый — главный кандидат. |
| 0 | ЗиЛ (запасной) | «Soviet Fridge "ZIL Moskva"» https://sketchfab.com/3d-models/none-ea1ea4d5847b4550bc58ef4107df6f94 | CC-BY, crazyshroomz, 32k | 🔎 На превью красный — перекрасить в белый. |
| 2 | **Газовая плита** | «Old Soviet Gas Stove» https://sketchfab.com/3d-models/none-0958b0ab4e2344cc847e14355f196512 | CC-BY, shaqsh, 6k | 🔎 Белая эмаль, 4 конфорки. На превью вместе с кафелем — фон кафеля не брать. |
| 3/4 | Плита (запасные) | «Gas Stove» longtail https://sketchfab.com/3d-models/none-056ccf898f9e49acb6e3e370deabf184 ; «Gas Stove / Газова Плита» Khatagames https://sketchfab.com/3d-models/none-b4ee8646cdd340928e4d08e2b920ac51 | CC-BY | 🔎 |
| 5 | Раковина (ванная или кухня) | «Soviet Sink / Радянська Раковина» https://sketchfab.com/3d-models/none-789e81f99ae94875939b63c1c9da3a64 | CC-BY, Khatagames, 7.8k | 🔎 |
| 8/9 | **Чугунная батарея** (под каждым окном, ремень §8) | «PLAIN cast-iron radiator» https://sketchfab.com/3d-models/none-c797974d7e73445cb22698260e3c78ef / «PLAIN 3K» https://sketchfab.com/3d-models/none-f100fab40964464a99103ffe53e6b8f5 | CC-BY, termagroup | 🔎 Форма «гармошки» верная, но 177–258k граней. **Обязательно** упростить до ≤ 5k. |
| 10 | **Шифоньер** (если dashkilya растянется) | «Soviet Old Wardrobe» https://sketchfab.com/3d-models/none-f9dd8aa0dd684049911352fa5864ce0c | CC-BY, Skilletik, 1.5k | 🔎 На превью высокий двустворчатый с ящиком внизу — пропорции шифоньера. |
| 13 | Секция стенки со стеклом | «Советский шкаф PSX», Tesalo https://sketchfab.com/3d-models/none-c302fcb3df744c2cb26f6bfc3eef44b4 | CC-BY, 1.7k | 🔎 PSX-стиль, проверить сочетаемость. |
| 23/24 | **Сервант с хрусталём** (стенка) | «Old soviet sideboard» dashkilya https://sketchfab.com/3d-models/none-27974684f94e4755a863e9984cb4266b ; «Sideboard of the USSR» zxcharred https://sketchfab.com/3d-models/none-ce00bacd5fe244709956f4c2e45352ae | CC-BY, 11k / 3k | 🔎 Вместе с полкой CommieBlock и Television_01 закрывает «стенку» x=6.5 из готовых частей. |
| 15/16 | Люстра (альтернатива) | «Soviet Chandelier Cascade» https://sketchfab.com/3d-models/none-2bb4e90fa037498d951d208ebf873a73 / «Soviet Chandelier Ruby» https://sketchfab.com/3d-models/none-cc1f4fed9a424d4ba268ef72d52506f5, owring | CC-BY | 🔎 «Каскад» — очень советская, для зала. «Рубин» с красным стеклом — подвес. |
| 18/19 | Ванная: тазы, корзина | «Soviet Basin» zxcharred https://sketchfab.com/3d-models/none-20360708ddda4feab073d5f441f98162 ; «советский тазик для белья» https://sketchfab.com/3d-models/none-0861d39521d14105ba98dff7482f78e0 | CC-BY | 🔎 |
| 20 | Стиральная машина «Малютка» | «Soviet Wash Machine» wing13 https://sketchfab.com/3d-models/none-c05b53b622fb4066a8181c59950fb79c | CC-BY, 0.8k | 🔎 Вид — угловатый ящик с крышкой, не цилиндр «Малютки». Принимать по кадру. |
| 21/22 | **Вешалка с крючками** (прихожая) | «Wooden hook rack» sousinho https://sketchfab.com/3d-models/none-b7928bccdd1344b79c0b67e7a9878016 ; «Coat Hat Rack» KodaWowo https://sketchfab.com/3d-models/none-b53aa4a6c9274284b0b35a229adcaae7 | CC-BY | 🔎 Пальто на крючках — готового нет (§4). |
| 25 | Венский стул (кухня или спальня) | «Old Soviet Chair» shaqsh https://sketchfab.com/3d-models/none-f22abd940288406b9f3079a4ee91b53c | CC-BY, 2.7k | 🔎 |
| 6, 14 | Готовые наборы | «Post-soviet furniture set with dithering» mariakrasinska https://sketchfab.com/3d-models/none-e179fb17eb884ad2b0a2ade519ea42a7 (9k, PS1-стиль) ; «Old soviet interior items 1940-1980s» vladySLAVE https://sketchfab.com/3d-models/none-a2b22ff57dc9442e90100e51f493f23d (**1.1 млн граней**) | CC-BY | 🔎 Первый может дать единый стиль для кухни. Второй — богатый, но тяжёлый: брать отдельные предметы и упрощать. |
| ❌ | «Soviet Tv Rubin 54TC» (144 грани, коробка), «Soviet Kitchen Sink» (194 грани, ящик), «Old russian wardrobe» (расписной, узкий), «Old wardrobe» L1kePotato (открытый низкий) | | | ❌ |

Перед скачиванием открыть страницу модели и проверить лицензию на ней — по правилу проекта, каждый файл отдельно. CC-BY — указать автора в CREDITS.

---

## 4. Что пока остаётся без готового источника — решение Димы

Это текстиль, крупный узор и архитектура. Готовых подходящих я не нашёл:

1. **Шторы, тюль, покрывало, матрас, горка подушек с накидкой, плед, пальто на вешалке.**
   **Вопрос Диме:** «Ткани (шторы, тюль, постель, пальто) можно сделать простыми плоскостями с готовыми CC0-текстурами, или ищем только готовые модели?»
2. **Ковры на стене и на полу.** Плоскость с текстурой. Источник текстуры: AI-генерация `rug-v2.png` или сканы.
   **Вопрос Диме:** «Ковры и обои из AI-текстур `rug-v2` / `wallpaper-v2` годятся, или нужны сканы и фото?»
3. **Окна, двери, плинтусы, наличники, антресоль, лампадка** — это архитектура, а не мебель. Предлагаю считать допустимой простой геометрией с готовыми CC0-материалами.
   **Вопрос Диме:** «Окна, двери и плинтусы делаем сами как часть стен — согласен?»
4. **Торшер, бра с тканевым абажуром, радиоточка, тумбочка у кровати, мойка с «ёлочкой» на кухне, навесные кухонные шкафы, ванна на ножках** — нужен отдельный поиск. По «soviet floor lamp» и «malyutka» коммерческих результатов нет.

## 5. Порядок, совместимый с запретом

1. Шифоньер — уже делается. Принять по кадру спереди и ref-E/F/05/07. Если растянут — Skilletik #10.
2. Спальня из готового: `old_bed_frame`, Slippers, `painted_wooden_stool` 🔎, патрон и лампочка 🔎, рамки 🔎, иконы Met. Постель — после ответа Димы по п.4.1.
3. Прихожая: Dresser с правкой зеркала, VEFPhone, Slippers, `rubber_boots`, вешалка 🔎.
4. Зал: SovietSofa / Armchair / CoffeeTable с правкой цвета, сервант 🔎, Shelf + книги, Television_01, `mantel_clock`, WallClock, Chandelier 🔎, ваза, цветы.
5. Кухня: ЗиЛ 🔎 #1, газовая плита 🔎 #2, `dining_table` 🔎, `painted_wooden_cabinet` 🔎, табуретки, мелочь Poly Haven.
6. После каждого шага — тот же набор 38 кадров и листы `ref-*`. Принимает Дима.
