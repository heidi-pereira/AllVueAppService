const defaultDecimalPlaces = 5;

export const preciseRound = (input: number, decimalPlaces: number = defaultDecimalPlaces) => {
    const factorOfTen = 10 ** decimalPlaces;
    return Math.round(input * factorOfTen) / factorOfTen;
}

export const toPercentage = (input: number, decimalPlaces: number = defaultDecimalPlaces) => preciseRound(input * 100, decimalPlaces);

export const preciseSum = (inputs: (number | undefined)[], decimalPlaces: number = defaultDecimalPlaces) => {
    const sum = inputs.reduce<number>((rollingSum, currentNumber) => rollingSum + (currentNumber ?? 0), 0);
    return preciseRound(sum, decimalPlaces);
}