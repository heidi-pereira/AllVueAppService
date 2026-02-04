import React from "react";
import { DataLoadInProgressError } from "../DataLoadInProgressError";
import ReloadingDataView from "./ReloadingDataView";
import deepEqual from 'deep-equal';
import { InnerErrorReport } from "./InnerErrorReport";
import { ApplicationConfiguration } from "../ApplicationConfiguration";
import {useEffect, useState} from "react";
import { useReadVueQueryParams, useWriteVueQueryParams } from "./helpers/UrlHelper";
import {useLocation, useNavigate, useSearchParams} from "react-router-dom";

export const CatchReportAndDisplayErrors: React.FC<{
    applicationConfiguration: ApplicationConfiguration,
    childInfo: { [key: string]: string },
    url?: string,
    startPagePath?: string,
    startPageName?: string,
    children: React.ReactNode
}> = ({ applicationConfiguration, childInfo: initialChildInfo, url: initialUrl, startPagePath, startPageName, children }) => {
    const location = useLocation();
    const [state, setState] = useState({
        isReloading: false,
        childInfo: initialChildInfo
    });
    const writeVueQueryParams = useWriteVueQueryParams(useNavigate(), useLocation());
    const readVueQueryParams = useReadVueQueryParams();
    useEffect(() => {
        const nextUrl = initialUrl || location.pathname;
        if (!deepEqual(state.childInfo, initialChildInfo)) {
            setState({
                isReloading: false,
                childInfo: initialChildInfo
            });
        }
    }, [initialUrl, initialChildInfo]);

    const isRequiredDataLoadInProgressError = (error: any): boolean =>
        error.typeDiscriminator === DataLoadInProgressError.typeDiscriminator;

    const handleCatch = (error: Error) => {
        const requiredDataStillLoading = isRequiredDataLoadInProgressError(error);
        setState(prev => ({ ...prev, isReloading: requiredDataStillLoading }));

        if (requiredDataStillLoading) {
            applicationConfiguration.waitForDataReload(readVueQueryParams, writeVueQueryParams)
                .then(() => setState(prev => ({ ...prev, isReloading: false })));
        }
    };

    try {
        if (state.isReloading) return <ReloadingDataView />;

        return (
            <InnerErrorReport
                childInfo={state.childInfo}
                ignoredErrors={[DataLoadInProgressError.typeDiscriminator]}
                startPageName={startPageName}
                startPagePath={startPagePath}
                url={location.pathname + location.search}
            >
                {children}
            </InnerErrorReport>
        );
    } catch (error) {
        handleCatch(error as Error);
        if (state.isReloading) return <ReloadingDataView />;
        throw error;
    }
};
