import React from "react";
import { Popover } from 'reactstrap';

interface ICrosstabLowSampleWarningProps {
    isLowSample: boolean;
    lowSampleThreshold: number;
}

const CrosstabLowSampleWarning = (props: ICrosstabLowSampleWarningProps) => {
    const [popoverOpen, setPopoverOpen] = React.useState(false);
    const toggleLowSamplePopup = () => setPopoverOpen(!popoverOpen);
    const popoverId = 'crosstab-low-sample-warning-popover';

    React.useEffect(() => {
        if (popoverOpen) {
            document.getElementById(popoverId)?.focus({preventScroll:true});
        }
    }, [popoverOpen]);

    if (props.isLowSample) {
        return (
            <>
                <button className="low-sample-warning-btn" onClick={toggleLowSamplePopup} id="low-sample-warning">
                    <i className="material-symbols-outlined">warning</i> <div>Low sample</div>
                </button>
                <Popover
                    popperClassName="low-sample-popover"
                    placement="bottom-end"
                    isOpen={popoverOpen}
                    hideArrow={true}
                    target="low-sample-warning"
                    toggle={toggleLowSamplePopup}>
                    <div className="popover-content" id={popoverId} onBlur={() => setPopoverOpen(false)} tabIndex={-1}>
                        <p className="text">This table has a total sample size of <strong>{props.lowSampleThreshold} or less</strong>.</p>
                        <p className="text">Answers with low sample sizes are also highlighted in <strong className="warning-text">red</strong>.</p>
                        <p className="text">Low sample sizes may be a result of breaks or filters you've applied.</p>
                    </div>
                </Popover>
            </>
        );
    }
    return null;
}

export default CrosstabLowSampleWarning;