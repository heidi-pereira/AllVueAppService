import React from 'react';

const WarningWithToggle = (props: { text: string, toggleVisibility: () => void}) => {
    return <div className="sampleWarning not-exported" onClick={() => props.toggleVisibility()}>
        <i className="material-symbols-outlined">warning</i>
        <span>{props.text}</span>
    </div>;
}
export default WarningWithToggle;