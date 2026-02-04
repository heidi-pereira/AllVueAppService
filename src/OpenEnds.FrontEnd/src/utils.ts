import { OpenEndTheme, RootTheme } from "./Model/Model";

export function calculatePercentage(value?: number, total?: number): string {
    if (!total || !value) {
        return "0%";
    }
    const percentage = (value! / total!) * 100;
    return percentage < 1 ? "<1%" : `${percentage.toFixed(0)}%`;
}

export function displayPercentage(percentage: number): string {
    return percentage === 0 ? "0%" : percentage < 1 ? "<1%" : `${percentage.toFixed(0)}%`;
}

export function isNonEmptyString(input: string | undefined): input is string {
    return typeof input === "string" && input.trim() !== "";
}

export function themesAsHierarchy(themes: OpenEndTheme[]): RootTheme[] {
    const rootThemes: RootTheme[] = themes.filter(theme => theme.parentId === null).map(theme => ({ ...theme, subThemes: [] }));

    rootThemes.forEach(rootTheme => {
        const subThemes = themes.filter(theme => theme.parentId === rootTheme.themeId);
        if (subThemes) {
            rootTheme.subThemes.push(...subThemes)
        }
    });

    return rootThemes;
}

export function themeAsRoot(themes: OpenEndTheme[], selectedTheme: OpenEndTheme): RootTheme {
    const subThemes = themes.filter(theme => theme.parentId === selectedTheme.themeId);

    return {
        ...selectedTheme,
        subThemes: subThemes
    };
}