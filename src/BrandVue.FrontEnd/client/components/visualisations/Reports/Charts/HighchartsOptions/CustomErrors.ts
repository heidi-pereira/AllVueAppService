export class OverlapError extends Error {
    constructor(message: string) {
        super(message);
        this.name = "OverlapError";

        Object.setPrototypeOf(this, OverlapError.prototype);
    }
}

export class UnsupportedVariableError extends Error {
    constructor(message: string) {
        super(message);
        this.name = "UnsupportedVariableError";

        Object.setPrototypeOf(this, UnsupportedVariableError.prototype);
    }
}