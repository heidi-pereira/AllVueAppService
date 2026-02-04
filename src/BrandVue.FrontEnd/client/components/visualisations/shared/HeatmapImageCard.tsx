import styles from './HeatmapImageCard.module.less';
import { CuratedFilters } from '../../../filter/CuratedFilters';
import { Metric } from '../../../metrics/metric';
import { PageCardState } from '../shared/SharedEnums';
import React from 'react';
import * as BrandVueApi from "../../../BrandVueApi";
import { ViewHelper } from '../ViewHelper';
import { NoDataError } from '../../../NoDataError';
import { FilterInstance } from '../../../entity/FilterInstance';
import TileTemplate from "./TileTemplate";
import {
    BaseExpressionDefinition,
    CustomConfigurationOptions,
    HeatmapClickStats,
    HeatMapOptions,
    HeatmapOverlayRequestModel,
    HeatMapReportOptions,
    IApplicationUser,
    SampleSizeMetadata,
} from "../../../BrandVueApi";
import Throbber from '../../throbber/Throbber';
import AllVueDescriptionFooter from "../AllVueDescriptionFooter";
import { Tooltip } from '@mui/material';
import { defaultHeatMapOptions, getOptionsWithDefaultFallbacks } from '../../helpers/HeatMapHelper';
import { useAppSelector } from '../../../state/store';
import { selectSubsetId } from '../../../state/subsetSlice';
import { selectCurrentReportOrNull } from '../../../state/reportSelectors';
import { ITimeSelectionOptions } from "../../../state/ITimeSelectionOptions";
import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface IHeatmapImageCardProps {
    metric: Metric;
    filterInstances: FilterInstance[];
    curatedFilters: CuratedFilters;
    getDescriptionNode?: (isLowSample: boolean) => JSX.Element;
    baseExpressionOverride: BaseExpressionDefinition | undefined;
    setDataState(state: PageCardState): void;
    setCanDownload?: (canDownload: boolean) => void;
    displayFooter: boolean;
    heatmapOptions: HeatMapOptions | CustomConfigurationOptions | undefined;
    user: IApplicationUser | null;
    // optional override when no report is available
    decimalPlaces?: number;
}

async function getData(
    metric: Metric,
    curatedFilters: CuratedFilters,
    filterInstances: FilterInstance[],
    heatmapOptions: HeatMapOptions,
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition): Promise<BrandVueApi.HeatmapImageResult> {
    if (metric.entityCombination.length > 1) {
        throw new Error("Cannot show heatmap click data for more than one entity");
    }
    if (metric.entityCombination.length == 1 && filterInstances?.length == 0) {
        return new Promise<BrandVueApi.HeatmapImageResult>((result, other) => { });
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
    const heatmapRequestModel = new HeatmapOverlayRequestModel({ resultsModel: requestModel, heatMapOptions: heatmapOptions });
    return await BrandVueApi.Factory.DataClient(throwError => throwError()).getHeatmapImageOverlay(heatmapRequestModel);
}

const convertOptions = (reportOptions: HeatMapOptions | CustomConfigurationOptions | undefined): HeatMapOptions => {
    if (reportOptions === undefined) {
        return defaultHeatMapOptions();
    } 
    if (reportOptions instanceof HeatMapReportOptions) {
        const heatmapOptions = new HeatMapOptions({
            radius: reportOptions.radiusInPixels,
            intensity: reportOptions.intensity,
            overlayTransparency: reportOptions.overlayTransparency,
            keyPosition: reportOptions.keyPosition,
            displayKey: reportOptions.displayKey,
            displayClickCounts: reportOptions.displayClickCounts,
        });
        return heatmapOptions;
    }
    return reportOptions as HeatMapOptions;
}

const HeatmapImageCard = (props: IHeatmapImageCardProps) => {
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [heatmapDataUrl, setHeatmapDataUrl] = React.useState<string>();
    const [baseImageUrl, setBaseImageUrl] = React.useState<string>();
    const [keyImageDataUrl, setKeyImageDataUrl] = React.useState<string>();
    const [overlayOpacity, setOverlayOpacity] = React.useState<number>(0.5);
    const [instanceName, setInstanceName] = React.useState<string>();
    const [sampleSizeMetadata, setSampleSizeMetadata] = React.useState<SampleSizeMetadata>();
    const [heatmapClickStats, setHeatmapClickStats] = React.useState<HeatmapClickStats | undefined>(undefined);
    const [heatmapOptions, setHeatmapOptions] = React.useState<HeatMapOptions>(convertOptions(props.heatmapOptions));
    const subsetId = useAppSelector(selectSubsetId)
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReportOrNull);
    const report = currentReportPage?.report;
    const decimalPlaces = report?.decimalPlaces ?? props.decimalPlaces ?? 0;

    const loadHeatmap = () => {
        let isCancelled = false;
        setIsLoading(true);
        setHeatmapDataUrl("");
        setHeatmapClickStats(undefined);
        if (props.filterInstances.length > 0 && props.filterInstances[0].instance) {
            var selectedItem = props.filterInstances[0].instance;
            setBaseImageUrl(selectedItem.imageUrl);
            setInstanceName(selectedItem.name);
        }
        if (props.setCanDownload) {
            props.setCanDownload(false);
        }

        getData(props.metric, props.curatedFilters, props.filterInstances, heatmapOptions, subsetId, timeSelection, props.baseExpressionOverride)
            .then(d => {
                if (!isCancelled) {
                    if (d.baseImageUrl != baseImageUrl) {
                        setBaseImageUrl(d.baseImageUrl);
                    }
                    setHeatmapOptions(d.heatMapOptions);
                    setSampleSizeMetadata(d.sampleSizeMetadata)
                    setHeatmapClickStats(d.heatmapClickStats);
                    setOverlayOpacity(1 - d.heatMapOptions.overlayTransparency!)
                    setHeatmapDataUrl("data:image/png;base64," + d.overlayImage);
                    setKeyImageDataUrl("data:image/png;base64," + d.keyImage);
                    setIsLoading(false);
                }
            }).catch((e: any) => {
                if (!isCancelled) {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        props.setDataState(PageCardState.NoData);
                        setIsLoading(false);
                    } else {
                        props.setDataState(PageCardState.Error);
                        setIsLoading(false);
                        throw e;
                    }
                }
            });

        return () => { isCancelled = true };
    };

    React.useEffect(() => {
        loadHeatmap();
    }, [props.curatedFilters, JSON.stringify(props.filterInstances), props.metric, JSON.stringify(props.baseExpressionOverride), timeSelection]);

    React.useEffect(() => {
        const delayDebounceFn = setTimeout(() => {
            loadHeatmap();
        }, 250)

        return () => clearTimeout(delayDebounceFn);
    }, [JSON.stringify([heatmapOptions.radius, heatmapOptions.intensity])])

    React.useEffect(() => {
        const options = convertOptions(props.heatmapOptions);
        const optionsWithDefaults = getOptionsWithDefaultFallbacks(options);
        setHeatmapOptions(optionsWithDefaults);
        if (options) {
            setOverlayOpacity(1 - options.overlayTransparency!);
        }
    }, [JSON.stringify(props.heatmapOptions)])

    const heatmapStatistics = () => (
        <>
            {heatmapClickStats &&
                <div className="allvue-description-footer">
                    {props.user && props.user.doesUserHaveAccessToInternalSavantaSystems &&
                        heatmapClickStats.numberOfRespondentsWithDataErrors != undefined &&
                        heatmapClickStats.numberOfRespondentsWithDataErrors > 0 &&
                        <>
                            <div className="footer-element">{heatmapClickStats.numberOfRespondentsWithDataErrors} Respondents with data errors</div>
                        </>
                    }
                    {heatmapClickStats.averageClicksPerRespondent != undefined && <div className="footer-element">Average clicks is {heatmapClickStats.averageClicksPerRespondent.toFixed(decimalPlaces)}</div>}
                    {heatmapClickStats.averageClicksPerRespondentWhoHaveClicked != undefined && <div className="footer-element">Average clicks is Average clicks (for respondents with at least 1 click) {heatmapClickStats.averageClicksPerRespondentWhoHaveClicked.toFixed(decimalPlaces)}</div>}
                    {heatmapClickStats.averageTimeBetweenClicks != undefined && <div className="footer-element">Average time between clicks is {heatmapClickStats.averageTimeBetweenClicks.toFixed(decimalPlaces)} seconds</div>}
                    {heatmapClickStats.averageTimeSpentClickingPerRespondent != undefined && <div className="footer-element">Average time between first and last click is {heatmapClickStats.averageTimeSpentClickingPerRespondent.toFixed(decimalPlaces)} seconds</div>}
                    {heatmapClickStats.numberOfClicksPerRespondent != undefined && <div className="footer-element">{heatmapClickStats.numberOfClicksPerRespondent[0]} Respondents not clicked</div>}
                </div>
            }
        </>
    );

    const heatmapFooter = () => (
        <AllVueDescriptionFooter
            sampleSizeMeta={sampleSizeMetadata}
            metric={props.metric}
            filterInstanceNames={[]}
            baseExpressionOverride={props.baseExpressionOverride}
            isSurveyVue={true}
            footerAverages={undefined}
            decimalPlaces={decimalPlaces}
            extraRows={instanceName ? [`"${instanceName}"`] : []}
        />
    );

    const getDescriptionNode = () => {
        if (props.getDescriptionNode) {
            return (props.getDescriptionNode(false));
        }
        return (<div />);
    };

    const getBaseImageOpacity = () => {
        if (isLoading || heatmapDataUrl == undefined) {
            return "0.5";
        }
        return "1";
    };

    const getTooltip = () => {
        if (!baseImageUrl) {
            if (props.user?.doesUserHaveAccessToInternalSavantaSystems) {
                let reason =`Metric: '${props.metric.name}' VarCode: '${props.metric.varCode}' Entity '${props.metric.entityCombination.map(x=>x.identifier).join(",")}' `;
                if (props.filterInstances.length == 0) {
                    reason += "No filter instances provided.";
                }
                else if (props.filterInstances[0].instance == undefined) {
                    reason += `Filter instance not set '${props.filterInstances[0].type.identifier}'`;
                }
                else {
                    reason += `Named Item: '${instanceName}'      Image: '${baseImageUrl}'`
                }
                return `No logo specified for choice. ${reason}`;
            }
            return `Unable to find the image related to the heatmap.`;
        }
        if (!heatmapClickStats) {
            return "Waiting...";
        }
        return (
            <div>
                {props.user && props.user.doesUserHaveAccessToInternalSavantaSystems &&
                    heatmapClickStats.numberOfRespondentsWithDataErrors != undefined &&
                    heatmapClickStats.numberOfRespondentsWithDataErrors > 0 &&
                    <div>{heatmapClickStats.numberOfRespondentsWithDataErrors} Respondents with data errors.</div>
                }
                {heatmapClickStats.averageClicksPerRespondent != undefined && <div>Average clicks is {heatmapClickStats.averageClicksPerRespondent.toFixed(decimalPlaces)}.</div>}
                {heatmapClickStats.averageClicksPerRespondentWhoHaveClicked != undefined && <div>Average clicks (for respondents who have clicked) is {heatmapClickStats.averageClicksPerRespondentWhoHaveClicked.toFixed(decimalPlaces)}.</div>}
                {heatmapClickStats.averageTimeBetweenClicks != undefined && <div>Average time between clicks is {heatmapClickStats.averageTimeBetweenClicks.toFixed(decimalPlaces)} seconds.</div>}
                {heatmapClickStats.averageTimeSpentClickingPerRespondent != undefined && <div>Average time between first and last click is {heatmapClickStats.averageTimeSpentClickingPerRespondent.toFixed(decimalPlaces)} seconds.</div>}
                {heatmapClickStats.numberOfClicksPerRespondent != undefined && <div>{heatmapClickStats.numberOfClicksPerRespondent[0]} respondents have NOT clicked.</div>}
            </div>)
    };


    const getKeyFrameStyles = () => {
        const frameStyles = new Array();
        frameStyles.push(styles.keyFrame);
        if (heatmapOptions?.keyPosition === BrandVueApi.HeatMapKeyPosition.TopLeft ||
            heatmapOptions?.keyPosition === BrandVueApi.HeatMapKeyPosition.TopRight) {
            frameStyles.push(styles.top);
        }
        if (heatmapOptions?.keyPosition === BrandVueApi.HeatMapKeyPosition.BottomRight ||
            heatmapOptions?.keyPosition === BrandVueApi.HeatMapKeyPosition.TopRight) {
            frameStyles.push(styles.right);
        }
        return frameStyles.join(" ");
    }

    return (
        <TileTemplate descriptionNode={getDescriptionNode()}>
            <div className={styles.container}>
                <Tooltip placement="top" title={getTooltip()}>
                    <div className={styles.pictureViewerFrame}>
                        {baseImageUrl && <img src={baseImageUrl} style={{ opacity: getBaseImageOpacity() }} />}
                        {heatmapDataUrl && <img src={heatmapDataUrl} className={baseImageUrl ? styles.overlay : styles.missingBaseUrl} style={{ opacity: overlayOpacity }} />}
                        {isLoading &&
                            <div className="throbber-container"><Throbber /></div>
                        }
                    </div>
                </Tooltip>
                {heatmapOptions?.displayKey &&
                    <div className={getKeyFrameStyles()}>
                        <img src={keyImageDataUrl} />
                        <div>
                            <span>{keyImageDataUrl && "Least clicked"} </span>
                            <span>{keyImageDataUrl && "Most clicked"}</span>
                        </div>
                    </div>
                }
                {heatmapOptions?.displayClickCounts && heatmapStatistics()}
                {props.displayFooter &&
                    heatmapFooter()
                }
            </div>
        </TileTemplate>
    );
};

export default HeatmapImageCard;