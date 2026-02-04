import React from "react";
import { CalculationType, DateRangeVariableComponent, Subset, VariableGrouping} from "../../../../BrandVueApi";
import { useMetricStateContext } from "../../../../metrics/MetricStateContext";
import { ApplicationConfiguration } from "../../../../ApplicationConfiguration";
import {  getGroupCountAndSample } from "../../Variables/VariableModal/Utils/VariableComponentHelpers";
import { VariableDefinitionCreationService } from "../../Variables/VariableModal/Utils/VariableDefinitionCreationService";
import { VariableContext } from "../../Variables/VariableModal/Utils/VariableContext";
import { useEntityConfigurationStateContext } from "../../../../entity/EntityConfigurationStateContext";
import { VariableCreationService } from "../../Variables/VariableModal/Utils/VariableCreationService";
import { useSavedReportsContext } from "../../Reports/SavedReportsContext";
import { BaseVariableContext } from "../../Variables/BaseVariableContext";
import {useWriteVueQueryParams} from "../../../helpers/UrlHelper";
import {useLocation, useNavigate} from "react-router-dom";
import { useAppDispatch, useAppSelector } from "client/state/store";
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { handleError } from "client/components/helpers/SurveyVueUtils";
import { selectCurrentReport } from "client/state/reportSelectors";

interface ICreateWaveVariableButton {
    applicationConfiguration: ApplicationConfiguration;
    allSubsets: Subset[];
    onCreatedMetricName: (metricName: string)=> void;
}

const CreateWaveVariableButton = (props: ICreateWaveVariableButton) => {
    const [isBusy, setIsBusy] = React.useState<boolean>(false);
    const subset = props.allSubsets[0];
    const dispatch = useAppDispatch();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const {
        googleTagManager,
        pageHandler,
        questionTypeLookup,
    } = React.useContext(VariableContext)
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const variableDefinitionCreationService = new VariableDefinitionCreationService(variables, questionTypeLookup, entityConfiguration)

    const { entityConfigurationDispatch } = useEntityConfigurationStateContext();
    const { metricsDispatch, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { reportsDispatch } = useSavedReportsContext();
    const { baseVariableDispatch } = React.useContext(BaseVariableContext);
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());

    function enumerateMonthsBetweenDates(startingDate: Date, endingDate: Date): Date[] {
        if (startingDate > endingDate) {
            [startingDate, endingDate] = [endingDate, startingDate];
        }

        const months: Date[] = [];
        const startDate = new Date(startingDate.getFullYear(), startingDate.getMonth(), 1);
        const endDate = new Date(endingDate.getFullYear(), endingDate.getMonth(), 1);

        while (startDate <= endDate) {
            const date = new Date(startDate);
            months.push(date);
            startDate.setMonth(startDate.getMonth() + 1);
        }
        return months;
    }

    function getLastDayOfMonth(year: number, month: number): Date {
        const nextMonth = new Date(Date.UTC(year, month+1, 1));
        nextMonth.setUTCMilliseconds(nextMonth.getUTCMilliseconds() - 1);
        return nextMonth;
    }

    const getWaveVariableName = (): string => {
        const existingNamed = metrics.find(x => x.name == "Wave" || x.displayName == "Wave");
        if (existingNamed != undefined) {
            const baseName = "Wave Monthly ";
            for (let index = 0; index < 10; index++) {
                const metric = `${baseName}${index + 1}`;
                if (metrics.find(x => x.displayName.localeCompare(metric, undefined, { sensitivity: 'base' }) == 0) == undefined) {
                    return metric;
                }
            }
        }
        return "Wave"
    }

    const createWaveVariable = () => {
        if (!props.applicationConfiguration.hasLoadedData) {
            handleError("Failed to create a 'Wave Variable' as the data has not loaded");
            return;
        }
        if (props.applicationConfiguration.dateOfFirstDataPoint > props.applicationConfiguration.dateOfLastDataPoint) {
            handleError("Failed to create a 'Wave Variable' as there is no data available")
            return;
        }
        setIsBusy(true);
        const variableCreationService = new VariableCreationService(googleTagManager,
            pageHandler,
            metricsDispatch,
            dispatch,
            baseVariableDispatch,
            entityConfigurationDispatch,
            reportsDispatch);

        const generateWaveVariableName = getWaveVariableName();
        const dates = enumerateMonthsBetweenDates(props.applicationConfiguration.dateOfFirstDataPoint, props.applicationConfiguration.dateOfLastDataPoint);
        const waveVariable = variableDefinitionCreationService.createWaveDefinition();
        waveVariable.groups.pop();
        const promises = dates.map( async (startDate, index) => {
            const startingDate = new Date(Date.UTC(startDate.getFullYear(), startDate.getMonth(), 1));

            const month = startingDate.toLocaleString('default', { month: 'short' });
            const year = startingDate.getFullYear() % 100;
            const endingDate = getLastDayOfMonth(startingDate.getFullYear(), startingDate.getMonth());
            const waveName = `${month} '${year}`;

            const component = new DateRangeVariableComponent();
            component.minDate = startingDate;
            component.maxDate = endingDate;
            const waveGroup: VariableGrouping = new VariableGrouping({
                toEntityInstanceId: index + 1,
                toEntityInstanceName: waveName,
                component: component,
            });
            const result = await getGroupCountAndSample(subset.id, waveGroup)

            const hasValues = result.filter(y => y.count > 0 && y.sample > 0);
            if (hasValues.length > 0) {
                waveVariable.groups.push(waveGroup);
            }
        });

        Promise.all(promises).then(value => {
            const createVariablePromise = variableCreationService.createVariable(generateWaveVariableName, waveVariable, CalculationType.YesNo, setQueryParameter)
            createVariablePromise.then(variableCreated => {
                setIsBusy(false);
                props.onCreatedMetricName(variableCreated.urlSafeMetricName);
            }).catch(error => handleError(error))
        });
    }

    return (<button disabled={isBusy} id='create-wave' className="hollow-button"  onClick={() => createWaveVariable()}>
        <i className="material-symbols-outlined">file_download</i>
        <div>Create Monthly Wave Variable</div>
    </button>)
}
export default CreateWaveVariableButton;