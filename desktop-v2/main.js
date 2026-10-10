// Desktop shell for "Атака на запоминание": fullscreen window, discrete GPU, no browser chrome.
const {app, BrowserWindow, Menu} = require('electron');
const path = require('path');

app.commandLine.appendSwitch('force_high_performance_gpu');   // laptops: use the discrete GPU
app.commandLine.appendSwitch('ignore-gpu-blocklist');
app.commandLine.appendSwitch('enable-gpu-rasterization');
app.commandLine.appendSwitch('autoplay-policy', 'no-user-gesture-required');

function create() {
  Menu.setApplicationMenu(null);
  const win = new BrowserWindow({
    width: 1600, height: 900, fullscreen: true, backgroundColor: '#000000', title: 'Атака на запоминание',
    autoHideMenuBar: true,
    webPreferences: {backgroundThrottling: false, contextIsolation: true, sandbox: true},
  });
  win.loadFile(path.join(__dirname, 'game', 'index.html'));
  // F11 toggles fullscreen, Alt+F4 / closing the window quits
  win.webContents.on('before-input-event', (e, input) => {
    if (input.type === 'keyDown' && input.key === 'F11') { win.setFullScreen(!win.isFullScreen()); e.preventDefault(); }
  });
}
app.whenReady().then(create);
app.on('window-all-closed', () => app.quit());
