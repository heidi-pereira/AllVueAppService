import { ErrorLevel } from "./BrandVueApi";

export interface IKnownError {
    logLevel: ErrorLevel;
    displayDetailsToUser: boolean;
}