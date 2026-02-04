import {
    PartDescriptor,
    Report,
    ReportType,
} from "../../../../BrandVueApi";
import { PartWithExtraData } from "../ReportsPageDisplay";
import { PartType } from "../../../panes/PartType";
import ConfigureReportPartSplitBy from "./Options/ConfigureReportPartSplitBy";
import ConfigureReportPartOrderBy from "./Options/ConfigureReportPartOrderBy";
import ConfigureReportPartBase from "./Options/ConfigureReportPartBase";
import ConfigureReportShowAverage from "./Options/ConfigureReportShowAverage";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { PageHandler } from "../../../PageHandler";
import { IConfigureNets } from "./ConfigureNets";
import ConfigureReportPartHeatMapOptions from "./Options/ConfigureReportPartHeatMapOptions";
import { canSelectFilterInstances } from "../Charts/ReportsChartHelper";
import ConfigureReportPartExportOptions from "client/components/visualisations/Reports/Configuration/Options/ConfigureReportPartExportOptions";
import { useAppSelector } from "client/state/store";
import { selectCurrentReport } from "client/state/reportSelectors";

interface IConfigureReportPartOptionsTabProps {
    reportPart: PartWithExtraData;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    savePartChanges(newPart: PartDescriptor): void;
    isUsingBreaks: boolean;
    canCreateNewBase: boolean | undefined;
    isNotText: boolean;
    configureNets: IConfigureNets;
    isHeatMap: boolean;
}

const ConfigureReportPartOptionsTab = (props: IConfigureReportPartOptionsTabProps) => {
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const canChangeBase = !props.isHeatMap && props.reportPart.part.partType !== PartType.ReportsCardFunnel;
    const canChangeSplitBy = props.reportPart.part.partType !== PartType.ReportsCardFunnel;
    const canShowAverages = props.isNotText && props.reportPart.part.partType !== PartType.ReportsCardFunnel;
    const canPickFilterInstances = canSelectFilterInstances(props.reportPart, report);
    const canShowExportOptions = report.reportType === ReportType.Chart && props.isNotText && !props.isHeatMap;

    return (
        <div className="configure-options">
            {canChangeBase &&
                <ConfigureReportPartBase
                    reportPart={props.reportPart}
                    reportBaseTypeOverride={report.baseTypeOverride}
                    reportBaseVariableOverride={report.baseVariableId}
                    savePartChanges={props.savePartChanges}
                    canCreateNewBase={props.canCreateNewBase}
                />
            }
            {props.isNotText &&
                    <ConfigureReportPartOrderBy
                        reportType={report.reportType}
                        reportOrderBy={report.reportOrder}
                        part={props.reportPart.part}
                        savePartChanges={props.savePartChanges}
                    />
            }
            {canChangeSplitBy && 
                <ConfigureReportPartSplitBy
                    reportPart={props.reportPart}
                    canPickFilterInstances={canPickFilterInstances}
                    savePartChanges={props.savePartChanges}
                />}
            {canShowAverages &&
                <ConfigureReportShowAverage
                    part={props.reportPart.part}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    savePartChanges={props.savePartChanges}
                    averageTypes={props.reportPart.part.averageTypes}
                    displayMeanValues={props.reportPart.part.displayMeanValues}
                    displayStandardDeviation={props.reportPart.part.displayStandardDeviation}
                    metric={props.reportPart.metric}
                    selectedInstanceIds={props.reportPart.part.selectedEntityInstances?.selectedInstances ?? props.configureNets.availableEntityInstances.map(i => i.id)}
                />
            }
            {props.isHeatMap && 
                <ConfigureReportPartHeatMapOptions
                part={props.reportPart.part}
                savePartChanges={props.savePartChanges}
                options={props.reportPart.part.customConfigurationOptions}
                />
            }
            {canShowExportOptions &&
                <ConfigureReportPartExportOptions
                    part={props.reportPart.part}
                    savePartChanges={props.savePartChanges}
                />
            }
        </div>
    );
}

export default ConfigureReportPartOptionsTab;