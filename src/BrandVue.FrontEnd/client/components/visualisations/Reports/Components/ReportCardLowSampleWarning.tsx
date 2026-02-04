import React from "react";
import { Popover } from 'reactstrap';
import { getLowSampleThreshold } from "../../BrandVueOnlyLowSampleHelper";

interface ReportCardLowSampleWarningProps {
    id: string;
    isLowSample?: boolean;
    shrink: boolean;
    isLineChart?: boolean;
}

const ReportCardLowSampleWarning = (props: ReportCardLowSampleWarningProps) => {

    const [popoverOpen, setPopoverOpen] = React.useState(false);
    const id = `low-sample-warning-${props.id}`;
    const popoverId = `${id}-popover`;

    const toggleLowSamplePopup = (e: React.MouseEvent) => {
        e.stopPropagation();
        setPopoverOpen(!popoverOpen);
    }

    React.useEffect(() => {
        if (popoverOpen) {
            document.getElementById(popoverId)?.focus({preventScroll:true});
        }
    }, [popoverOpen]);

    const lowSampleThrehold = getLowSampleThreshold();
    const lowSampleDisplayType = props.isLineChart ? "dotted lines" : "hollow bars or columns";
    const lowSampleImage = props.isLineChart ? "dotted-line-image" : "hollow-bar-image";
    if (props.isLowSample) {
        return (
            <>
                <button className="low-sample-warning-btn" onClick={toggleLowSamplePopup} id={id}>
                    <i className="material-symbols-outlined">warning</i>
                    {!props.shrink && <div>Low sample</div>}
                </button>
                <Popover
                    popperClassName="low-sample-popover"
                    placement="bottom-end"
                    isOpen={popoverOpen}
                    hideArrow={true}
                    target={id}
                    toggle={toggleLowSamplePopup}>
                    <div id={popoverId} className="popover-content" onClick={(e) => e.stopPropagation()} onBlur={() => setPopoverOpen(false)} tabIndex={-1}>
                    	<p className="text">Some answers on this chart have a sample size of <strong>less than {lowSampleThrehold}</strong> and are shown as {lowSampleDisplayType}:</p>
                        <div className={lowSampleImage}></div>
                        <p className="text">Low sample sizes may be a result of breaks or filters you've applied.</p>
                    </div>
                </Popover>
            </>
        );
    }
    return null;
}

export default ReportCardLowSampleWarning;