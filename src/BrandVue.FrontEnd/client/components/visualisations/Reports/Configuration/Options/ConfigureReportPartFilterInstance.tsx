import {
    MultipleEntitySplitByAndFilterBy,
    PartDescriptor,
} from "../../../../../BrandVueApi";
import { getSplitByAndFilterByEntityTypesForPart } from "../../../../helpers/SurveyVueUtils";
import { PartWithExtraData } from "../../ReportsPageDisplay";
import { useEntityConfigurationStateContext } from "../../../../../entity/EntityConfigurationStateContext";
import style from "./ConfigureReportPartFilterInstance.module.less";
import { PartType } from "client/components/panes/PartType";
import { FilterInstancePicker } from "./FilterInstancePicker";
import { FilterMultiInstancePicker } from "./FilterMultiInstancePicker";

interface IConfigureReportPartFilterInstanceProps {
    reportPart: PartWithExtraData;
    canPickFilterInstances: boolean;
    savePartChanges(newPart: PartDescriptor): void;
} 

const ConfigureReportPartFilterInstance = (props: IConfigureReportPartFilterInstanceProps) => {
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const limitToSingleInstance = props.reportPart.part.partType != PartType.ReportsCardStackedMulti;
    const config = props.reportPart.part.multipleEntitySplitByAndFilterBy

    const selectedEntityTypes = getSplitByAndFilterByEntityTypesForPart(  
            props.reportPart.part,  
            props.reportPart.metric,  
            entityConfiguration);

    const newUpdateSelectedFilterInstances = (newConfig: MultipleEntitySplitByAndFilterBy) => {
        const modifiedPart = new PartDescriptor(props.reportPart.part);
        modifiedPart.multipleEntitySplitByAndFilterBy = newConfig;
        props.savePartChanges(modifiedPart);
    }

    if (props.canPickFilterInstances && selectedEntityTypes) {
        return (
            <div className={style.pickFilterInstances}>
                <label className={style.categoryLabel}>Show results for</label>
                <div className="configure-filter-instances">
                    {selectedEntityTypes.filterByEntityTypes.map(type => {
                        const matchedFilters = config.filterByEntityTypes.filter(t => t.type === type.identifier);
                        const selectedInstanceIds = matchedFilters.map(m => m.instance);
                        const entityInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(type);
                        const selectedInstances = entityInstances.filter(i => selectedInstanceIds.includes(i.id));

                        return limitToSingleInstance ?
                            <FilterInstancePicker
                                entityType={type}
                                selectedInstances={selectedInstances}
                                allInstances={entityInstances}
                                config={config}
                                updatePartWithConfig={newUpdateSelectedFilterInstances}
                                key={type.identifier} />
                            :
                            <FilterMultiInstancePicker
                                entityType={type}
                                selectedInstances={selectedInstances}
                                allInstances={entityInstances}
                                config={config}
                                updatePartWithConfig={newUpdateSelectedFilterInstances}
                                key={type.identifier} />
                    })}
                </div>
            </div>
        );
    }
    return null;
}

export default ConfigureReportPartFilterInstance;