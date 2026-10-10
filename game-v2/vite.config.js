import {defineConfig} from 'vite';
import {viteSingleFile} from 'vite-plugin-singlefile';

export default defineConfig(({mode}) => ({
  base: './',
  server: {host: '127.0.0.1', port: 5173},
  plugins: mode === 'single' ? [viteSingleFile()] : [],
  build: {
    target: 'es2022',
    assetsInlineLimit: mode === 'single' ? 100_000_000 : 4096,
    chunkSizeWarningLimit: 100_000,
    outDir: mode === 'single' ? 'dist-single' : 'dist',
  },
}));
