import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import eslint from 'vite-plugin-eslint';
import { fileURLToPath, URL } from "url";

const target = 'http://localhost:7035';

export default defineConfig({
    resolve: {
        alias: [
            { find: '@', replacement: fileURLToPath(new URL('./src', import.meta.url)) },
            { find: '@model', replacement: fileURLToPath(new URL('./src/Model', import.meta.url)) },
            { find: '@shared', replacement: fileURLToPath(new URL('../Vue.Common.FrontEnd/Components', import.meta.url)) },
        ]
    },
    esbuild: {
        legalComments: 'none',
        supported: {
            'top-level-await': true,
        }
    },
    build: {
        emptyOutDir: true,
        outDir: './dist',
    },
    base: '/openends/',
    plugins: [react(), eslint()],
    server: {
        proxy: {
            '/openends/signin-oidc': {
                target,
                secure: false,
                rewrite: (path) => path.replace('/openends', '')
            },
            '/openends/api': {
                target,
                secure: false,
                rewrite: (path)=>path.replace('/openends', '')
            },
            '/openends/login': {
                target,
                secure: false,
                rewrite: (path)=>path.replace('/openends', '')
            },
            '/openends/health': {
                target,
                secure: false,
                rewrite: (path)=>path.replace('/openends', '')
            },
            '/mixpanel': {
                target: 'http://api-js.mixpanel.com',
                secure: false,
                changeOrigin: true,
                rewrite: (path) => path.replace('/mixpanel', '')
            },
        },
    },
    // Can possibly remove this in future but at the moment without it the api is set to legacy and produces a warning
    css: {
        preprocessorOptions: {
          scss: {
            api: 'modern-compiler' // or "modern"
          }
        }
      }
})
