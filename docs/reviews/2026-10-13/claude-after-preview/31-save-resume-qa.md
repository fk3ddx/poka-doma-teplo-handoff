# SaveResumeQA — сохранение и продолжение через полный перезапуск процесса

База: **Codex bca717a**. Арт и геймплей не трогаются. Добавлены минимальные хуки (без флага поведение прежнее) и QA.

## Хуки в игровом коде
| Файл | Что | Без флага |
|---|---|---|
| `ScenarioSaves.cs` (новый) | Пути обоих чекпоинтов. С `-qa-save-dir <dir>` оба файла живут в `<dir>`, путь вычисляется в `StoryDirector.Awake` и `LoopDirector.Start`, то есть до любого чтения и записи. Папку игрока как QA-папку отклоняет. | `persistentDataPath`, как было |
| `StoryDirector.Awake` | `savePath = ScenarioSaves.File("story-checkpoint.json")` | тот же путь |
| `LoopDirector.Start` | `savePath = ScenarioSaves.File("scenario-core-checkpoint.json")`; `SavePath` для отчёта | тот же путь |
| `LoopDirector` | Тело кнопки меню «Продолжить с сохранения» вынесено в `ContinueFromSave()`, условие показа — в `OffersContinue`. `OnGUI` вызывает ровно их. | та же кнопка, тот же код |
| `ScenarioSmoke` | `ResumeAfterContinue(dir, lap, chase)`: после Continue пройти восстановленный круг до следующего (через тамбур) **или** провести восстановленную погоню до финала. Передача в финал вынесена в `HandToFinale()` — тот же код, что в integrated-N. | `-scenario-smoke` работает как раньше |

## SaveResumeQA
Аргументы: `-save-resume-qa <out> -save-resume-case lap2|chase|finale -save-resume-phase stage|resume -qa-save-dir <dir>`.

**stage (процесс 1).** Проверяет изоляцию путей и чистит в QA-папке только свои два файла. Ставит `loop.testing=false`: запись идёт настоящими методами, в изолированную папку.

| Случай | Постановка |
|---|---|
| lap2 | настоящий `EnterLoop` (`Checkpoint("Loop")`) → фонарик → `EnterLap(2)` (`SaveCheckpoint`) |
| chase | `EnterLoop` → фонарик → `EnterLap(6)` → `chase.Begin()` у двери прихожей (как `-scenario-chase-only`; `Begin` сам пишет `chase:true`) |
| finale | `QaBegin("Finale")` → `Finale()` пишет `Checkpoint("Finale")` |

Затем проверяет записанный JSON (глава, круг, фонарик, погоня), снимает PNG и завершает процесс.

**resume (новый процесс).**
- Проверяет, что меню предлагает «Продолжить с сохранения» (`OffersContinue`).
- Нажимает его: `LoopDirector.ContinueFromSave()` → `StoryDirector.Continue()`. Прямого `EnterLap` нет.
- Пишет восстановленные главу, круг, фонарик, погоню, направление двери и позицию; снимает PNG.

| Случай | Что проверяется после Continue | Чем ведётся дальше |
|---|---|---|
| lap2 | Loop, круг 2, фонарик, без погони, дверь в прихожую, игрок на старте круга | ScenarioSmoke проходит круг 2 → варп → `RETURN_DOOR` → круг 3 |
| chase | Loop, круг 6, фонарик, погоня снова идёт | ScenarioSmoke: `ChaseRun` → удары часов → живой Finale → FinaleQA до эпилога |
| finale | Finale с начала, задача «Дождаться бабушку» | FinaleQA (`ContinueCurrent`) до эпилога |

## Изоляция и перезапись (дополнение)
- **Невалидный `-qa-save-dir` — ошибка, без fallback.** Это флаг без папки, пустая строка, следующий флаг вместо пути, неприменимый путь или папка игрока. `ScenarioSaves.QaRejected` даёт пути во временный кэш (никогда не `persistentDataPath`). SaveResumeQA сразу пишет FAIL и выходит с кодом 1, ничего не читая, не записывая и не удаляя.
- **Перед Continue** оба `SavePath` ещё раз проверяются на QA-папку.
- **Хеши.** SHA-256 обоих JSON снимается в `Boot` (самый ранний момент процесса, после Awake, до всех Start) и ещё раз прямо перед Continue. Check: они равны, то есть на загрузке ничто файлы не переписало. Runner хеширует файлы после писателя, до запуска читателя, и сверяет все три точки.
- **Удаление story-сохранения после эпилога.** В `Epilogue` условие стало `(!loop.testing || qaAllowSave)`; обычная игра (`testing=false`) не меняется. В случаях chase/finale читатель ставит `qaAllowSave` и ждёт `loop.Finished`. Check: файла больше нет (`epilogue-save.json`). Runner после выхода процесса проверяет это ещё раз по диску.
- **`--case reject`:** один процесс с пустым `-qa-save-dir`. Ожидается ненулевой выход, FAIL в `result.json` и пустая QA-папка.

## Запуск (Windows)
`python tools/qa/save_resume.py --label <run> --build-label <build> --case lap2|chase|finale|reject` (новый каталог на каждый запуск, `exist_ok=False`)

Два отдельных процесса, общая `qa-saves/` внутри папки результата. Итог в `save-resume.json`: stage, resume, walk/chase/finale, epilogue.

## Ограничения теста
- **Состояние до сохранения поставлено**, а не сыграно: круг 2 через `EnterLap(2)`, погоня через `chase.Begin()` без сцены ChaseStart, финал через `QaBegin`. Запись при этом идёт настоящими методами. Непрерывную игру до этих точек уже покрывают flow-c и integrated-N.
- **Выход из игры — это `Application.Quit` после записи**, а не закрытие окна посреди кадра. Отказоустойчивость записи (обрыв во время `WriteAllText`) не проверяется.
- **Сохранение в финале — только `Checkpoint("Finale")` в его начале.** Continue всегда начинает финал с 22:00; промежуточного прогресса финала игра не хранит, так и задумано.
- **Нажатие кнопки** — вызов того же метода, что в `OnGUI`, а не клик мыши по IMGUI.
- Во втором процессе в начале Continue `ResumeSavedLap` → `EnterLap` снова пишет чекпоинт круга (в QA-папку). Это обычное поведение игры.

## Не проверено
Только Roslyn против заглушек (ScenarioSmoke и HallLightProbe теперь тоже в моей сборке). Прогоны — за тобой.
