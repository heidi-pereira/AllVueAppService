import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { Metric } from '../../../../metrics/metric';
import { PageCardState } from '../../shared/SharedEnums';
import React from 'react';
import { IGoogleTagManager } from '../../../../googleTagManager';
import TextCard from '../../shared/TextCard';
import Separator from '../../../helpers/Separator';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { getFilterInstancesForPart, SplitByAndFilterByEntityTypes } from "../../../helpers/SurveyVueUtils";
import { PageHandler } from '../../../PageHandler';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { BaseExpressionDefinition } from '../../../../BrandVueApi';
import { ProductConfigurationContext } from "../../../../ProductConfigurationContext";
import BrandVueOnlyLowSampleHelper from '../../BrandVueOnlyLowSampleHelper';

interface IReportsTableTextPageProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    metric: Metric;
    selectedPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    multipleEntitySplitByAndFilterBy: SplitByAndFilterByEntityTypes;
    baseExpressionOverride: BaseExpressionDefinition | undefined;
    setIsLowSample(lowSample: boolean): void;
}

const ReportsTableTextPage = (props: IReportsTableTextPageProps) => {
    const [dataState, setDataState] = React.useState(PageCardState.Show);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const filterInstances = getFilterInstancesForPart(props.selectedPart.part, props.multipleEntitySplitByAndFilterBy, entityConfiguration).secondaryFilterInstances;
    const { productConfiguration } = React.useContext(ProductConfigurationContext);

    React.useEffect(() => {
        setDataState(PageCardState.Show);
    }, [props.metric]);

    const questionTypeText = (metric: Metric): string => {
        if (metric.isBasedOnCustomVariable) {
            return "Custom variable";
        }

        return metric.calcType.toString();
    };

    const getCardDescription = () => {
        const metricName = props.metric ? props.metric.displayName : props.selectedPart.part.spec1;
        const questionType = props.metric ? questionTypeText(props.metric) : "";
        return (
            <div>
                <div className="name-and-options">
                    <div className="name-and-type">
                        <div className="question-name-text" title={metricName}>{metricName}</div>
                        <Separator />
                        <div className="question-type-text">{questionType}</div>
                    </div>
                </div>
                {filterInstances && filterInstances.length > 0 &&
                    <div className="filter-instance-container">
                        {filterInstances.map(instance =>
                            <div className="filter-instance" key={instance.instance.name}>
                                {instance.type.displayNameSingular}: {instance.instance.name}
                            </div>
                        )}
                    </div>
                }
                </div>
        );
    }

    const getTileContent = () => {
        switch (dataState) {
        case PageCardState.NoData:
            return <div className="reports-card-error no-data">
                       <div>No results</div>
                   </div>;
        case PageCardState.Error:
            return <div className="reports-card-error error">
                       <i className="material-symbols-outlined no-symbol-fill">info</i>
                       <div>There was an error loading results</div>
                   </div>;

        case PageCardState.InvalidQuestion:
            return <div className="reports-card-error invalid-question">
                       <i className="material-symbols-outlined">speaker_notes_off</i>
                       <div>Results can't be shown for this question type</div>
                   </div>;

        case PageCardState.Show:
            return <div className="page-card-full has-link table-card-full">
                       <TextCard
                           googleTagManager={props.googleTagManager}
                           pageHandler={props.pageHandler}
                           metric={props.metric}
                           getDescriptionNode={(isLowSample) => getCardDescription()}
                           filterInstances={filterInstances}
                           curatedFilters={props.curatedFilters}
                           setDataState={setDataState}
                           setIsLowSample={props.setIsLowSample}
                           baseExpressionOverride={props.baseExpressionOverride}
                           lowSampleThreshold={BrandVueOnlyLowSampleHelper.lowSampleForEntity}
                           fullWidth
                        />
                   </div>;
        }
        return <></>;
    };

    return getTileContent();
};

export default ReportsTableTextPage;