# pc24c — перемотка: пауза без побочных эффектов, реальная высота камеры

Поверх pc24b (Codex 84d1fca + pc24b). Не проверено в Unity: только компиляция с заглушками.

## Пауза
Codex silent-mix: `rewind_tick` requested 152 / started 32 / limit 120 на один replay+отмену. Причина: на паузе `Play` продолжал цикл при `deltaTime=0`, а `AudioListener.pause` делает `isPlaying=false`, поэтому `RewindTick` каждый кадр заново запрашивал голос.
- `NightRewind.Play`: при `player.paused || sound.paused` кадр пропускается целиком — ни сэмпла, ни звука, ни дрожания OSD/камеры, ни смены воспоминания.
- `ScenarioSound.RewindTick` / `PlayReversed`: на паузе ничего не делают. Тик перезапускается только когда ScenarioSound сам освободил голос (`cue != RewindTickCue`), а не по `isPlaying`.

## Высота камеры
- В каждый сэмпл пишется `head.localPosition.y` (0.94 присед / 1.55 стоя); воспроизводится с интерполяцией. Stride 8 + двери + лампы + 7.
- На PLAY ночь 2 начинается стоя: `PrepareNight2` и `ValyaAtTheDoor(true)` ставят `crouched=false` перед `Place`.
- Отмена посреди перемотки возвращает `crouched` и высоту глаз, какие были до неё.

## `-rewind-qa`: новое
- Маршрут ночи: две остановки присев, после 22:05 — присев до смерти (штатное укрытие Ctrl).
- `camera height recorded`: есть сэмплы < 1 м и > 1.5 м.
- `camera height replayed as recorded`: во время reverse высота сравнивается с живым журналом ночи по исходному времени кадра; около смены позы (±0.35 с) не сравнивается; расхождение > 5 см — FAIL, первые 5 в NOTE.
- `PLAY: night 2 starts standing`, `after rewind: standing in night 2` (eye 1.55, body > 1.6).
- `pause has no playback side effects`: 0 новых `CueStarted` и 0 запросов `rewind_tick` за 0.5 с паузы; поворот головы не изменился.
- `rewind ticking is ONE voice`: за перемотку `rewind_tick` started == 1, limit == 0.
- `SOUND reversed one-shots requested/started/limited` — только отчёт; лимит 8 голосов честный.
- Отмена: replay запускается присев; `cancel restores the crouch`; после `QaBegin("Night2")` — стоя.
- Новые поля в `result.json`: `pauseCueStarts, pauseTickRequests, tickRequested, tickStarted, tickLimited, reversedRequested, reversedStarted, reversedLimited, crouchedSamples, standingSamples, playbackCrouchedFrames, playbackStandingFrames, eyeMismatches, diedCrouched, playEye`.

CLI тот же.

## Риски
- Если после `AudioListener.pause=false` Unity ещё кадр отдаёт `isPlaying=false`, ScenarioSound освободит тик и он стартует второй раз: проверка `started == 1` даст FAIL. Тогда это увидим по числу.
- Смерть присев (`diedCrouched`) снимается впервые: постановка DeathFull от высоты 0.94 не проверялась в `-death-qa`; смотреть кадры.
