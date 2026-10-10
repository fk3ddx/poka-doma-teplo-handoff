# Десктопная оболочка (Electron)

Собирает игру в Windows-приложение без браузера, с приоритетом дискретной видеокарты.

```
cd game-v2 && npm install --include=dev && npm run build:single
mkdir -p app/game && cp game-v2/dist-single/index.html desktop-v2/game/index.html
cd desktop-v2 && npx @electron/packager . "Ataka-na-zapominanie" --platform=win32 --arch=x64 --asar --out=out
```

Готовый zip выкладывается в GitHub Releases (больше 100 МБ, в git не помещается).
