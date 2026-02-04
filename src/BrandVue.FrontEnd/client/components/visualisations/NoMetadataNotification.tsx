import React from "react";

const NoMetadataNotification = () => {
    return (
        <div className="no-metadata">
            <div className="sadness">
                <i className="material-symbols-outlined">
                    sentiment_dissatisfied
                </i>
            </div>
            <div className="text">
                <span>
                    You can't explore the data yet because the survey has no responses
                </span>
                <span>
                    Questions will be loaded automatically as soon as there are responses (test responses excluded)
                </span>
            </div>
        </div>
    );
}

export default NoMetadataNotification;