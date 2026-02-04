import { IKnownError } from "./IKnownError";
import { ErrorLevel } from "./BrandVueApi";

export class DataLoadInProgressError extends Error implements IKnownError {
    public static typeDiscriminator = "DataLoadInProgressError";
    public typeDiscriminator = DataLoadInProgressError.typeDiscriminator;
    public logLevel = ErrorLevel.DoNotLog;
    public displayDetailsToUser = false;

    constructor() {
        super("We're updating the site, while this is happening new data and charts are unavailable. These updates usually take less than 5 minutes.");
    }
}