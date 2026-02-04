import React from "react";


const ReloadingDataView = () => {
    return (
        <div className="reloadingData">
            <p>{`We're updating ${(window as any).productDisplayName}. While this is happening, new charts are unavailable.`}</p>
                <p>This page will refresh automatically as soon as we&#39;ve completed this quick update.</p>
            <ul className="activity_chart">
                <li className="" data-amount="8">
                    <div className="activity_chart_bar" style={{ height: "90%" }}></div>
                </li>
                <li className="" data-amount="9">
                    <div className="activity_chart_bar" style={{ height: "80%" }}></div>
                </li>
                <li className="" data-amount="3">
                    <div className="activity_chart_bar" style={{ height: "70%" }}></div>
                </li>
                <li className="" data-amount="3">
                    <div className="activity_chart_bar" style={{ height: "60%" }}></div>
                </li>
                <li className="" data-amount="6">
                    <div className="activity_chart_bar" style={{ height: "50%" }}></div>
                </li>
                <li className="" data-amount="0">
                    <div className="activity_chart_bar" style={{ height: "40%" }}></div>
                </li>
                <li className="" data-amount="6">
                    <div className="activity_chart_bar" style={{ height: "30%" }}></div>
                </li>
                <li className="" data-amount="2">
                    <div className="activity_chart_bar" style={{ height: "20%" }}></div>
                </li>
            </ul>
        </div>
    );
};

export default ReloadingDataView;