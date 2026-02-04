export enum FontStyle {
    Regular = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Strikeout = 8,
}

export class FontHelper {

    public Max: number;
    public Min: number;
    constructor(min: number, max: number) {
        this.Min = min;
        this.Max = max;
    }

    public static ApplyFontStyle(element: HTMLElement, fontStyle: any) {
        const style = element.style;
        style.fontWeight = (fontStyle & FontStyle.Bold) !== 0 ? 'bold' : 'normal';
        style.fontStyle = (fontStyle & FontStyle.Italic) !== 0 ? 'italic' : 'normal';
        style.textDecoration =
            ((fontStyle & FontStyle.Underline) !== 0 ? 'underline ' : '') +
            ((fontStyle & FontStyle.Strikeout) !== 0 ? 'line-through' : '');
        style.textDecoration = style.textDecoration.trim() || 'none';
    }

}
