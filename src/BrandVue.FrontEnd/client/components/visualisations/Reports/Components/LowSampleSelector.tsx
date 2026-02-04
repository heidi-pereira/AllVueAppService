import BrandVueOnlyLowSampleHelper from "../../BrandVueOnlyLowSampleHelper";
import style from "./LowSampleSelector.module.less";

export interface ILowSampleSelector {
    highlightLowSample: boolean;
    handleHighlightLowSampleChanged(e: React.ChangeEvent<HTMLInputElement>): void;
    lowSampleThreshold: number;
    handleLowSampleThresholdChange(e: React.ChangeEvent<HTMLInputElement>): void;
    allowLowSampleThresholdEditing: boolean;
}

const LowSampleSelector = (props: ILowSampleSelector) => {
    const containerName = props.highlightLowSample ? style.lowSampleThresholdContainer : style.lowSampleThresholdContainerDisabled;
    return (
        <div className={style.container}>
            <div className={style.reportLowSample}>
                <input type="checkbox"
                    className="checkbox"
                    id="low-sample-checkbox"
                    checked={props.highlightLowSample}
                    onChange={props.handleHighlightLowSampleChanged} />
                <label htmlFor="low-sample-checkbox">
                    Highlight low sample
                </label>
                {props.allowLowSampleThresholdEditing ?
                    <div className={containerName}>
                        <label htmlFor="low-sample-threshold" className={style.srOnly}>Low sample threshold</label>
                        <input type="number"
                            id="low-sample-threshold"
                            className={style.lowSampleThreshold}
                            min={0}
                            value={props.lowSampleThreshold}
                            onChange={props.handleLowSampleThresholdChange}
                            disabled={!props.highlightLowSample} />
                    </div>
                    :
                    <div className={style.optionHint}>A low sample warning is shown if any sample sizes are {BrandVueOnlyLowSampleHelper.lowSampleForEntity} or lower</div>
                }
            </div>
        </div>
    );
};

export default LowSampleSelector;