export default class Globals {

    private static isInitialized: boolean = false;

    public static reset(): void {
        this.isInitialized = false;
    }

    public static init(): void {
        if (this.isInitialized) {
            throw Error("Globals is already initialized.");
        }

        this.isInitialized = true;
    }

}