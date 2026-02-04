import React from 'react';
import { Popover } from 'reactstrap';
import { Placement } from 'popper.js';

interface IReportLargeExportPopover {
    isOpen: boolean;
    attachedElementId: string;
    placement?: Placement;
    close(): void;
    doExport(): void;
}

const LargeExportPopover = (props: IReportLargeExportPopover) => {
    const popoverId = `${props.attachedElementId}-popover`;

    React.useEffect(() => {
        if (props.isOpen) {
            document.getElementById(popoverId)?.focus({preventScroll:true});
        }
    }, [props.isOpen]);

    return (
        <Popover
            popperClassName="large-export-popover"
            placement={props.placement ?? 'bottom-end'}
            isOpen={props.isOpen}
            hideArrow={false}
            target={props.attachedElementId}
            trigger="legacy">
            <div id={popoverId} className="popover-content" onClick={(e) => {e.stopPropagation()}} onBlur={() => props.close()} tabIndex={-1}>
                <p className="title">Large file</p>
                <p className="text">This export contains a large amount of data so the file may take a long time to be created.</p>
                <p className="text">Please leave AllVue open as the file is generated.</p>
                <button className="primary-button" onClick={props.doExport}>Export</button>
            </div>
        </Popover>
    )
}

export default LargeExportPopover;