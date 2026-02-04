import React from "react";
import { VariableSampleResult } from "../../../../../BrandVueApi";
import style from './EstimatedResultBar.module.less';
import { useState } from "react";
import PopoverTooltip, { PopoverType } from "../../../../../components/visualisations/Settings/Weighting/PopoverTooltip";
import EstimatedResultBarSingleEntity from "./EstimatedResultBarSingleEntity";

interface IEstimatedResultBarMultiEntityProps {
    sample: VariableSampleResult[];
    helpText?: string;
}

const EstimatedResultBarMultiEntity = (props: IEstimatedResultBarMultiEntityProps) => {
    const [popoverOpen, setPopoverOpen] = useState<boolean>(false);

    const getSampleBar = (result: VariableSampleResult) => {
        return (
            <div className={style.result}>
                <EstimatedResultBarSingleEntity sample={result} includeLabel={false}/>
           </div>
        )
    }

    const getResultSummary = () => {
        const maxBarsToShow = 10;
        let sampleToMap = props.sample;
        if(props.sample.length > maxBarsToShow) {
            sampleToMap = props.sample.slice(0, maxBarsToShow);
        }

        const barsNotShown = props.sample.length - maxBarsToShow;

        return (
            <div className={style.popoverContent}>
                <div className={style.popoverHeader}>
                    <div className={style.header}>Unweighted count and sample</div>
                    <div className={style.header}>Result</div>
                </div>
                <div className={style.popoverBody}>
                    {sampleToMap.map(s => getSampleBar(s))}
                    <div className={style.butWaitTheresMore}>
                        {barsNotShown > 0 && `and ${barsNotShown} more options`}
                    </div>
                </div>
            </div>
        )
    }

    const popoverId = "estimateDetails"
    return (
        <div className={style.resultBar}>
            <div className={style.estimateLabel}>
                Unweighted count and sample:
                <div className={style.estimateDetails}
                    id={popoverId}
                    onMouseEnter={() => setPopoverOpen(true)}
                    onMouseLeave={() => setPopoverOpen(false)}
                >
                    <PopoverTooltip
                        type={PopoverType.OptionsSummary}
                        popoverContent={getResultSummary()}
                        id={popoverId}
                        isOpen={popoverOpen}
                        includeHeader={false}
                        limitWidth={false}
                        placement="top"
                    />
                    {props.sample.length} options
                </div>
            </div>
            {props.helpText && props.sample != undefined &&
                <div className={style.helptext}>
                    <i className="material-symbols-outlined">warning</i> {props.helpText}
                </div>
            }
        </div>
    );
}

export default EstimatedResultBarMultiEntity;