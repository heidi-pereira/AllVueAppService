import React from "react";
import {
    AnalysisDataState,
    AnalysisScorecardTitle,
    AnalysisSubPageComponent,
    buttonLink,
    getDataWrapper,
    getTitle,
    getTitleDescription,
    IButtonOptions,
    orderMetricScoreResults,
    ScoreCurrentVsPrevious,
    subPageButtonLink,
    subPageComponentBackground
} from "../helpers/AnalysisHelper";
import {
    getMetricNamesForPanes,
    getPageTreeForDisplayName,
    getUrlForPageDisplayName,
    IPageUrlOptions
} from "../helpers/PagesHelper";
import {IBrandAnalysisRequestProps} from "../helpers/BrandAnalysisRequestHelper";
import RankDelta from "../visualisations/RankDelta";
import {Metric} from "../../metrics/metric";
import {ViewHelper} from "../visualisations/ViewHelper";
import {
    IAverageDescriptor,
    Factory,
    MeasureFilterRequestModel,
    StackedMultiEntityRequestModel,
    StackedMultiEntityResults,
    ComparisonPeriodSelection
} from "../../BrandVueApi";
import {NoDataError} from "../../NoDataError";
import style from "./BrandAnalysis.module.less";
import {ProductConfigurationContext} from "../../ProductConfigurationContext";
import {MetricFilterState} from "../../filter/metricFilterState";
import {EntitySet} from "../../entity/EntitySet";
import {useEntityConfigurationStateContext} from "../../entity/EntityConfigurationStateContext";
import {useMetricStateContext} from "../../metrics/MetricStateContext";
import { useLocation } from "react-router-dom";
import { useReadVueQueryParams } from "../helpers/UrlHelper";
import { selectSubsetId } from "client/state/subsetSlice";
import { useAppSelector } from "client/state/store";
import { selectTimeSelection } from "client/state/timeSelectionStateSelectors";

interface IBrandAnalysisWhereNextProps extends IBrandAnalysisRequestProps {
    title: AnalysisScorecardTitle,
}

type MetricInsight = {
    icon: JSX.Element,
    text: JSX.Element
}
enum ChangeType {
    Increase = "increase",
    Decrease = "decrease"
}

const relatedTitle = (title: AnalysisScorecardTitle): AnalysisScorecardTitle => {
    switch (title) {
        case AnalysisScorecardTitle.Advocacy:
            return AnalysisScorecardTitle.Buzz;
        case AnalysisScorecardTitle.Buzz:
            return AnalysisScorecardTitle.Usage;
        case AnalysisScorecardTitle.Usage:
            return AnalysisScorecardTitle.Love;
        case AnalysisScorecardTitle.Love:
            return AnalysisScorecardTitle.Advocacy;
    }
};

const textContent = (title: AnalysisScorecardTitle, appBasePath: string, productName: string, urlOptions: IPageUrlOptions) => {
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();
    return <div>
        <div>
            <h4>Where next?</h4>
            <ul>
                <li>
                    <strong>{getTitleDescription(title, productName)}</strong> is closely related to:
                    <ul>
                        <li>
                            <div className={style.link ?? ""}>
                            <a href={buttonLink(relatedTitle(title), appBasePath, location, readVueQueryParams)}>{getTitle(relatedTitle(title), productName)}</a>
                            </div>
                        </li>
                    </ul>

                </li>
                <li>
                    Change the <strong>focus brand</strong> and <strong>competitor set</strong> at the top of this page to see how your competitors are doing.
                </li>
            </ul>
        </div>
    </div>
};

const getInsight = (insight: MetricInsight, index: number) : JSX.Element =>  {
    return (
        <div key={index} className={style.metricInsight}>
            <div className={style.metricInsightIcon}>
                {insight.icon}
            </div>
            <div className={style.metricInsightText}>
                {insight.text}
            </div>
        </div>
    );
}

const BrandAnalysisWhereNext: React.FunctionComponent<IBrandAnalysisWhereNextProps> = (props) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const metricContext = useMetricStateContext();
    const [insightState, setInsightState] = React.useState<ScoreCurrentVsPrevious[]>([]);
    const [insightDataState, setInsightDataState] = React.useState(AnalysisDataState.Loading);
    const [averages, setAverages] = React.useState<IAverageDescriptor[]>([]);
    const location = useLocation();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const metricMovement = (metricScores: ScoreCurrentVsPrevious, baseDescription: string, textToRemove: string[], changeType: ChangeType) => {
        const textContent = (metricName: string, diff: number, baseDescription: string) => {
            return <p>The attribute <strong>{metricName}</strong> has {diff == 0 ? "stayed the same" : diff > 0 ? "increased the most" : "fallen the furthest"} among {baseDescription}</p>
        }

        if ((changeType === ChangeType.Increase && metricScores.diff < 0) || (changeType === ChangeType.Decrease && metricScores.diff > 0)) {
            return {
                icon: <RankDelta delta={0} downIsGood={false} />,
                text: <p>No {changeType} in {baseDescription} over this time period</p>
            }
        }

        return {
            icon: <RankDelta delta={metricScores.previousScore - metricScores.currentScore} downIsGood={false} />,
            text:
                textContent(getSanitisedMetricName(textToRemove, metricScores.instanceName), metricScores.diff, baseDescription)
        }
    }

    const topInstances = (instanceNames: string[], userDescription: string) => {
        return {
            icon: <span><i className={`material-icons-round ${style.gold}`}>emoji_events</i></span>,
            text: <p>The top Image associations amongst {userDescription} are <strong>{instanceNames.join(", ")}</strong>.</p>
        }
    }

    const getSanitisedMetricName = (textToRemove: string[], metricName: string) => {
        textToRemove.forEach(t => metricName = metricName.replace(t, ""))
        return metricName;
    }

    const imageMetricPage = props.pageMetricConfiguration.buttonLink ?? 'All image associations';

    const processStackedMultiEntityResults = (res: StackedMultiEntityResults): void => {

        const simpleResults: ScoreCurrentVsPrevious[] = res.resultsPerInstance.map(m => {
            var entityResults = m.data[0];
            return {
                instanceName: m.filterInstance.name,
                currentScore: entityResults.weightedDailyResults[1].weightedResult,
                previousScore:entityResults.weightedDailyResults[0].weightedResult,
                diff: entityResults.weightedDailyResults[1].weightedResult - entityResults.weightedDailyResults[0].weightedResult
            } as ScoreCurrentVsPrevious
        })
        
        setInsightState(simpleResults);
        setInsightDataState(AnalysisDataState.Show);
    }

    React.useEffect(() => {
        Factory.MetaDataClient(throwErr => throwErr())
            .getAverages(subsetId)
            .then(setAverages);
    }, [subsetId]);

    React.useEffect(() => {
        setInsightDataState(AnalysisDataState.Loading);
        if (averages.length > 0) {

            const requestModel = insightsRequestModelFromProps();
            
            if (requestModel) {
                load(requestModel!);
            }
        }
    }, [
        props.activeEntitySet,
        JSON.stringify(props.curatedFilters),
        props.primaryMetric,
        averages.length
    ]);

    const load = async (requestModel: StackedMultiEntityRequestModel) => {
        await Factory.DataClient(err => err()).getStackedResultsForMultipleEntities(requestModel).then(processStackedMultiEntityResults)
            .catch((e: Error) => {
                if ((e as any).typeDiscriminator === NoDataError.typeDiscriminator) {
                    setInsightDataState(AnalysisDataState.NoData);
                } else {
                    setInsightDataState(AnalysisDataState.Error);
                    throw e;
                }
            });
    }
    
    const insightsRequestModelFromProps = (): StackedMultiEntityRequestModel | null => {
        
        const pageTree = getPageTreeForDisplayName(props.pageMetricConfiguration.buttonLink);
        const metricNameSet = new Set(getMetricNamesForPanes(pageTree[pageTree.length-1]?.panes));
        const metricNames = Array.from(metricNameSet);
        const insightMetrics = metricContext.enabledMetricSet.getMetricByName(metricNames);
        const filter = getLinkFilterFromConfig(props.title);
        const filterMetric = metricContext.enabledMetricSet.getMetric(filter.name);
        const filterContent = filter.value.split(".");
        const filterState = MetricFilterState.getArrayValueArgumentsFromString(filterContent[1], false);

        if(insightMetrics.length == 1) {
            const measureFilter = new MeasureFilterRequestModel({
                measureName: filterMetric?.name!,
                entityInstances: Object.fromEntries(
                    props.primaryMetric.entityCombination
                        .filter(ec => ec.isBrand)
                        .map(e=>[e.identifier, [-1]])),
                values: filterState.values,
                invert: false,
                treatPrimaryValuesAsRange: props.primaryMetric.legacyPrimaryTrueValues?.isRange ?? false
            });

            const requestModel = ViewHelper.createStackedMultiEntityRequestModelFromInstances(
                props.curatedFilters,
                insightMetrics[0],
                [props.brand.id],
                props.activeEntitySet.type.identifier,
                getDefaultFilterByEntitySet(insightMetrics[0]),
                false,
                subsetId,
                timeSelection
            );
            requestModel.filterModel.filters = [measureFilter, ...requestModel.filterModel.filters];
            requestModel.period.comparisonDates = props.curatedFilters.comparisonDates(false, timeSelection, false, ComparisonPeriodSelection.CurrentAndPreviousPeriod);
            return requestModel;
        }
        return null;
    }

    const getDefaultFilterByEntitySet = (imageMetric: Metric) : EntitySet => {
        const filterByEntityType = imageMetric.entityCombination.find(e => e.identifier !== props.activeEntitySet.type.identifier);
        return entityConfiguration.getDefaultEntitySetFor(filterByEntityType!);
    }


    const insights = (title: AnalysisScorecardTitle): JSX.Element[] | JSX.Element | null => {
        let insights: MetricInsight[] = [];
        if (insightState.length == 0) {
            return null;
        }
        if(insightState.some(i => i.previousScore == null)) {
            return <div className={`${style.subtext} ${style.noData}`}>No previous scores available for the selected time period</div>
        }
        const orderedSimpleResults = orderMetricScoreResults(insightState);
        const mostSignificantIncrease = orderedSimpleResults[0];
        const mostSignificantDecrease = orderedSimpleResults[orderedSimpleResults.length - 1];
        const top3Metrics = insightState
            .sort((a, b) => b.currentScore - a.currentScore)
            .filter((_, i) => i < 3)
            .map(r => r.instanceName);

        let baseDescription = "";
        let textToRemove = ["Image:"];

        switch (title) {
            case AnalysisScorecardTitle.Advocacy:
                baseDescription = props.pageMetricConfiguration.shortUserDescription ?
                    props.pageMetricConfiguration.shortUserDescription :
                    "brand Advocates";
                textToRemove.push(" Advocacy");
                insights.push(...[
                    metricMovement(mostSignificantIncrease, baseDescription, textToRemove, ChangeType.Increase),
                    metricMovement(mostSignificantDecrease, baseDescription, textToRemove, ChangeType.Decrease)
                ])
                break;
            case AnalysisScorecardTitle.Buzz:
                baseDescription = props.pageMetricConfiguration.shortUserDescription ?
                    props.pageMetricConfiguration.shortUserDescription :
                    "those who talk positively about your brand";
                insights.push(...[
                    metricMovement(mostSignificantIncrease, baseDescription, textToRemove, ChangeType.Increase),
                    metricMovement(mostSignificantDecrease, baseDescription, textToRemove, ChangeType.Decrease)
                ])
                break;
            case AnalysisScorecardTitle.Usage:
                baseDescription = props.pageMetricConfiguration.shortUserDescription ?
                    props.pageMetricConfiguration.shortUserDescription :
                    productConfiguration.productName === "charities" ? "current supporters" : "current users/buyers"
                insights.push(...[
                    metricMovement(mostSignificantIncrease, baseDescription, textToRemove, ChangeType.Increase),
                    metricMovement(mostSignificantDecrease, baseDescription, textToRemove, ChangeType.Decrease)
                ])
                break;
            case AnalysisScorecardTitle.Love:
                baseDescription = props.pageMetricConfiguration.shortUserDescription ?
                    props.pageMetricConfiguration.shortUserDescription :
                    "brand Lovers";
                insights.push(...[
                    metricMovement(mostSignificantIncrease, baseDescription, textToRemove, ChangeType.Increase),
                    metricMovement(mostSignificantDecrease, baseDescription, textToRemove, ChangeType.Decrease)
                ])
                break;
        };

        var userDescription;
        switch (title) {
            case AnalysisScorecardTitle.Advocacy:
                userDescription = props.pageMetricConfiguration?.longUserDescription ??
                    "brand Advocates";
                break;
            case AnalysisScorecardTitle.Buzz:
                userDescription = props.pageMetricConfiguration?.longUserDescription ??
                    "those who talk about your brand";
                break;
            case AnalysisScorecardTitle.Usage:
                userDescription = props.pageMetricConfiguration?.longUserDescription ??
                    (productConfiguration.productName === "charities" ? "those who are current supporters" : "those who are current users/buyers");
                break;
            case AnalysisScorecardTitle.Love:
                userDescription = props.pageMetricConfiguration?.longUserDescription ?? "those who love you";
                break;
        }

        insights.push(topInstances(top3Metrics, userDescription));

        return insights.map((m, i) => getInsight(m, i));
    };

    const insightsContent = (title: AnalysisScorecardTitle) => {
        return (
            <div>
                <div>
                    <h4>How are we doing?</h4>
                </div>
                <div role={"insights-content"}>
                    {getDataWrapper(insightDataState, () => insights(title), { twoThrobbers: true })}
                </div>
            </div>
        );
    }
//getMetricFiltersFromArgs
    const getLinkFilterFromConfig = (title: AnalysisScorecardTitle) => {
        if (props.pageMetricConfiguration.filterMetric) {
            return {
                name: props.pageMetricConfiguration.filterMetric.name, value: props.pageMetricConfiguration.filterMetric.value }
        }

        let filterName;
        const mf = new MetricFilterState();
        mf.values = props.primaryMetric.legacyPrimaryTrueValues?.values;
        mf.treatPrimaryValuesAsRange = props.primaryMetric.legacyPrimaryTrueValues?.isRange;
        const filterValues = mf.valueToString();

        switch (title) {
            case AnalysisScorecardTitle.Advocacy:
                filterName = 'NPS';
                break;
            case AnalysisScorecardTitle.Buzz:
                filterName = props.primaryMetric.name;
                break;
            case AnalysisScorecardTitle.Usage:
                filterName = 'Customer segment';
                break;
            case AnalysisScorecardTitle.Love:
                filterName = 'Brand Affinity';
                break;
            default:
                break;
        }

        return { name: filterName, value: `-1.${filterValues}` };
    }
    
    const buttonAction = (url: string): string => {

        const params = getLinkFilterFromConfig(props.title);
        if (!params) {
            return url;
        }
        return url.replace("?", `?${params.name}=${params.value}&`);
    }

    const link = () => {
        const readVueQueryParams = useReadVueQueryParams();
        const url = `ui${getUrlForPageDisplayName(imageMetricPage, location, readVueQueryParams)}`;
        const buttonOptions : IButtonOptions = {
            focusInstance: props.brand,
            entitySetInstances: props.activeEntitySet.getInstances().clone().getAll(),
            buttonAction: buttonAction
        };
        return subPageButtonLink(`Explore brand image`, url, buttonOptions);
    }

    return <div className={`tile ${style.brandAnalysisSubPage}`} style={subPageComponentBackground(AnalysisSubPageComponent.WhereNext, productConfiguration.cdnAssetsEndpoint)}>
        <div className={style.cardContent}>
            <div className={style.subPageCardTitle}>
                <div className={style.titleIcon}>
                    <i className="material-symbols-outlined">near_me</i>
                </div>
                <div className={style.titleText}>Where next?</div>
            </div>
            <div className={style.subPageFlexRow}>
                <div className={`${style.subPageTextContainer} ${style.equal}`}>
                    {textContent(props.title, productConfiguration.appBasePath, productConfiguration.productName, location.state)}
                </div>
                <div className={`${style.subPageTextContainer} ${style.equal}`}>
                    {insightsContent(props.title)}
                    {link()}
                </div>
            </div>
        </div>
    </div>;
}

export default BrandAnalysisWhereNext;