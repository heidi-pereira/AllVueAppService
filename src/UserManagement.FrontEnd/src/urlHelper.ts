export function getBasePathFromCurrentPage(): string {
    const result = document.querySelector('base')?.getAttribute('href');
    if (!result) {
        const defaultBasePath = '/usermanagement'; 
        console.error(`Base path not found, defaulting to ${defaultBasePath}`);
        return defaultBasePath;
    }
    if (result.endsWith('/')) {
        return result.slice(0, -1); // Remove trailing slash
    }
    return result;
}

// Helper to prepend base path to URLs
export function withBasePath(path: string) {
    const base = getBasePathFromCurrentPage();
    if (path === '/') return base;
    return base.replace(/\/$/, '') + path;
}
