export class StringHelper {
    public static sharedPrefixIncludingColon(array: string[]): string {
        const DefaultValue = "";
        if (array.length <= 1) {
            return DefaultValue;
        }
        
        const preColonStrings = array.map(s => s.split(":")[0]);
        const firstPreColonString = preColonStrings[0];
        const prefixesTheSame = preColonStrings.every(p => p === firstPreColonString);
        return prefixesTheSame ? `${firstPreColonString}:`: DefaultValue;
    }
    public static replaceJSX(input: string, searchString: string, replace: JSX.Element) {
        const parts = input.split(searchString);
        const result = [] as (string | JSX.Element)[];
        for (let i = 0; i < parts.length; i++) {
            result.push(parts[i]);
            if (i < parts.length - 1)
                result.push(replace);
        }
        return result;
    }

    public static replaceJSXMulti(input: string, stringReplacements: { searchString: string, replace: JSX.Element }[]) {
        // Unzip the input string into two arrays - one for [placeholders] and one for non-placeholders
        const regExpForPlaceholders = /\[([^\][]+)]/g;

        const nonPlaceholderText: string[] = input.replace(regExpForPlaceholders, "[]").split("[]");
        let placeholders: (string | JSX.Element)[] = Array.from(input.matchAll(regExpForPlaceholders), m => m[0]) ?? [];

        // Replace placeholder strings with JSX (if match found)
        for (let i = 0; i < placeholders.length; i++) {
            const replacementJSX = stringReplacements.find(sr => sr.searchString === placeholders[i]);
            if (replacementJSX) {
                placeholders[i] = replacementJSX.replace;
            }
        }

        // Zip arrays back together
        let inputWithReplacements: (string | JSX.Element)[] = [];

        const biggestArr = nonPlaceholderText.length > placeholders.length ? nonPlaceholderText : placeholders;
        const smallerArr = nonPlaceholderText.length < placeholders.length ? nonPlaceholderText : placeholders;

        for (var i = 0; i < biggestArr.length; i++) {
            inputWithReplacements.push(biggestArr[i]);
            const smallerArrValue = smallerArr[i];
            if (smallerArrValue) {
                inputWithReplacements.push(smallerArrValue);
            }
        }

        return inputWithReplacements;
    }

    public static formatBaseVariableName(displayName: string): string {
        return displayName.replace(/ base/i, "");
    }
}