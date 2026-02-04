import * as BrandVueApi from "../BrandVueApi";


export class Iso2LetterCountryCodesLowercase {
    public static readonly  gb = "gb";
    public static readonly us = "us";
    public static readonly de = "de";
}

export class NumberFormattingHelper {

    private static localeDescriptor: string ;
    private static isoCountryCode: string = Iso2LetterCountryCodesLowercase.gb;
    private static currencyFormat: Intl.NumberFormat;
    private static currencyFormat0Dp: Intl.NumberFormat;
    private static defaultFormat: Intl.NumberFormat;
    private static defaultFormatNoTrailingZeros: Intl.NumberFormat;
    private static defaultFormat0Dp: Intl.NumberFormat;
    private static defaultFormat1Dp: Intl.NumberFormat;
    private static defaultFormat1DpNoTrailingZeros: Intl.NumberFormat;
    private static defaultFormat2Dp: Intl.NumberFormat;
    private static defaultFormat2DpNoTrailingZeros: Intl.NumberFormat;
    private static nonCulturealDefaultFormat1Dp: Intl.NumberFormat;    
    

    static setLocale(subset: BrandVueApi.Subset) {
        NumberFormattingHelper.isoCountryCode = (subset && subset.iso2LetterCountryCode) ? NumberFormattingHelper.isoCountryCode = subset.iso2LetterCountryCode.toLowerCase() : Iso2LetterCountryCodesLowercase.gb;

        switch (NumberFormattingHelper.isoCountryCode) {

        case Iso2LetterCountryCodesLowercase.us:

            NumberFormattingHelper.localeDescriptor = "en-us";
            break;

        case Iso2LetterCountryCodesLowercase.gb:
            NumberFormattingHelper.localeDescriptor = "en-gb";
            break;

        case Iso2LetterCountryCodesLowercase.de:
            NumberFormattingHelper.localeDescriptor = "de-de";
            break;


        default:
            //This may work for some countries eg fr
            //but not for austria eg de-AT
            //
            //For full list of valid locals
            //https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/NumberFormat
            //https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl#Locale_identification_and_negotiation
            //
            NumberFormattingHelper.isoCountryCode = NumberFormattingHelper.isoCountryCode;
            break;
        }
        NumberFormattingHelper.configureFormats();
    }
    private static getCurrencyFromIsoCountryCode(isoCountCode: string): string {
        switch (NumberFormattingHelper.isoCountryCode) {
        case Iso2LetterCountryCodesLowercase.us:
            return "USD";
        case Iso2LetterCountryCodesLowercase.gb:
            return "GBP";
        //
        //Assume that all other countries have the Euro
        //
        default:
            return "EUR";
        }
    }
    private static configureFormats() {

        NumberFormattingHelper.currencyFormat = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "currency",
                currency: this.getCurrencyFromIsoCountryCode(NumberFormattingHelper.isoCountryCode)
            });
        NumberFormattingHelper.currencyFormat0Dp = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "currency",
                currency: this.getCurrencyFromIsoCountryCode(NumberFormattingHelper.isoCountryCode),
                minimumFractionDigits: 0,
                maximumFractionDigits: 0
            });
        NumberFormattingHelper.defaultFormat = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "decimal",
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            });
        NumberFormattingHelper.defaultFormatNoTrailingZeros = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "decimal",
                minimumFractionDigits: 0,
                maximumFractionDigits: 2
            });
        NumberFormattingHelper.defaultFormat0Dp = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "decimal",
                minimumFractionDigits: 0,
                maximumFractionDigits: 0
            });
        NumberFormattingHelper.defaultFormat1Dp = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "decimal",
                minimumFractionDigits: 1,
                maximumFractionDigits: 1
            });
        NumberFormattingHelper.defaultFormat1DpNoTrailingZeros = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "decimal",
                minimumFractionDigits: 0,
                maximumFractionDigits: 1
            });
        NumberFormattingHelper.defaultFormat2Dp = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "decimal",
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            });
        NumberFormattingHelper.defaultFormat2DpNoTrailingZeros = new Intl.NumberFormat(
            NumberFormattingHelper.localeDescriptor,
            {
                style: "decimal",
                minimumFractionDigits: 0,
                maximumFractionDigits: 2
            });
        NumberFormattingHelper.nonCulturealDefaultFormat1Dp = new Intl.NumberFormat(
            "en-us",
            {
                style: "decimal",
                minimumFractionDigits: 1,
                maximumFractionDigits: 1
            });
    }

    private static ensureFormatsExist() {
        if (!NumberFormattingHelper.currencyFormat) {
            NumberFormattingHelper.configureFormats();
        }
    }

    public static formatCurrency(v: number) {
        NumberFormattingHelper.ensureFormatsExist();
        return NumberFormattingHelper.currencyFormat.format(v);
    }

    public static formatCurrency0Dp(v: number) {
        return NumberFormattingHelper.currencyFormat0Dp.format(v);
    }

    public static formatCurrencyLong(v: number) {
        return NumberFormattingHelper.formatCurrency(v);
    }

    public static formatCurrencyWithAffix(v: number, affix: string) {
        return `${NumberFormattingHelper.formatCurrency(v)}  ${affix}`;
    }
    
    public static formatCurrencyWithAffix0Dp(v: number, affix: string) {
        return `${NumberFormattingHelper.formatCurrency0Dp(v)}  ${affix}`;
    }
    
    public static formatCurrencyWithAffixAutoDp(v: number, affix: string) {
        return v >= 10 ? 
            this.formatCurrencyWithAffix0Dp(v, affix) :
            this.formatCurrencyWithAffix(v, affix);
    }

    public static plusMinus(difference: number, format, threshold: number): string {
        if (!difference) {
            return "-";
        } else {
            let res: string = format(difference);
            res = res.replace("+", "").replace("-", "");
            if (difference > threshold) {
                return "+" + res;
            } else {
                if (difference < -threshold) {
                    return "-" + res;
                } else {
                    return "-";
                }
            }
        }
    }

    public static format0Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();
        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return NumberFormattingHelper.defaultFormat0Dp.format(v);
        }
        return "-";
    };

    public static format1Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();
        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return NumberFormattingHelper.defaultFormat1Dp.format(v);
        }
        return "-";
    };

    public static formatAxis1Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();
        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return NumberFormattingHelper.defaultFormat1DpNoTrailingZeros.format(v);
        }
        return "-";
    };

    public static format2Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();
        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return NumberFormattingHelper.defaultFormat2Dp.format(v);
        }
        return "-";
    };
    public static formatAxis2Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();
        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return NumberFormattingHelper.defaultFormat2DpNoTrailingZeros.format(v);
        }
        return "-";
    };

    

    public static formatNps0Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            if (v > 0) {
                return `+${NumberFormattingHelper.defaultFormat0Dp.format(v)}`;
            } else {
                return NumberFormattingHelper.defaultFormat0Dp.format(v);
            }

        } else {
            return "-";
        }
    }

    public static formatNps1Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            if (v > 0) {
                return `+${NumberFormattingHelper.defaultFormat1Dp.format(v)}`;
            } else {
                return NumberFormattingHelper.defaultFormat1Dp.format(v);
            }

        } else {
            return "-";
        }
    }
    public static formatAxisNps1Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            if (v > 0) {
                return `+${NumberFormattingHelper.defaultFormat1DpNoTrailingZeros.format(v)}`;
            } else {
                return NumberFormattingHelper.defaultFormat1DpNoTrailingZeros.format(v);
            }

        } else {
            return "-";
        }
    }

    public static formatNps2Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            if (v > 0) {
                return `+${NumberFormattingHelper.defaultFormat2Dp.format(v)}`;
            } else {
                return NumberFormattingHelper.defaultFormat2Dp.format(v);
            }

        } else {
            return "-";
        }
    }
    public static formatAxisNps2Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            if (v > 0) {
                return `+${NumberFormattingHelper.defaultFormat2DpNoTrailingZeros.format(v)}`;
            } else {
                return NumberFormattingHelper.defaultFormat2DpNoTrailingZeros.format(v);
            }

        } else {
            return "-";
        }
    }

    public static formatUkDressSize1Dp(raw: number): string {
        NumberFormattingHelper.ensureFormatsExist();
        return NumberFormattingHelper.defaultFormatNoTrailingZeros.format(raw);
    }

    public static formatUsDressSize1Dp(raw: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (raw < 0) {
            var rounded = Math.floor(raw);
            var floatingPart = (raw - rounded) / 2.0;
            if (raw >= -1) {
                floatingPart += 0.5;
            }

            var floatingString = NumberFormattingHelper.defaultFormatNoTrailingZeros.format(raw);
            if (floatingString) {
                var index = floatingString.indexOf(".");
                if (index >= 0 && floatingString.length > index + 1) {
                    floatingString = floatingString.substring(index + 1);
                } else {
                    floatingString = "";
                }
            }

            return floatingString && floatingString.length > 0
                ? `00.${floatingString}`
                : "00";
        }

        return NumberFormattingHelper.defaultFormat.format(raw);
    }

    private static isValidNumber(v: number): boolean {
        return !Number.isNaN(v) && Number.isFinite(v);
    }

    public static formatPercentage2Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return `${NumberFormattingHelper.defaultFormat2Dp.format(NumberFormattingHelper.convertFloatToPercentage(v))}%`;
        } else {
            return "-";
        }
    }

    public static formatPercentage1Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return `${NumberFormattingHelper.defaultFormat1Dp.format(NumberFormattingHelper.convertFloatToPercentage(v))}%`;
        } else {
            return "-";
        }
    }
    public static formatAxisPercentage1Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return `${NumberFormattingHelper.defaultFormat1DpNoTrailingZeros.format(NumberFormattingHelper.convertFloatToPercentage(v))}%`;
        } else {
            return "-";
        }
    }
    public static formatAxisPercentage2Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return `${NumberFormattingHelper.defaultFormat2DpNoTrailingZeros.format(NumberFormattingHelper.convertFloatToPercentage(v))}%`;
        } else {
            return "-";
        }
    }

    public static formatNonCulturealDefaultFormat1Dp(v: number): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return `${NumberFormattingHelper.nonCulturealDefaultFormat1Dp.format(NumberFormattingHelper.convertFloatToPercentage(v))}%`;
        } else {
            return "1000%";
        }
    }

    public static formatCount(count: number | undefined): string {
        if (count != null && NumberFormattingHelper.isValidNumber(count)) {
            return count.toLocaleString(undefined, { maximumFractionDigits: 0 });
        }
        return "-";
    }


    public static formatPercentage0Dp(v: number | null): string {
        NumberFormattingHelper.ensureFormatsExist();

        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return `${NumberFormattingHelper.defaultFormat0Dp.format(NumberFormattingHelper.convertFloatToPercentage(v))}%`;
        } else {
            return "-";
        }
    }

    private static convertFloatToPercentage(v: number): number {
        if (v == 0) {
            return 0;
        }
        const verySmallNumberToFixRoundingErrors = 1e-10;
        if (v < 0) {
            return (v * 100) - verySmallNumberToFixRoundingErrors;
        }
        return (v * 100) + verySmallNumberToFixRoundingErrors;
    }

    public static formatPercentage0DpWithSign(v: number): string {
        if (v != null && NumberFormattingHelper.isValidNumber(v)) {
            return (v > 0.00499999 ? "+" : "") + NumberFormattingHelper.formatPercentage1Dp(v);
        } else {
            return "-";
        }
    }

    public static getOrdinalName (value: number): string {
        const lastTwoDigits = value % 100;
        const lastDigit = value % 10;

        let suffix = "th";
        if (lastTwoDigits < 11 || lastTwoDigits > 13) {
            if (lastDigit === 1) {
                suffix = "st";
            } else if (lastDigit === 2) {
                suffix = "nd";
            } else if (lastDigit === 3) {
                suffix = "rd";
            }
        }

        return `${value}${suffix}`;
    }


}
