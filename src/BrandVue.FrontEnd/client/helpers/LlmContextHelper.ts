// This file implements a text subsetting algorithm to reduce an array of text items so it can
// fit in an LLM's context window. We could simply chop the tail off the array, but this would
// result in a poor distribution of text items. Instead, we want to keep a representative subset.

// Example: we want 0.42 of the input, which is comes to 3/7.
// Input: [■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■,■]
// BAD:   [■,■,■,■,■,■,■,■,■,■,■,■, , , , , , , , , , , , , , , , ] (simply chopping off the tail)
// BETTER:[■,■,■, , , , ,■,■,■, , , , ,■,■,■, , , , ,■,■,■, , , , ] (keeping a distributed subset)

// Algorithm:
// Assumes most text items in the input array are of a similar length.
// 1) Guess a proportion of the array to keep by simply dividing the maximum length by the total length of all strings in the array.
// 2) Convert proportion into a fraction such as "3/7".
// 3) We then filter the array by "index % denominator < nominator" to get the subset.
// 4) If the subset is still too large because of the assumption, we reduce the proportion slightly and loop as necessary.

/**
 * Returns a subset of items from an array of strings such that the combined text length of the subset is less than or equal to a given maximum length.
 * @param arr Array of strings
 * @param maxLength Maximum length of the combined text
 * @returns Subset of arr
 */
export function getSubsetToMaxLength(arr: string[], maxLength: number): string[] {
    const totalLength = arr.reduce((sum, str) => sum + str.length, 0);

    if (totalLength <= maxLength) {
        return arr;
    }

    let currentFraction = maxLength / totalLength;

    let subset: string[];
    do {
        subset = getSubset(arr, currentFraction);
        currentFraction -= 0.01;
    } while (subset.reduce((sum, str) => sum + str.length, 0) > maxLength && currentFraction > 0);

    return subset;
}

function getSubset(arr: string[], fraction: number): string[] {
    const [nominator, denominator] = nearestFraction(fraction);
    return arr.filter((_, index) => index % denominator < nominator);
}

/**
 * Returns the fraction that is closest to a given number. E.g. 0.25 -> 1/4, 0.75 -> 3/4...
 * @param num Number between 0 and 1
 * @returns Fraction as a tuple of numerator and denominator
 * @throws Error if num is not between 0 and 1, or otherwise invalid. * 
 */
export function nearestFraction(num: number): [number, number] {
    if (num < 0 || num > 1) {
        throw new Error("Input must be between 0 and 1");
    }

    if (num === 0) return [0, 1];
    if (num === 1) return [1, 1];

    let bestNumerator = 0;
    let bestDenominator = 1;
    let bestDifference = 1;

    for (let denominator = 1; denominator <= 100; denominator++) {
        const numerator = Math.round(num * denominator);
        const difference = Math.abs(num - numerator / denominator);

        if (difference < bestDifference) {
            bestNumerator = numerator;
            bestDenominator = denominator;
            bestDifference = difference;
        }
    }

    const gcd = getGCD(bestNumerator, bestDenominator);
    return [bestNumerator / gcd, bestDenominator / gcd];
}

function getGCD(a: number, b: number): number {
    return b === 0 ? a : getGCD(b, a % b);
}