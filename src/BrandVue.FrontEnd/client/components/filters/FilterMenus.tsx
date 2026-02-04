import React, { useEffect, useState } from "react";
import DateRangePicker from "./DateRangePicker";
import AverageSelector from "./AverageSelector";
import { ICommonVariables, ActionEventName } from "../../googleTagManager";
import { IAverageDescriptor } from "../../BrandVueApi";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { QueryStringParamNames, useReadVueQueryParams } from "../helpers/UrlHelper";
import { selectBestAverage } from "../helpers/AveragesHelper";
import moment from "moment";
import { getStartEndDateUTCFromUrl } from "../helpers/DateHelper";
import {useWriteVueQueryParams} from "../helpers/UrlHelper";
import { useLocation, useNavigate } from "react-router-dom";

interface IFilterMenuProps {
    average: IAverageDescriptor;
    configuration: ApplicationConfiguration
    addEvent: (eventName: ActionEventName, values?: ICommonVariables | null) => void;
    userVisibleAverages: IAverageDescriptor[];
    updateFilterStartEnd: (start: Date, end: Date) => void;
    updateFilterAverage: (average: IAverageDescriptor) => void;
}

const FilterMenus: React.FunctionComponent<IFilterMenuProps> = (props) => {
    const { addEvent, average, userVisibleAverages, configuration, updateFilterStartEnd, updateFilterAverage } = props;
    const writeVueQueryParams = useWriteVueQueryParams(useNavigate(), useLocation());
    const readVueQueryParams = useReadVueQueryParams();
    const [previousPropsAverageId, setPreviousPropsAverageId] = useState(props.average.averageId);
    const {start, end} = getStartEndDateUTCFromUrl(props.configuration.dateOfFirstDataPoint, props.configuration.dateOfLastDataPoint, true, readVueQueryParams, writeVueQueryParams);

    useEffect(() => {
        updateFilterStartEnd(start.toDate(), end.toDate())

        const averageId = readVueQueryParams.getQueryParameter<string>("Average");
        const average = selectBestAverage(props.userVisibleAverages, averageId);
        if (average && previousPropsAverageId !== average.averageId) { //Handles the case where the possible list of averages could change
            updateFilterAverage(average);
            setPreviousPropsAverageId(props.average.averageId)
        }
    }, []);

    const onDateChanged = (startDate: Date, endDate: Date, range: string | undefined) => {
        updateFilterStartEnd(startDate, endDate);

        if (!props.configuration.hasLoadedData) return;

        if (range) {
            applyDateQueryParameters("", "", range);
        } else {
            const startDateFormatted = moment.utc(startDate).format("YYYY-MM-DD");
            const endDateFormatted = moment.utc(endDate).format("YYYY-MM-DD");
            applyDateQueryParameters(startDateFormatted, endDateFormatted, "");
            addEvent("changeDate", { startDate: startDateFormatted, endDate: endDateFormatted });
        }
    };

    const applyDateQueryParameters = (start: string, end: string, rangeString: string) => {
        setQueryParameters([
            { name: QueryStringParamNames.range, value: rangeString },
            { name: QueryStringParamNames.start, value: start },
            { name: QueryStringParamNames.end, value: end }]);
    };
    const { setQueryParameters } = useWriteVueQueryParams(useNavigate(), useLocation());
    return (
        <>
            <DateRangePicker
                dateOfFirstDataPoint={configuration.dateOfFirstDataPoint}
                dateOfLastDataPoint={configuration.dateOfLastDataPoint}
                startDate={start}
                endDate={end}
                onDateChanged={onDateChanged}
            />
            {userVisibleAverages.length > 0 &&
                <AverageSelector average={average}
                    userVisibleAverages={userVisibleAverages}
                    updateFilterAverage={updateFilterAverage}
                />
            }
        </>
    );
};

export default FilterMenus;