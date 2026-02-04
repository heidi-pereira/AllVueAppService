import * as BrandVueApi from "./BrandVueApi";
import { DataSubsetManager } from "./DataSubsetManager";
import moment from "moment";
import {
    IReadVueQueryParams,
    IWriteVueQueryParams,
    QueryStringParamNames
} from "./components/helpers/UrlHelper";
import { rangeCalculations } from "./components/helpers/DateHelper";

export class ApplicationConfiguration {
    hasLoadedData: boolean;
    dateOfFirstDataPoint: Date;
    dateOfLastDataPoint: Date;

    dataLoadPromise: Promise<BrandVueApi.ApplicationConfigurationResult> | null;

    public loadConfig(readVueQueryParams: IReadVueQueryParams , writeVueQueryParams: IWriteVueQueryParams): Promise<BrandVueApi.ApplicationConfigurationResult> {
        return BrandVueApi.Factory.ConfigClient(throwErr => throwErr())
            .getApplicationConfiguration(DataSubsetManager.selectedSubset?.id)
            .then((r) => {
                this.dateOfFirstDataPoint = r.dateOfFirstDataPoint;
                this.dateOfLastDataPoint = r.dateOfLastDataPoint;

                // Allow override of "now" for reporting only!
                const now = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.now);
                if (now) {

                    const dateLookups = rangeCalculations(() => moment.utc(now),
                        moment.utc(r.dateOfFirstDataPoint),
                        moment.utc(r.dateOfLastDataPoint));
                    const rangeString = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.range);
                    const rangeDate = dateLookups.find(l => l.url === rangeString);

                    if (rangeDate) {
                        // Clear Now and Range and make Start/End fixed dates
                        writeVueQueryParams.setQueryParameters([
                            { name: QueryStringParamNames.now, value: "" },
                            { name: QueryStringParamNames.range, value: "" },
                            { name: QueryStringParamNames.start, value: rangeDate.start.format("YYYY-MM-DD") },
                            { name: QueryStringParamNames.end, value: now }
                        ]);
                    } else {
                        const startDate = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.start);
                        const endDate = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.end);
                        if (startDate != null && endDate != null) {
                            const diff = moment.utc(endDate).diff(moment.utc(now), "months");
                            const newStart = moment.utc(startDate).subtract(diff, "months");
                            writeVueQueryParams.setQueryParameters([
                                { name: QueryStringParamNames.now, value: "" },
                                { name: QueryStringParamNames.start, value: newStart.format("YYYY-MM-DD") },
                                { name: QueryStringParamNames.end, value: now }
                            ]);
                        }
                        else {
                            writeVueQueryParams.setQueryParameters([
                                { name: QueryStringParamNames.now, value: "" },
                                { name: QueryStringParamNames.end, value: now }
                            ]);
                        }
                    }
                }

                const locale = window.navigator["userLanguage"] || window.navigator["language"]
                //Because of how we calculate start and end dates set First day of week to be monday
                //as the week ends on Sunday..
                //Only tested with samsung where weekly waves end on sundays
                moment.updateLocale(locale, {
                    week: {
                        dow: 1, // First day of week is Monday
                    }
                });

                this.hasLoadedData = r.hasLoadedData;

                if (DataSubsetManager.selectedSubset) {
                    if (this.hasLoadedData) writeVueQueryParams.setQueryParameter("isLoading", undefined);
                    else this.waitForDataReload(readVueQueryParams, writeVueQueryParams);
                }

                return r;
            });
    }

    public waitForDataReload(readVueQueryParams: IReadVueQueryParams, writeQueryParams: IWriteVueQueryParams): Promise<BrandVueApi.ApplicationConfigurationResult> {
        if (this.dataLoadPromise) {
            return this.dataLoadPromise;
        }
        writeQueryParams.setQueryParameter("isLoading", true.toString());
        return this.dataLoadPromise = this.loadConfig(readVueQueryParams,writeQueryParams);
    }
}