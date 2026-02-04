import { IKnownError } from "./IKnownError";
import { ErrorLevel } from "./BrandVueApi";

export class NoDataError extends Error implements IKnownError {
    public static typeDiscriminator = "NoDataError";
    public typeDiscriminator = NoDataError.typeDiscriminator;
    displayDetailsToUser = false;
    logLevel = ErrorLevel.DoNotLog;

    constructor(msg?: string) {
        super(!!msg ? msg : "No data for the brands, time period and other filters you have selected.");
    }
}