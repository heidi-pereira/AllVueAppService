import React from "react";
import {
    PartDescriptor,
    Report,
} from "../../../../BrandVueApi";
import { PartWithExtraData } from "../ReportsPageDisplay";
import ConfigureReportPartDisplayName from "./Options/ConfigureReportPartDisplayName";
import ConfigureReportPartShowTop from "./Options/ConfigureReportPartShowTop";
import ConfigureReportPartChartType from "./Options/ConfigureReportPartChartType";
import ConfigureReportPartEntityInstances from "./Options/ConfigureReportPartEntityInstances";
import {IConfigureNets} from "./ConfigureNets";
import { PartType } from "../../../panes/PartType";

interface IConfigureReportPartQuestionTabProps {
    reportPart: PartWithExtraData;
    savePartChanges(newPart: PartDescriptor): void;
    isUsingOverTime: boolean;
    isUsingWaves: boolean;
    isUsingBreaks: boolean;
    setIsSidePanelOpen: (boolean) => void;
    configureNets: IConfigureNets;
    isNotText: boolean;
}

const ConfigureReportPartQuestionTab = (props: IConfigureReportPartQuestionTabProps) => {
    const canNet = props.reportPart.part.partType !== PartType.ReportsCardFunnel;

    return (
        <div className="configure-options">
            <ConfigureReportPartDisplayName
                part={props.reportPart.part}
                savePartChanges={props.savePartChanges}
            />
            <ConfigureReportPartChartType
                reportPart={props.reportPart}                
                savePartChanges={props.savePartChanges}
                isUsingOverTime={props.isUsingOverTime}
                isUsingWaves={props.isUsingWaves}
                isUsingBreaks={props.isUsingBreaks}
            />

            {
                props.isNotText &&
                    <ConfigureReportPartShowTop
                    part={props.reportPart.part}
                    entityInstanceCount={props.configureNets.availableEntityInstances.length}
                    savePartChanges={props.savePartChanges}
                    />
            }

            {canNet && <ConfigureReportPartEntityInstances
                reportPart={props.reportPart}
                savePartChanges={props.savePartChanges}
                setIsSidePanelOpen={props.setIsSidePanelOpen}
                configureNets={props.configureNets}
            />}
        </div>
    );
}

export default ConfigureReportPartQuestionTab;