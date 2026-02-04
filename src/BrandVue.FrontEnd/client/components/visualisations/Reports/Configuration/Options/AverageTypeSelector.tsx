import React from 'react';
import { AverageType, MainQuestionType } from '../../../../../BrandVueApi';
import style from './AverageTypeSelector.module.less';
import { Metric } from '../../../../../metrics/metric';
import { useMetricStateContext } from '../../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from '../../../../../state/variableConfigurationSelectors';
import { useAppSelector } from "../../../../../state/store";
import { GetUnderlyingMetric } from '../../../Variables/VariableModal/Utils/VariableComponentHelpers';
import Tooltip from "../../../../Tooltip";

interface IAverageTypeSelectorProps {
    selectedAverages: AverageType[];
    toggleAverage(average: AverageType): void;
    disabledMessage?: string;
    metric: Metric | undefined;
    displayMeanValues: boolean;
    displayStandardDeviation: boolean;
    toggleDisplayMeanValues(e: React.ChangeEvent<HTMLInputElement>): void;
    toggleStandardDeviation(e: React.ChangeEvent<HTMLInputElement>): void;
    metrics: Metric[];
    supportsStandardDeviation: boolean;
}

const AverageTypeSelector = (props: IAverageTypeSelectorProps) => {
    const { questionTypeLookup } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);

    const isDisabled = props.disabledMessage !== undefined;
    const checkbox = "checkbox";

    const getAverageId = (average: AverageType) => {
        switch (average) {
            case AverageType.Median: return "median";
            case AverageType.Mean: return "mean";
            case AverageType.Mentions: return "mentions";
        }
        return "Error";
    }

    const getAverageLabel = (average: AverageType) => {
        return average.toLocaleString();
    }

    const getAverageHint = (average: AverageType) => {
        const metricToCheck = GetUnderlyingMetric(props.metric, props.metrics, variables) ?? props.metric!;
        const questionType = questionTypeLookup[metricToCheck.name];
        
        switch (average) {
            case AverageType.Median: return "The score that's sequentially in the middle";
            case AverageType.Mean: return questionType == MainQuestionType.SingleChoice
                ? "The sum of all scores divided by the number of responses"
                : "The sum of all values divided by the number of choices";
            case AverageType.Mentions: return "The average number of answers selected by a respondent";
        }
        return "Error";
    }
    
    const getAveragesForQuestionType = () => {
        const metricToCheck = GetUnderlyingMetric(props.metric!, props.metrics, variables) ?? props.metric!;
        const questionType = questionTypeLookup[metricToCheck.name];
        if(questionType == MainQuestionType.Text || questionType == MainQuestionType.HeatmapImage) {
            return;
        }

        if(questionType == MainQuestionType.MultipleChoice) {
            return [AverageType.Median, AverageType.Mean, AverageType.Mentions];
        }

        return [AverageType.Median, AverageType.Mean];
    }

    const averagesToMap = getAveragesForQuestionType();
    if(!averagesToMap) {
        return null;
    }

    const calculateMeanIsTrue = props.selectedAverages.includes(AverageType.Mean);

    return (
        <>
            <label className={style.categoryLabel}>{ "Averages" }</label>
            {averagesToMap.map(average => {
                const id = getAverageId(average);
                const isEnabledAndChecked = !isDisabled && props.selectedAverages.includes(average);
                return (
                    <div className={style.option} key={id} title={props.disabledMessage}>
                        <div className="box">
                            <input type={checkbox}
                                className={checkbox}
                                id={id}
                                checked={isEnabledAndChecked}
                                onChange={() => props.toggleAverage(average)}
                                disabled={isDisabled} />
                            <label className={style.optionLabel} htmlFor={id}>
                                {getAverageLabel(average)}
                            </label>
                        </div>
                        <div className={style.averageHint}>
                            {getAverageHint(average)}
                        </div>
                    </div>
                );
            })}
            {
                props.supportsStandardDeviation && (() => {
                    const standardDeviationCheckbox = (
                        <div className={style.option}>
                            <div className="box">
                                <input type={checkbox}
                                    className={checkbox}
                                    id="sdValueLabelToggle"
                                    checked={props.displayStandardDeviation && calculateMeanIsTrue}
                                    onChange={props.toggleStandardDeviation}
                                    disabled={!calculateMeanIsTrue} />
                                <label className={style.optionLabel} htmlFor="sdValueLabelToggle">
                                    Show standard deviation
                                </label>
                            </div>
                        </div>
                    );

                    return calculateMeanIsTrue ? standardDeviationCheckbox : (
                        <Tooltip placement="top" title={"Standard deviation can only be shown when 'Mean' is selected as an average type"}>
                            {standardDeviationCheckbox}
                        </Tooltip>
                    );
                })()
            }
            <div className={style.option}>
                <div className="box">
                    <input type={checkbox}
                        className={checkbox}
                        id="meanValueLabelToggle"
                        checked={props.displayMeanValues}
                        onChange={props.toggleDisplayMeanValues} />
                    <label className={style.optionLabel} htmlFor="meanValueLabelToggle">
                        Show scale values
                    </label>
                </div>
            </div>
        </>
    )
}

export default AverageTypeSelector;