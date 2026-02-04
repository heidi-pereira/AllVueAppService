import React from "react";
import style from './ReportVuePageSidePanel.module.less';
import { ReportVuePageView } from "./ReportVuePage";

interface IReportVuePageSidePanelProps {
    icon: string;
    currentView: ReportVuePageView;
    setCurrentView(view: ReportVuePageView): void;
}

const ReportVuePageSidePanel = (props: IReportVuePageSidePanelProps) => {
    const [isExpanded, setExpanded] = React.useState<boolean>(false);

    const getPanelItem = (view: ReportVuePageView) => {
        let className = style.panelItem;
        if (view === props.currentView) {
            className += ` ${style.active}`;
        }
        return (
            <div className={className} onClick={() => props.setCurrentView(view)}>
                <i className="material-symbols-outlined no-symbol-fill">{getViewIcon(view)}</i>
                <span>{getViewName(view)}</span>
            </div>
        );
    }

    const getViewName = (view: ReportVuePageView) => {
        switch (view) {
            case ReportVuePageView.Administation: return 'Admin';
            case ReportVuePageView.Standard: return 'Standard';
        }
    }

    const getViewIcon = (view: ReportVuePageView) => {
        switch (view) {
            case ReportVuePageView.Administation: return 'settings';
            case ReportVuePageView.Standard: return props.icon;
        }
    }

    return (
        <aside
            className={`${style.reportVueSidePanel} ${isExpanded ? style.expanded : ''}`}
            onMouseEnter={() => setExpanded(true)}
            onMouseLeave={() => setExpanded(false)}
        >
            {getPanelItem(ReportVuePageView.Standard)}
            {getPanelItem(ReportVuePageView.Administation)}
        </aside>
    );
}

export default ReportVuePageSidePanel;
