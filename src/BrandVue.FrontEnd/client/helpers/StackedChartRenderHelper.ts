export class StackedChartRenderHelper {
    public static hslColourFromPercentAsDecimal(percent: number) {
        const green = 120;
        // Return a CSS HSL string
        return `hsl(${(green * percent)}, 100%, 45%)`;
    }
}
