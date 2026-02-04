/**
 * Throws an error if the provided value is null or undefined.
 * @param value The value to check.
 * @param variableNameOrDescription Add information to the error message.
 * @returns The value if it is not null or undefined.
 * @throws {Error} If the value is null or undefined.
 */
export function throwIfNullish<T>(value: T | null | undefined, variableNameOrDescription: string = "value"): T {
    if (value === null || value === undefined) {
        const nullType = value === null ? 'null' : 'undefined';
        const message = `${variableNameOrDescription} should not be ${nullType}.`;
        throw new Error(message);
    }
    return value;
}