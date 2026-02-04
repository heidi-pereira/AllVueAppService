import { CuratedFilters } from '../../../filter/CuratedFilters';
import { Metric } from '../../../metrics/metric';
import { PageCardState } from '../shared/SharedEnums';
import React from 'react';
import { ClickPoint, Factory, IAverageDescriptor, RawHeatmapResults } from "../../../BrandVueApi";
import { ViewHelper } from '../ViewHelper';
import { NoDataError } from '../../../NoDataError';
import { PageCardPlaceholder } from './PageCardPlaceholder';
import { FilterInstance } from '../../../entity/FilterInstance';
import TileTemplate from './TileTemplate';
import { BaseExpressionDefinition } from '../../../BrandVueApi';
import { getLowSampleThreshold } from '../BrandVueOnlyLowSampleHelper';
import { useAppSelector } from '../../../state/store';
import { selectSubsetId } from '../../../state/subsetSlice';
import { ITimeSelectionOptions } from "../../../state/ITimeSelectionOptions";
import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface IHeatmapRawDataCardProps {
    metric: Metric;
    filterInstances: FilterInstance[];
    curatedFilters: CuratedFilters;
    baseExpressionOverride: BaseExpressionDefinition | undefined;
    setDataState(state: PageCardState): void;
    setIsLowSample?: (lowSample: boolean) => void;
    setCanDownload?: (canDownload: boolean) => void;
}

async function getData(
    metric: Metric,
    curatedFilters: CuratedFilters,
    filterInstances: FilterInstance[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition): Promise<RawHeatmapResults> {
    if (metric.entityCombination.length > 1) {
        throw new Error("Cannot show heatmap click data for more than one entity");
    }
    if (metric.entityCombination.length == 1 && filterInstances?.length == 0) {
        throw new Error("No filter instance provided for single entity text metric");
    }

    const entityInstanceIds = filterInstances.length > 0 ? [filterInstances[0].instance.id] : [];
    const requestModel = ViewHelper.createCuratedRequestModel(
        entityInstanceIds,
        [metric],
        curatedFilters,
        0,
        { baseExpressionOverride: baseExpressionOverride },
        subsetId,
        timeSelection
    );

    return await Factory.DataClient(throwError => throwError()).getRawHeatmapResults(requestModel);
}

const HeatmapRawDataCard = (props: IHeatmapRawDataCardProps) => {
    const [results, setResults] = React.useState<ClickPoint[]>([])
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const subsetId = useAppSelector(selectSubsetId)
    const timeSelection = useAppSelector(selectTimeSelection);

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);
        if (props.setCanDownload) {
            props.setCanDownload(false);
        }

        getData(props.metric, props.curatedFilters, props.filterInstances, subsetId, timeSelection, props.baseExpressionOverride)
            .then(d => {
                if (!isCancelled) {
                    const isLowSample = d.sampleSizeMetadata.sampleSize.unweighted < getLowSampleThreshold();
                    setResults(d.clickPoints);
                    setIsLoading(false);
                    if (props.setIsLowSample) {
                        props.setIsLowSample(isLowSample)
                    }
                    if (props.setCanDownload) {
                        props.setCanDownload(true);
                    }
                }
            }).catch((e: any) => {
                if (!isCancelled) {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        props.setDataState(PageCardState.NoData);
                    } else {
                        props.setDataState(PageCardState.Error);
                        throw e;
                    }
                }
            });

        return () => { isCancelled = true };
    }, [props.curatedFilters, JSON.stringify(props.filterInstances), props.metric, JSON.stringify(props.baseExpressionOverride), timeSelection]);

    if (isLoading) {
        return (
            <TileTemplate>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    return (
        <>
            <TileTemplate>
                    <div className="page-text-container">
                        {results.map((result, index) => {
                            return <div key={index} className="page-text">
                                {result.reponseId}: {result.xPercent}, {result.yPercent}, {result.timeOffset}
                            </div>
                        })}
                    </div>
            </TileTemplate>
        </>
    );
};

export default HeatmapRawDataCard;