import style from './TextCard.module.less';
import { CuratedFilters } from '../../../filter/CuratedFilters';
import { Metric } from '../../../metrics/metric';
import { PageCardState } from '../shared/SharedEnums';
import React from 'react';
import * as BrandVueApi from "../../../BrandVueApi";
import { ViewHelper } from '../ViewHelper';
import { NoDataError } from '../../../NoDataError';
import { PageCardPlaceholder } from './PageCardPlaceholder';
import { FilterInstance } from '../../../entity/FilterInstance';
import { IGoogleTagManager } from '../../../googleTagManager';
import TileTemplate from "./TileTemplate";
import { BaseExpressionDefinition } from "../../../BrandVueApi";
import { PageHandler } from '../../PageHandler';
import TextCardSearchInput from "./TextCardSearchInput";
import AilaSummariseRatingButtonGroup from '../../buttons/AilaSummariseRatingButtonGroup';
import { IEntityConfiguration } from '../../../entity/EntityConfiguration';
import Clipboard from 'react-clipboard.js';
import toast from 'react-hot-toast';
import { marked } from 'marked';
import { useAppSelector } from '../../../state/store';
import { selectSubsetId } from '../../../state/subsetSlice';
import { ITimeSelectionOptions } from "../../../state/ITimeSelectionOptions";
import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface ITextCardProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    metric: Metric;
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    filterInstances: FilterInstance[];
    curatedFilters: CuratedFilters;
    baseExpressionOverride: BaseExpressionDefinition | undefined;
    setDataState(state: PageCardState): void;
    setIsLowSample?: (lowSample: boolean) => void;
    setCanDownload?: (canDownload: boolean) => void;
    fullWidth?: boolean;
    entityConfiguration?: IEntityConfiguration;
    lowSampleThreshold: number;
}

async function getData(
    metric: Metric,
    curatedFilters: CuratedFilters,
    filterInstances: FilterInstance[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition): Promise<BrandVueApi.RawTextResults> {
    if (metric.entityCombination.length > 1) {
        //TODO: 2+ entity text results could potentially be implemented
        throw new Error("Cannot show results for text metrics with more than one entity");
    } else if (metric.entityCombination.length == 1) {
        if (filterInstances?.length == 0) {
            throw new Error("No filter instance provided for single entity text metric");
        }
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

    return await BrandVueApi.Factory.DataClient(throwError => throwError()).getRawTextResults(requestModel);
}

const TextCard = (props: ITextCardProps) => {
    const [results, setResults] = React.useState<string[]>([]);
    const [resultSummary, setResultSummary] = React.useState<string | null>(null);
    const [searchText, setSearchText] = React.useState<string>("");
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [isLowSample, setIsLowSample] = React.useState<boolean>(false);
    const [selectedTab, setSelectedTab] = React.useState<string>("Responses");
    const [summaryFeedbackDisabled, setSummaryFeedbackDisabled] = React.useState<boolean>(false);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const hasTextSummary = () => resultSummary !== null;

    const MarkdownRenderer = ({ input }) => {
        const html = marked(input);
        if (typeof html === 'string') {
            return <div dangerouslySetInnerHTML={{ __html: html }} />;
        }
        // Will never happen because our invocation of marked always returns a string
        return <div />;
    };

    const numTextToShow = 14;
    const resultsLength = results.length;

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);
        if (props.setCanDownload) {
            props.setCanDownload(false);
        }

        getData(props.metric, props.curatedFilters, props.filterInstances, subsetId, timeSelection, props.baseExpressionOverride)
            .then(d => {
                if (!isCancelled) {
                    const isLowSample = d.sampleSizeMetadata.sampleSize.unweighted < props.lowSampleThreshold;
                    setIsLowSample(isLowSample);
                    setResults(d.text);
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

        // Reset the summary when the data changes
        setResultSummary(null);

        return () => { isCancelled = true };
    }, [props.curatedFilters, JSON.stringify(props.filterInstances), props.metric, JSON.stringify(props.baseExpressionOverride), timeSelection]);

    const handleSearchInput = (text: string) => {
        setSearchText(text.trim().toLowerCase());
    }

    const getTrimmedText = () => {
        if (resultsLength <= numTextToShow) {
            return results;
        }

        return results.slice(0, numTextToShow);
    }

    const trimmedText = getTrimmedText();
    const numTextShowCondition = resultsLength > numTextToShow && !props.fullWidth;
    const filteredResults = results.filter(r => r.toLowerCase().includes(searchText))
    const textData = props.fullWidth ? filteredResults : trimmedText;

    const onSummariseComplete = (summary: string) => {
        setResultSummary(summary);
        setSelectedTab("Summary");
    }

    const getTextCardDescription = () => (
        <>
            {hasTextSummary() &&
                <div className={style.tabsThin}>
                    <div className={style.justifySpaceBetween}>
                        <div className={style.tabsThinContainer}>
                            <a href="#" className={selectedTab === "Summary" ? style.tabLink + " " + style.active : style.tabLink} onClick={(e) => { setSelectedTab("Summary"); e.preventDefault(); }}>
                                <span>Summary</span>
                            </a>
                            <a href="#" className={selectedTab === "Responses" ? style.tabLink + " " + style.active : style.tabLink} onClick={(e) => { setSelectedTab("Responses"); e.preventDefault(); }}>
                                <span>Responses</span>
                            </a>
                        </div>

                        {selectedTab == "Summary" && <div className={style.buttonClipboard}>
                            <Clipboard
                                className="hollow-button"
                                component="button"
                                onSuccess={() => toast.success("Summary copied to clipboard")}
                                onError={() => toast.error("Unable to copy to clipboard")}
                                data-clipboard-text={resultSummary}>
                                <i className="material-symbols-outlined">content_copy</i>
                                <span>Copy to clipboard</span>
                            </Clipboard>
                        </div>}
                    </div>
                </div> || <br />
            }
            {
                selectedTab == "Summary" && hasTextSummary() &&
                <div className={style.warningBubble}>
                    <div className={style.ailaInfo}>
                        <i className="material-symbols-outlined">info</i>
                    </div>
                    <div>
                        <p className={style.ailaInfo}>This summary is generated by AI.</p>
                        <p>Keep in mind that while it provides a quick overview, it may not capture all the nuances. If you close your browser you'll lose this summary. Subsequent generations may vary.</p>
                    </div>
                </div>
            }
            {
                props.fullWidth && selectedTab == "Responses" &&
                <TextCardSearchInput
                    handleSearchInput={handleSearchInput}
                    googleTagManager={props.googleTagManager}
                    results={results}
                    filteredResults={filteredResults}
                    onSummariseComplete={onSummariseComplete}
                    noNewDataForSummarise={resultSummary !== null}
                    pageHandler={props.pageHandler}
                    isLoading={isLoading}
                />

            }
        </>
    );
    if (isLoading) {
        return (
            <TileTemplate
                descriptionNode={!props.fullWidth ? props.getDescriptionNode(false) : getTextCardDescription()}
            >
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    const renderContent = () => (
        <>
            {selectedTab == "Summary" && hasTextSummary() &&
                <div className={style.summaryContainer}>
                    <div className={style.summaryContainerHeader}>
                        <div className="summary-body"><MarkdownRenderer input={resultSummary} /></div>
                    </div>
                    <AilaSummariseRatingButtonGroup disabled={summaryFeedbackDisabled} onFeedback={() => setSummaryFeedbackDisabled(true)} />
                </div>}
            {selectedTab == "Responses" &&
                <div className="page-text-container">
                    {textData.map((text, index) => {
                        return <div key={index} className="page-text">
                            <i className="material-symbols-outlined">format_quote</i>
                            <span className="text-result">{text}</span>
                        </div>
                    })}
                    {numTextShowCondition &&
                        <div className="more-text-link">+ {resultsLength - numTextToShow} more</div>}
                </div>}
        </>
    );

    return (
        <TileTemplate descriptionNode={!props.fullWidth ? props.getDescriptionNode(isLowSample) : getTextCardDescription()}>
            {renderContent()}
        </TileTemplate>
    );
};

export default TextCard;