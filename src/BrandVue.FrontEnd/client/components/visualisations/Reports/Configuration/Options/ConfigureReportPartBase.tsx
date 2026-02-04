import { BaseDefinitionType, BaseExpressionDefinition, PartDescriptor } from "../../../../../BrandVueApi";
import { baseExpressionDefinitionsAreEqual } from "../../../../helpers/SurveyVueUtils";
import { PartWithExtraData } from "../../ReportsPageDisplay";
import BaseOptionsSelector from "../../Components/BaseOptionsSelector";

export interface IConfigureReportPartBaseProps {
    reportPart: PartWithExtraData;
    reportBaseTypeOverride: BaseDefinitionType | undefined;
    reportBaseVariableOverride: number | undefined;
    savePartChanges(newPart: PartDescriptor): void;
    canCreateNewBase: boolean | undefined;
}

const ConfigureReportPartBase = (props: IConfigureReportPartBaseProps) => {
    const setBaseProperties = (baseType: BaseDefinitionType | undefined, baseVariableId: number | undefined) => {
        updateBaseDefinition(getBaseExpressionDefinition(baseType, baseVariableId));
    }

    const selectDefaultBase = () => {
        updateBaseDefinition(undefined);
    }

    const updateBaseDefinition = (baseExpressionOverride: BaseExpressionDefinition | undefined) => {
        if (!baseExpressionDefinitionsAreEqual(baseExpressionOverride, props.reportPart.part.baseExpressionOverride)) {
            const modifiedPart = new PartDescriptor(props.reportPart.part);
            modifiedPart.baseExpressionOverride = baseExpressionOverride;
            props.savePartChanges(modifiedPart);
        }
    }

    const getBaseExpressionDefinition = (type: BaseDefinitionType | undefined, baseVariableId: number | undefined): BaseExpressionDefinition | undefined => {
        if ((type || baseVariableId) && props.reportPart.metric) {
            return new BaseExpressionDefinition({
                baseType: type ?? BaseDefinitionType.SawThisQuestion,
                baseVariableId: baseVariableId,
                baseMeasureName: props.reportPart.metric.name
            });
        }
    }

    return (
        <BaseOptionsSelector
            metric={props.reportPart.metric}
            className="configure-base"
            baseType={props.reportPart.part.baseExpressionOverride?.baseType}
            baseVariableId={props.reportPart.part.baseExpressionOverride?.baseVariableId}
            selectDefaultBase={selectDefaultBase}
            setBaseProperties={setBaseProperties}
            defaultBaseType={props.reportBaseTypeOverride}
            defaultBaseVariableId={props.reportBaseVariableOverride}
            canCreateNewBase={props.canCreateNewBase}
            selectedPart={props.reportPart.part.spec2}
            updateLocalMetricBase={() =>{}}
        />
    );
}

export default ConfigureReportPartBase;
