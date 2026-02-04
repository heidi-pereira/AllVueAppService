import React from 'react';
import { IAverageDescriptor, AverageType, BaseExpressionDefinition, CrosstabAverageResults, MainQuestionType, OverTimeAverageResults, SampleSizeMetadata, WeightedDailyResult } from '../../BrandVueApi';
import { Metric } from '../../metrics/metric';
import { baseExpressionDefinitionDisplayName, formatOverTimeDate, getMetricDisplayName, stripHtmlTagsFromHelpText } from '../helpers/SurveyVueUtils';
import { groupMetricFiltersByMeasureName } from './Reports/Filtering/FilterHelper';
import { mainQuestionTypeToString } from './Reports/Utility/MainQuestionTypeHelpers';
import { useFilterStateContext } from '../../filter/FilterStateContext';
import { MetricFilterState } from '../../filter/metricFilterState';
import { getAllVueSampleSizeDescription } from '../helpers/SampleSizeHelper';
import classNames from 'classnames';
import { BaseVariableContext } from './Variables/BaseVariableContext';
import { getAverageDisplayText } from './AverageHelper';
import { useContext } from 'react';
import { useMetricStateContext } from '../../metrics/MetricStateContext';
import {useEntityConfigurationStateContext} from "../../entity/EntityConfigurationStateContext";

export interface IAllVueDescriptionFooterProps {
    sampleSizeMeta?: SampleSizeMetadata;
    metric: Metric;
    filterInstanceNames?: string[];
    baseExpressionOverride?: BaseExpressionDefinition;
    isSurveyVue: boolean;
    decimalPlaces: number;
    footerAverages: CrosstabAverageResults[] | OverTimeAverageResults[][] | OverTimeAverageResults[] | undefined;
    extraRows?: string[];
    averageDescriptor?: IAverageDescriptor;
    filterByIndex?: number;
}

const AllVueDescriptionFooter = (props: IAllVueDescriptionFooterProps) => {
    const [isExpandable, setIsExpandable] = React.useState(false);
    const [isExpanded, setIsExpanded] = React.useState(false);
    const { baseVariables } = useContext(BaseVariableContext);
    const { questionTypeLookup } = useMetricStateContext();
    const { entityConfiguration} = useEntityConfigurationStateContext();

    const divRef = React.useRef<HTMLDivElement>(null);

    const defaultLayout = () => {
        setIsExpandable(false);
        setIsExpanded(false);
    }

    const expandedLayout = () => {
        setIsExpandable(true);
        setIsExpanded(true);
    }

    const collapsedLayout = () => {
        setIsExpandable(true);
        setIsExpanded(false);
    }

    const toggleIsExpanded = () => {
        if (isExpanded){
            collapsedLayout();
        }
        else{
        expandedLayout();
        }
    };

    const setLayout = (ref: React.RefObject<HTMLDivElement>) => {
        setIsExpandable(false);
        if (ref.current) {
            const {height} = ref.current.getBoundingClientRect();
            if (height < 77) {
                defaultLayout();
            } else if (isExpanded)
            {
                expandedLayout();
            }
            else {
                collapsedLayout();
            }
    }
}

    const useRefDimensions = (ref: React.RefObject<HTMLDivElement>) => {
        const handleResize = () => {
            setLayout(ref);
        }

        React.useEffect(() => {
            window.addEventListener('resize', handleResize);
            handleResize();
            return () => {window.removeEventListener('resize', handleResize)};
        }, [ref, isExpanded, isExpandable])
    }

    useRefDimensions(divRef);

    const { filters } = useFilterStateContext();

    const getSampleDescription = () => {
        //this is checking if sampleSize rather than sampleSizeMeta exists because the meta doesn't get serialized null -> undefined
        if (props.sampleSizeMeta?.sampleSize) {
            return getFooterElement(getAllVueSampleSizeDescription(props.sampleSizeMeta));
        }
        return null;
    }

    const getBaseDescription = () => {
        let baseDescription = props.metric.baseDescription;
        if (props.baseExpressionOverride && !props.metric.hasCustomBase) {
            baseDescription = baseExpressionDefinitionDisplayName(props.baseExpressionOverride, baseVariables);
        }
        return getFooterElement(`Base = ${baseDescription}`);
    }

    const roundValue = (value: number) => {
        return Math.round(value * 100) / 100;
    }

    const formatAverageValue = (averageType: AverageType, value: number) => {
        const decimalRespectingValue = (value * 100).toFixed(props.decimalPlaces)
        if(averageType == AverageType.EntityIdMean || averageType == AverageType.Mentions || averageType == AverageType.Median) {
            return roundValue(value);
        }
        return `${decimalRespectingValue}%`
    }

    const getFooterAverages = () => {
        if(props.isSurveyVue && props.footerAverages) {
            if (props.footerAverages && props.footerAverages[0] instanceof CrosstabAverageResults) {
                const averages = props.footerAverages as CrosstabAverageResults[];
                return averages.map(average => {
                    const averageName = getAverageDisplayText(average.averageType)
                    if (average.dailyResultPerBreak.length > 1) {
                        const resultPerBreak = average.dailyResultPerBreak.map(r =>
                            `${r.breakName}: ${formatAverageValue(average.averageType, r.weightedDailyResult.weightedResult)}`)
                            .join("; ");
                        return getFooterElement(`${averageName} = ${resultPerBreak}`);
                    } else {
                        return getFooterElement(`${averageName} = ${formatAverageValue(average.averageType, average.overallDailyResult.weightedDailyResult.weightedResult)}`);
                    }
                });
            } else if (props.footerAverages && props.footerAverages[0] instanceof OverTimeAverageResults) {
                //actual overtime result
                const averages = props.footerAverages as OverTimeAverageResults[];

                let getName = (result: WeightedDailyResult) => result.text;
                if (props.averageDescriptor) {
                    getName = (result: WeightedDailyResult) => formatOverTimeDate(props.averageDescriptor!, result.date);
                }

                return averages.map(average => {
                    const averageName = getAverageDisplayText(average.averageType);
                    const values = average.weightedDailyResults.map(result =>
                        `${getName(result)}: ${formatAverageValue(average.averageType, result.weightedResult)}`).join("; ");
                    return getFooterElement(`${averageName} = ${values}`);
                })
            } else if (props.footerAverages) {
                const averages = props.footerAverages as OverTimeAverageResults[][];
                return averages.filter(a => a.length > 0).map(average => {
                    const averageName = getAverageDisplayText(average[0].averageType);
                    const mentionsPerBreak = average.map(a =>
                        `${a.weightedDailyResults[0].text}: ${formatAverageValue(a.averageType, a.weightedDailyResults[0].weightedResult)}`).join("; ");
                    return getFooterElement(`${averageName} = ${mentionsPerBreak}`);
                })
            }
        }
    }

    const getFiltersDescription = () => {
        if (filters.length == 0) {
            return null;
        }
        const groupedFilterDescriptions = groupMetricFiltersByMeasureName(filters)
            .map(group => getGroupedFilterDescription(group));
        return getFooterElement(`Filters = ${groupedFilterDescriptions.join(", ")}`);
    }

    const getGroupedFilterDescription = (filterGroup: MetricFilterState[]) => {
        if (filterGroup.length > 0) {
            const matchedMetric = filterGroup[0].metric;
            return `${matchedMetric.displayName}: ${filterGroup.map(f => f.filterDescription(entityConfiguration)).join(", ")}`;
        }
    }

    const getQuestionDescription = () => {
        let questionDescription = "";

        if (props.isSurveyVue && questionTypeLookup[props.metric.name] !== MainQuestionType.CustomVariable) {
            questionDescription = `Q = "${stripHtmlTagsFromHelpText(props.metric.helpText)}" (${mainQuestionTypeToString(questionTypeLookup[props.metric.name])})`;
        } else {
            questionDescription = `Q = ${stripHtmlTagsFromHelpText(props.metric.measure ?? props.metric.displayName)}`;
        }

        if (props.filterByIndex !== undefined && props.filterByIndex !== null) {
            questionDescription += ` - ${props.filterInstanceNames![props.filterByIndex]}`;
        } else if (props.metric?.entityCombination?.length > 1 && props.filterInstanceNames && props.filterInstanceNames.length > 0) {
            questionDescription += ` - ${props.filterInstanceNames.join(', ')}`;
        }
        return getFooterElement(questionDescription);
    }

    const getFooterElement = (text: string, key?:string) => {
        return (<div key={key} className="footer-element" title={text}>{text}</div>)
    }

    const getFooter = () => {
        return (
            <div className="allvue-description-footer" ref={divRef}>
            <div>
                {getSampleDescription()}
                {props.isSurveyVue && getBaseDescription()}
                {getFiltersDescription()}
                {getQuestionDescription()}
                    {getFooterAverages()}
                    {props.extraRows && props.extraRows.map((extraRow, index) => getFooterElement(extraRow, `extra_${index}`))}
            </div>
            </div>
            );
    }

    return (
        <div className={classNames(
            "footer-wrapper",
            {"footer-expanded": isExpandable && isExpanded,
            "footer-collapsed": isExpandable && !isExpanded}
        )}>
            {getFooter()}
            {isExpandable && <i className="material-symbols-outlined" onClick={toggleIsExpanded}>{isExpanded? "keyboard_arrow_up" : "keyboard_arrow_down"}</i>}
        </div>
    );
}

export default AllVueDescriptionFooter;