import React from "react";

interface IProps {
    title: string;
    message: string;
}

const ErrorDisplay: React.FunctionComponent<React.PropsWithChildren & IProps> = ({ title, message, children }) => (
    <div id="ErrorView">
        <span id="loaded" data-error="true" style={{ display: 'none' }}></span>
        <div className="BasicError">
            <h2>{title}</h2>
            <h3 dangerouslySetInnerHTML={{ __html: message }} />
            {children}
        </div>
    </div>);

export default ErrorDisplay;