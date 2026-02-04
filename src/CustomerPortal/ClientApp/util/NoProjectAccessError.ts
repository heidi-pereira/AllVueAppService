export class NoProjectAccessError extends Error {
    public static typeDiscriminator = "NoProjectAccess";
    public typeDiscriminator = NoProjectAccessError.typeDiscriminator;

    constructor(msg?: string) {
        super(!!msg ? msg : "User does not have permission to access this project.");
    }
}