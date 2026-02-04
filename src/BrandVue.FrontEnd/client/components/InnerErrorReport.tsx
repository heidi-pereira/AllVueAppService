import React from "react";
import { IKnownError } from "../IKnownError";
import { ErrorDetails, ErrorLevel, Factory, IEntityType } from "../BrandVueApi";
import { NoDataError } from "../NoDataError";
import Freshchat from "../freshchat";
import ErrorDisplay from "./ErrorsDisplay";
import deepEqual from 'deep-equal';
export class InnerErrorReport extends React.Component<React.PropsWithChildren & { ignoredErrors?: string[], childInfo: { [key: string]: string; }, url?: string, startPagePath?: string, startPageName?: string },
    { hasError: boolean; error: any; info: any; displayErrorToUser: boolean, isLogged: boolean, hasEmptyData: boolean, url: string, childInfo: { [key: string]: string; } }> {
    constructor(props) {
        super(props);
        this.state = {
            hasError: false,
            displayErrorToUser: false,
            error: undefined,
            info: undefined,
            isLogged: false,
            hasEmptyData: false,
            childInfo: props.childInfo,
            url: props.url || window.location.href
        };
    }

    static getDerivedStateFromProps(nextProps, prevState) {
        const nextUrl = nextProps.url || window.location.href;
        if (nextUrl !== prevState.url ||
            !deepEqual(prevState.childInfo, nextProps.childInfo)) {
            return {
                hasError: false,
                hasEmptyData: false,
                childInfo: nextProps.childInfo,
                url: nextUrl
            };
        }
        else return null;
    }



    componentDidCatch(error, info) {
        const errorWithDiscriminator = error as any;
        if (this.props.ignoredErrors && this.props.ignoredErrors.includes(errorWithDiscriminator.typeDiscriminator)) {
            throw error;
        }

        const knownError = error as IKnownError;
        const logLevel = knownError.logLevel || ErrorLevel.Error;
        const shouldDisplayThisErrorTypeToUser = knownError.displayDetailsToUser || true;
        const emptyData = this.hasNoDataError(error);
        this.setState({
            hasError: !emptyData,
            error: error,
            info: info,
            isLogged: false,
            hasEmptyData: emptyData
        });
        const handleLoggingError = (err: () => never) => this.setState({
            hasError: true,
            error: err,
            info: "Failed to report error to server",
            displayErrorToUser: true, //Show user as much detail as possible if not reported
            isLogged: false
        });
        const serverError = Factory.ClientErrorClient(handleLoggingError);

        if (logLevel !== ErrorLevel.DoNotLog) {
            const errorDetails = new ErrorDetails();
            errorDetails.errorLevel = logLevel;
            errorDetails.message = error.message;
            errorDetails.url = this.state.url;
            errorDetails.extraInfo = this.props.childInfo;
            errorDetails.stack = error.stack;

            serverError.logError(errorDetails).then(serverAllowsDisplayingError =>
                this.setState({
                    displayErrorToUser: serverAllowsDisplayingError && shouldDisplayThisErrorTypeToUser,
                    isLogged: true
                })
            ).catch(handleLoggingError);
        }
    }

    hasNoDataError(error: any): boolean {
        let result = error.typeDiscriminator === NoDataError.typeDiscriminator;
        return result;
    }

    showFreshChat = (e: any) => {
        e.preventDefault();
        Freshchat.GetOrCreateWidget().show({name: 'Report an error'});
    }

    render() {
        if (this.state.hasError) return this.renderErrorView();
        if (this.state.hasEmptyData) return this.renderNoDataView();

        return this.props.children;
    }

    renderNoDataView() {
        return <ErrorDisplay
            title={`No data for the time period and other filters you have selected.`}
            message={`Please <a href=${this.state.url.split('?')[0]}>click here</a> to reset your filters.`}
        />
    }

    renderErrorView() {
        let responseData: any = {text: this.state.error.response};
        try{
            responseData = JSON.parse(responseData.text);
        } catch (err){
        }

        return <ErrorDisplay
            title={`An error has occurred${this.state.isLogged ? " and we are looking into it." : "."}`}
            message="Please try these steps to get back on track:"
        >
            <ul>
                <li>Refresh the page in your browser</li>
                {this.props.startPagePath &&
                <li>Try going back to the <a href={this.props.startPagePath}>{this.props.startPageName}</a> page</li>
                }
                {Freshchat.isEnabled &&
                <li>If the error persists, <a href="#" onClick={this.showFreshChat}>get in contact with us</a></li>
                }
            </ul>

            {this.state.displayErrorToUser &&
            <div className="mt-5 pt-3 debugInfo">
                <h3>Useful debug info</h3>
                <div>URL: <span className="errorValue">{this.state.url}</span></div>
                {this.renderObjectProperties(this.props.childInfo)}
                {this.renderObjectProperties(responseData)}
                {responseData.error
                    ? this.renderErrorBlock(responseData.error.message, responseData.error.stackTrace)
                    : this.renderErrorBlock(this.state.error.message, this.state.error.stack)
                }
            </div>
            }
        </ErrorDisplay>;
    }

    renderErrorBlock(message: string, stackTrace: string) {
        return (
            <>
                <div className="pb-4 pt-5 errorValue" dangerouslySetInnerHTML={{
                    __html: message
                }}/>
                <pre>
                    <code>
                        <div className="pb-5 pt-4" dangerouslySetInnerHTML={{
                            __html: stackTrace
                        }}/>
                    </code>
                </pre>
            </>
        );
    }

    renderObjectProperties(obj) {
        return Object.keys(obj).filter(key => obj[key] !== "" && typeof obj[key] !== 'object').map(
            key =>
            <div key={key}>{key}: <span className="errorValue">{obj[key]
            }</span></div>
        );
    }
}