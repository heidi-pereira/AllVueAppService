import React from 'react';

const PageInfo = (props: { helpText: string }) => {

    if (!props.helpText) return null;

    return (
        <div id="introtext" className="show">
            <div className="info-container">
                <span className="survey-question-title">Survey question / chart information</span>
                <span dangerouslySetInnerHTML={{ __html: props.helpText }}></span>
            </div>
        </div>
    );
};

export default PageInfo;