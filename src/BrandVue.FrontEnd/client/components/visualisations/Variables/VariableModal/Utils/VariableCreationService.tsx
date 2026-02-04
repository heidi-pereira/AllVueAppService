import * as BrandVueApi from "../../../../../BrandVueApi";
import {
    CalculationType,
    DateRangeVariableComponent,
    GroupedVariableDefinition,
    ReportVariableAppendType,
    SurveyIdVariableComponent,
    VariableDefinition
} from "../../../../../BrandVueApi";
import {QueryStringParamNames} from "../../../../helpers/UrlHelper";
import {ReportWithPage} from "../../../Reports/ReportsPage";
import { IGoogleTagManager } from "../../../../../googleTagManager";
import {PageHandler} from "../../../../PageHandler";
import { MetricAction } from "../../../../../metrics/MetricStateContext";
import { EntityConfigurationAction } from "../../../../../entity/EntityConfigurationStateContext";
import { SavedReportsAction } from "../../../../../components/visualisations/Reports/SavedReportsContext";
import { checkHasGroupedGroupsEntityTypes } from './VariableComponentHelpers';
import { BaseVariableAction } from "../../BaseVariableContext";
import { fetchVariableConfiguration } from "client/state/variableConfigurationsSlice";
import { useAppDispatch } from "client/state/store";
import { handleError } from "client/components/helpers/SurveyVueUtils";

export class VariableCreationService {
    private _googleTagManager: IGoogleTagManager;
    private readonly _pageHandler: PageHandler;
    private _currentReportPage?: ReportWithPage;
    private _metricsDispatch: (action: MetricAction) => Promise<void>;
    private _variablesDispatch: ReturnType<typeof useAppDispatch>;
    private _baseVariablesDispatch: (action: BaseVariableAction) => Promise<void>;
    private _entityConfigurationDispatch: (action: EntityConfigurationAction) => Promise<void>;
    private _reportsDispatch: (action: SavedReportsAction) => Promise<void>;

    constructor(googleTagManager: IGoogleTagManager,
        pageHandler: PageHandler,
        metricsDispatch: (action: MetricAction) => Promise<void>,
        variablesDispatch: ReturnType<typeof useAppDispatch>,
        baseVariablesDispatch: (action: BaseVariableAction) => Promise<void>,
        entityConfigurationDispatch: (action: EntityConfigurationAction) => Promise<void>,
        reportsDispatch: (action: SavedReportsAction) => Promise<void>,
        currentReportPage?: ReportWithPage)
    {
        this._googleTagManager = googleTagManager;
        this._pageHandler = pageHandler;
        this._currentReportPage = currentReportPage;
        this._metricsDispatch = metricsDispatch;
        this._variablesDispatch = variablesDispatch;
        this._baseVariablesDispatch = baseVariablesDispatch;
        this._entityConfigurationDispatch = entityConfigurationDispatch;
        this._reportsDispatch = reportsDispatch;
    }

    public awaitRefresh = async () => {
        await this._entityConfigurationDispatch({type: "RELOAD_ENTITYCONFIGURATION"});
        await this._variablesDispatch(fetchVariableConfiguration());
        await this._metricsDispatch({type: "RELOAD_METRICS"});
        await this._reportsDispatch({type: "TRIGGER_RELOAD"});
    }

    public refreshBases = async () => {
        await this._entityConfigurationDispatch({type: "RELOAD_ENTITYCONFIGURATION"});
        await this._variablesDispatch(fetchVariableConfiguration());
        await this._baseVariablesDispatch({type: 'RELOAD_BASE_VARIABLES'});
        await this._reportsDispatch({type: "TRIGGER_RELOAD"});
    }

    public createVariable = async (variableName: string, variableDefinition: VariableDefinition, calculationType: CalculationType, setQueryParameter: (name: string, value: (string | number | string[] | number[] | undefined)) => void, shouldSetQueryParamOnCreate?: boolean, selectedPart?: string, appendType?: ReportVariableAppendType) => {
        const updatedVariableDefinition = checkHasGroupedGroupsEntityTypes(variableDefinition, variableName);
        const model = this.getVariableCreationModel(variableName, updatedVariableDefinition, selectedPart, appendType);
        model.calculationType = calculationType;
        const event = this.isWaveVariable(variableDefinition) ? 'variablesCreateWaves' : 'variablesCreate';
        this._googleTagManager.addEvent(event, this._pageHandler);
        const result = await BrandVueApi.Factory.VariableConfigurationClient((_) => {})
            .create(model)
            .then(async (r) => {
                await this.awaitRefresh();
                return r;
            });
        if (shouldSetQueryParamOnCreate) {
            setQueryParameter(QueryStringParamNames.urlSafeMetricName, result.urlSafeMetricName);
        }
        return result;
    }

    public createBase = async (variableName: string, variableDefinition: VariableDefinition, selectedPart?: string) => {
        const verifiedVariableDefinition = checkHasGroupedGroupsEntityTypes(variableDefinition, variableName);
        const model = this.getVariableCreationModel(variableName, verifiedVariableDefinition, selectedPart, ReportVariableAppendType.Base);

        this._googleTagManager.addEvent("baseVariablesCreate", this._pageHandler);
        return await BrandVueApi.Factory.VariableConfigurationClient((_) => {})
            .createBaseVariable(model)
            .then((r) => {
                this.refreshBases();
                return r;
            });
    }

    public updateVariable = async (variableIdToView: number, variableName: string, variableDefinition: VariableDefinition, calculationType: CalculationType) => {
        const event = this.isWaveVariable(variableDefinition) ? 'variablesUpdateWaves' : 'variablesUpdate';
            this._googleTagManager.addEvent(event, this._pageHandler);
            await BrandVueApi.Factory.VariableConfigurationClient((_) => {})
                .update(variableIdToView, variableName, variableDefinition, calculationType)
                .then(() => this.awaitRefresh());
    }

    public updateBase = async (variableIdToView: number, variableName: string, variableDefinition: VariableDefinition) => {
            this._googleTagManager.addEvent("baseVariablesUpdate", this._pageHandler);
            await BrandVueApi.Factory.VariableConfigurationClient((_) => {})
                .update(variableIdToView, variableName, variableDefinition, null)
                .then(() => this.refreshBases());
    }

    public createFlattenedMultiEntity = async (variableName: string, variableDefinition: VariableDefinition, calculationType: CalculationType) => {
        const updatedVariableDefinition = checkHasGroupedGroupsEntityTypes(variableDefinition, variableName);
        const model = this.getVariableCreationModel(variableName, updatedVariableDefinition);
        model.calculationType = calculationType;
        await BrandVueApi.Factory.VariableConfigurationClient((_) => {})
            .flattenMultiEntityVariable(model)
            .then(() => this.awaitRefresh());
    }

    private getVariableCreationModel = (variableName: string, variableDefinition: VariableDefinition, selectedPart?: string, appendType?: ReportVariableAppendType) => {
        const getReportSettings = () => {
            if(this._currentReportPage && appendType) {
                return new BrandVueApi.VariableConfigurationReportSettings({
                    reportIdToAppendTo: this._currentReportPage.report.savedReportId,
                    selectedPart: selectedPart,
                    appendType: appendType
                });
            }
            return undefined;
        }

        return new BrandVueApi.VariableConfigurationCreateModel({
            name: variableName,
            definition: variableDefinition,
            reportSettings: getReportSettings()
        });
    }

    private isWaveVariable(variableDefinition: VariableDefinition): boolean {
        if (variableDefinition instanceof GroupedVariableDefinition) {
            return variableDefinition.groups.every(g => g.component instanceof DateRangeVariableComponent || g.component instanceof SurveyIdVariableComponent);
        }
        return false;
    }
}