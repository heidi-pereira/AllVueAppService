import TinyColor from 'tinycolor2';


export default class ColourTransformations {

    public static Brighten(colourString: string, brightenAmount: number = 0.7) {
        var colour = TinyColor(colourString).toRgb();
        colour.r = ColourTransformations.WhiteFilterFactorTransformation(colour.r, brightenAmount);
        colour.g = ColourTransformations.WhiteFilterFactorTransformation(colour.g, brightenAmount);
        colour.b = ColourTransformations.WhiteFilterFactorTransformation(colour.b, brightenAmount);
        return TinyColor(colour).toHexString();
    }

    public static WhiteFilterFactorTransformation(factor: number, brightenAmount: number) {
        return Math.round((255 * brightenAmount) + (factor * (1 - brightenAmount)));
    }
}