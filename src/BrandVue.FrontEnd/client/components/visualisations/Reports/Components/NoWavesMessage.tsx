import React from "react";

interface INoWavesMessageProps {
    isReportSettings?: boolean;
    isDisabled: boolean;
}

const NoWavesMessage = (props: INoWavesMessageProps) => {
    return (
        <aside className={`no-breaks-message ${props.isDisabled ? "disabled" : ""}`}>
            <i className="material-symbols-outlined breaks-icon">waves</i>
            <p>Add waves to compare results over time.</p>
            {props.isReportSettings &&
                <p>The waves you choose are applied to the entire report, but can be changed for individual charts.</p>
            }
        </aside>
    );
}

export default NoWavesMessage;