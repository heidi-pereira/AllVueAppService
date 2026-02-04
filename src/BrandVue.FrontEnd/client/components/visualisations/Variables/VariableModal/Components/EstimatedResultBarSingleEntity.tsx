import React from "react";
import { VariableSampleResult } from "../../../../../BrandVueApi";
import BrandVueOnlyLowSampleHelper from "../../../BrandVueOnlyLowSampleHelper";
import style from './EstimatedResultBar.module.less';

interface IEstimatedResultBarSingleEntityProps {
    sample: VariableSampleResult | undefined;
    includeLabel: boolean;
    helptext?: string;
}

interface IEstimatedResultBarDisplayProps {
    count: number | undefined;
    sample: number | undefined;
    percentage: number | undefined;
    countHasWarning: boolean;
    sampleHasWarning: boolean;
    includeLabel: boolean;
    entityName?: string;
    helpText?: string;
}

const EstimatedResultBarSingleEntity = (props: IEstimatedResultBarSingleEntityProps) => {
    const countIsZero = props.sample != undefined && props.sample.count == 0;
    const sampleIsZero = props.sample != undefined && props.sample.sample == 0;
    const countIsLow = props.sample != undefined && props.sample.count <= BrandVueOnlyLowSampleHelper.lowSampleForEntity;
    const countGreaterEqualSample = props.sample != undefined && props.sample.count >= props.sample.sample;

    const getPercentage = (): number | undefined => {
        if (props.sample) {
            if (props.sample.sample != 0) {
                return Math.round(props.sample.count / props.sample.sample * 100);
            }
            return 0;
        }
    }

    return (
        <EstimatedResultBarDisplay
            count={props.sample?.count}
            sample={props.sample?.sample}
            percentage={getPercentage()}
            countHasWarning={countIsZero || countIsLow || countGreaterEqualSample}
            sampleHasWarning={sampleIsZero || countGreaterEqualSample}
            helpText={props.helptext}
            includeLabel={props.includeLabel}
            entityName={props.sample?.splitByEntityInstanceName}
        />
    );
}

const EstimatedResultBarDisplay = (props: IEstimatedResultBarDisplayProps) => {
    const getPercentageElement = () => {
        const percent = props.percentage != undefined ? `${props.percentage}%` : '-';
        return (
            <div className={style.percentage}>
                {props.includeLabel && "Result: "}{percent}
            </div>
        );
    }

    const getCountAndSampleElement = () => {
        if (props.count != undefined && props.sample != undefined) {
            const countStyle = props.countHasWarning ? style.warning : '';
            const sampleStyle = props.sampleHasWarning ? style.warning : '';
            return (
                <>
                    <div className={style.entityName}>
                        {props.entityName}
                    </div>
                    <div className={countStyle}>{props.count}</div>
                    /
                    <div className={sampleStyle}>{props.sample}</div>
                </>
            );
        }
        return <div>-</div>
    }

    const getBarClass = () => {
        let className = style.filledEstimateBar;
        if (props.countHasWarning || props.sampleHasWarning) {
            className += ` ${style.warning}`;
        }
        return className;
    }

    const percentage = props.percentage ?? 0;
    return(
        <div className={style.resultBar}>
            <div className={style.samples}>
                <div className={style.countAndSample}>
                    {props.includeLabel && <div className={style.estimateLabel}>
                        Unweighted count and sample:
                    </div>}
                    {getCountAndSampleElement()}
                </div>
                {getPercentageElement()}
            </div>
            <div className={style.estimateBar}>
                <div className={getBarClass()} style={{width: `${percentage}%`}}></div>
            </div>
            {props.helpText && props.count != undefined && props.sample != undefined &&
                <div className={style.helptext}>
                    <i className="material-symbols-outlined">warning</i> {props.helpText}
                </div>
            }
        </div>
    )
}

export default EstimatedResultBarSingleEntity;