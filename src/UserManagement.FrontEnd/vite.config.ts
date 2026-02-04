import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import eslint from 'vite-plugin-eslint';
import { fileURLToPath, URL } from "url";
import alias from '@rollup/plugin-alias';

// Shared configure function for all proxies
const configureProxy = (proxy: any) => {
    proxy.on('proxyReq', (proxyReq: any, req: any) => {
        // Set the X-Forwarded-Host header globally
        proxyReq.setHeader('X-Forwarded-Host', req.headers.host || '');
    });
};

const target = 'http://localhost:7036';

const proxyConfig = {
    target,
    changeOrigin: true,
    secure: false,
    configure: configureProxy,
};
export default defineConfig({
    optimizeDeps: {
        include: [
            'gravatar',
            '@mui/material',
            '@mui/x-data-grid',
            'react',
            'react-dom'
        ],
    },
    resolve: {
        extensions: ['.ts', '.tsx', '.js'], // File extensions to resolve
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
        rollupOptions: {
        plugins: [
            alias({
            entries: [
                { find: 'gravatar', replacement: '/node_modules/gravatar' },
                { find: '@mui/x-data-grid', replacement: '/node_modules/@mui/x-data-grid' },
            ]
            })
        ]
        }
    },
    base: '/usermanagement',
    plugins: [react(), eslint()],
    server: {
        port: 7037,
        proxy: {
            '/usermanagement/api': proxyConfig,
            '/usermanagement/signin-oidc': proxyConfig,
            '/usermanagement/signout-oidc': proxyConfig,
            '/usermanagement/login': proxyConfig,
            '/usermanagement/logout': proxyConfig,
            '/usermanagement/health': proxyConfig,
        },
    },
});
