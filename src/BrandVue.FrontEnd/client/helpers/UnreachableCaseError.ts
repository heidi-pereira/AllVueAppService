// Used for enforcing exhaustive checks in switch statements or conditional logic
// The `never` type makes it so that it would generate a compile-time error if not all cases are handled
export class UnreachableCaseError extends Error {
    constructor(val: never) {
        super(`Unexpected value: ${val}`);
        Object.setPrototypeOf(this, UnreachableCaseError.prototype);
        this.name = 'UnreachableCaseError';
    }
}