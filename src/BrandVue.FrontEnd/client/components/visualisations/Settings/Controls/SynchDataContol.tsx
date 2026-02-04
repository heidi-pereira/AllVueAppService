import React from "react";
import style from "./SynchDataContol.module.less"
import { ProductConfigurationContext } from "../../../../ProductConfigurationContext";
import { DataLimiterStats, DataPreloadTaskStatus, Factory } from "../../../../BrandVueApi";
import {
    isForcedReloadAccessible
} from "../../../helpers/FeaturesHelper";
import { DataSubsetManager } from "../../../../DataSubsetManager";
import Tooltip from "../../../Tooltip";
import { toast } from 'react-hot-toast';
import { MixPanel } from '../../../mixpanel/MixPanel';

interface ISynchDataContolProps {
}

const SynchDataContol = (props: ISynchDataContolProps) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const dataCacheClient = Factory.DataCacheClient(error => error());
    const syncedDataClient = Factory.SyncedDataClient(error => error());
    const allSubsets = DataSubsetManager.getAll() || [];
    const [syncDataLimiterStats, setsyncDataLimiterStats] = React.useState<DataLimiterStats | undefined>();
    const [currentDataPreloadTaskStatus, setCurrentDataPreloadTaskStatus] = React.useState<DataPreloadTaskStatus | undefined>();

    const reloadCurrentTaskFromStorage = () => setCurrentDataPreloadTaskStatus(getCurrentDataPreloadTaskStatus());

    const LOCALSTORAGE_DATA_PRELOAD_STATUS = "data_preload_status";

    React.useEffect(() => {
        reloadCurrentTaskFromStorage();
        syncedDataClient.dataLimiterStats().then(result => { setsyncDataLimiterStats(result); });
    }, []);

    React.useEffect(() => {
        if (currentDataPreloadTaskStatus && !currentDataPreloadTaskStatus.isComplete) {
            setTimeout(() => {
                taskStatusCheck().then(() => {
                })
            }, 1000);
        }
    }, [currentDataPreloadTaskStatus]);

    const toastError = (error: Error, userFriendlyText: string) => {
        toast.error(userFriendlyText);
        console.log(error);
    }

    const userEnabledSubsets = allSubsets.filter(x => !x.disabled).map(x => x.displayName).join(",");

    const forceReloadOfSurvey = (e: any) => {
        MixPanel.track("forceReloadOfSurvey");
        dataCacheClient.forceReloadOfSurvey()
            .then(() => { })
            .catch((e: Error) => toastError(e, "An error occurred trying force the reload"));
    }
    const forceLoadReportData = (e: any) => {
        MixPanel.track("forceLoadReportData");
        dataCacheClient.forceLoadReportData()
            .then((task: DataPreloadTaskStatus) => {
                localStorage.setItem(LOCALSTORAGE_DATA_PRELOAD_STATUS, JSON.stringify(task));
            })
            .catch((e: Error) => toastError(e, "An error occurred trying force the reload"));
    }
    const formatDate = (date: Date | undefined) => {
        return date === undefined ? "" : `${date.toLocaleTimeString()} ${date.toDateString()}`;
    }
    const formatTime = (date: Date | undefined) => {
        return date === undefined ? "" : `${date.toLocaleTimeString()}`;
    }
    const tooltipText = () => {
        if (!syncDataLimiterStats?.allowReloadFromCheckingArchiveOrCompletes) {
            return `Caution: auto-reloading is disabled. There is no checking number of completes or number of respondents archived.`;
        }
        return `# Completes: ${syncDataLimiterStats?.completes} (inc archived).   # respondents archived: ${syncDataLimiterStats?.archived} (incl partials)`;
    }
    const lastReloadStyle = () => {
        if (!syncDataLimiterStats?.allowReloadFromCheckingArchiveOrCompletes)
            return style.warning;
        return "";
    }

    const getCurrentDataPreloadTaskStatus = (): DataPreloadTaskStatus | undefined => {
        const task = localStorage.getItem(LOCALSTORAGE_DATA_PRELOAD_STATUS);
        if (task) {
            return JSON.parse(task);
        }
        return undefined;
    }

    const taskStatusCheck = async () => {
        return await dataCacheClient.checkDataPreloadStatus().then((task: DataPreloadTaskStatus) => {
            if (task.errors.length > (currentDataPreloadTaskStatus ? currentDataPreloadTaskStatus.errors.length : 0)) {
                task.errors.slice(currentDataPreloadTaskStatus?.errors.length).forEach((error: string) => {
                    toastError(new Error(error), error);
                });
            }
            setCurrentDataPreloadTaskStatus(task);
            if (task && !task.isComplete && !task.isCancelled) {
                localStorage.setItem(LOCALSTORAGE_DATA_PRELOAD_STATUS, JSON.stringify(task));
            } else {
                if (task?.isCancelled) {
                    toastError(new Error("The report preloading was cancelled"), "The report preloading was cancelled. Please try again.");
                }
                localStorage.removeItem(LOCALSTORAGE_DATA_PRELOAD_STATUS);
            }
        }
        ).catch((e: Error) => {
            toastError(e, "The status of the preload task is not available. Please try again.")
            setCurrentDataPreloadTaskStatus(undefined);
            localStorage.removeItem(LOCALSTORAGE_DATA_PRELOAD_STATUS);
        });
    }

    const forceLoadReportDataButtonLabel = () => {
        if (currentDataPreloadTaskStatus) {
            if (currentDataPreloadTaskStatus.isComplete || currentDataPreloadTaskStatus.isCancelled) {
                return "Preload report data";
            } else {
                return `Preloaded ${currentDataPreloadTaskStatus.completedCount} of ${currentDataPreloadTaskStatus.totalCount}`;
            }
        }

        return "Preload report data";
    }

    const reloadCompleteNoErrors = () => currentDataPreloadTaskStatus?.isComplete && currentDataPreloadTaskStatus?.errors.length === 0 && !currentDataPreloadTaskStatus?.isCancelled;

    const reloadCompleteWithErrors = () => (currentDataPreloadTaskStatus?.isComplete && currentDataPreloadTaskStatus?.errors.length > 0) || currentDataPreloadTaskStatus?.isCancelled;

    const reloadCompleteButtonIcon = () => {
        let className = "material-symbols-outlined ";
        let iconName = "check";
        if (reloadCompleteNoErrors()) {
            className += style.success;
        }
        if (reloadCompleteWithErrors()) {
            className += style.failure;
            iconName = "close";
        }
        return <i className={className}>{iconName}</i>;
    }

    return (<div className={style.syncData}>
        <div className={style.reloaDataSection}>
            <Tooltip placement="top" title={`In order to Resync the data please go to FieldVue. AllVue next check: ${formatTime(syncDataLimiterStats?.timeToNextCheck)}`}>
                <div>Last sync date : {syncDataLimiterStats?.isResynchingData ? "Resync-ing responses": formatDate(syncDataLimiterStats?.latestDateToRequest)}</div>
            </Tooltip>
            <div className={style.last} />
        </div>
        <div className={style.reloaDataSection}>
            <Tooltip placement="top" title={tooltipText()} >
                <div className={lastReloadStyle()}>Last reload date: {formatDate(syncDataLimiterStats?.reloadedDate)}</div>
            </Tooltip>
            {(isForcedReloadAccessible(productConfiguration)) &&
                <div className={style.buttonGroup}>
                    <Tooltip placement="top" title={`This action forces SurveyVue to reload the whole survey (including questions) for all segments (${userEnabledSubsets}). Caution: this could take some time.`} >
                        <button className="secondary-button admin" onClick={(e) => forceReloadOfSurvey(e)}>Survey respondent reload</button>
                    </Tooltip>
                    <Tooltip placement="top" title={`This action forces SurveyVue to load the data for this survey's reports across all segments (${userEnabledSubsets}) into server memory. Caution: this could take some time.`} >
                        <button className="secondary-button admin" onClick={(e) => forceLoadReportData(e)} disabled={currentDataPreloadTaskStatus && !currentDataPreloadTaskStatus.isComplete}>{forceLoadReportDataButtonLabel()}{currentDataPreloadTaskStatus?.isComplete && reloadCompleteButtonIcon()}</button>
                    </Tooltip>
                </div>
            }
        </div>
    </div>);
}

export default SynchDataContol;