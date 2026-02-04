import { IAverageDescriptor, IApplicationUser, MainQuestionType, PartDescriptor, ReportType } from '../../../../BrandVueApi';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { Metric } from '../../../../metrics/metric';
import DisplayAverage from '../../DisplayAverage';
import ReportPageExcelDownload from '../Tables/ReportExcelDownload';
import ReportSelector from './ReportSelector';
import FiltersBar from '../Filtering/FiltersBar';
import { useFilterStateContext } from '../../../../filter/FilterStateContext';
import { FilterButtonType } from '../Filtering/AddFilterButton';
import CrosstabLowSampleWarning from '../../Crosstab/CrosstabLowSampleWarning';
import ReportPowerpointExportButton from './ReportPowerpointExportButton';
import { PageHandler } from '../../../PageHandler';
import { ApplicationConfiguration } from '../../../../ApplicationConfiguration';
import AverageSelector from '../../../filters/AverageSelector';
import AllVueDateRangePicker from 'client/components/visualisations/Reports/Components/AllVueDateRangePicker';
import ConfigureReportPartFilterInstance from '../Configuration/Options/ConfigureReportPartFilterInstance';
import { canSelectFilterInstances } from '../Charts/ReportsChartHelper';
import { PartWithExtraData } from '../ReportsPageDisplay';
import { getDateRangePickerTitleFromDates } from 'client/components/helpers/SurveyVueUtils';
import BrandVueOnlyLowSampleHelper from '../../BrandVueOnlyLowSampleHelper';
import { useAppSelector } from 'client/state/store';
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageTopBarProps {
    canEditReports: boolean;
    canExportData: boolean;
    metricsForReports: Metric[];
    reportsPageUrl: string;
    questionTypeLookup: {[key: string]: MainQuestionType};
    curatedFilters: CuratedFilters;
    overTimeFilters: CuratedFilters;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    user: IApplicationUser | null;
    showReportSettingsModal: () => void;
    openModalToFilterPage: () => void;
    highlightLowSample: boolean;
    isLowSample: boolean;
    isDataInSyncWithDatabase: boolean;
    applicationConfiguration: ApplicationConfiguration;
    averages: IAverageDescriptor[];
    userVisibleAverages: IAverageDescriptor[];
    isReportUsingOverTime: boolean;
    isReportPartUsingOvertime: boolean;
    startDate: Date | undefined;
    endDate: Date | undefined;
    overTimeAverage: IAverageDescriptor | undefined;
    setDates: (startDate: Date, endDate: Date) => void;
    setOverTimeAverage: (average: IAverageDescriptor) => void;
    selectedPart: PartWithExtraData | undefined;
    updatePart: (Part: PartWithExtraData) => void;
}

const ReportsPageTopBar = (props: IReportsPageTopBarProps) => {
    const { filters, metricsValidAsFilter } = useFilterStateContext();
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const getAddFiltersButton = () => {
        if (props.canEditReports) {
            return (
                <button className="hollow-button open-report-filter-menu-button" onClick={props.openModalToFilterPage}>
                    <i className="material-symbols-outlined">filter_alt</i>
                    <div>Filter</div>
                </button>
            );
        }
    }

    const canPickFilterInstances = props.selectedPart
        && canSelectFilterInstances(props.selectedPart, report)
        && report.reportType == ReportType.Table;

    const shouldShrinkButtons = filters.length > 0;
    const getLeftClassName = () => {
        return report.userHasAccess ? "left" : "left-noaccess";
    }

    return (
        <>
            <div className="report-top-bar">
                <div className={getLeftClassName()}>
                    <ReportSelector
                        user={props.user}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        questionTypeLookup={props.questionTypeLookup}
                        canEditReports={props.canEditReports}
                        applicationConfiguration={props.applicationConfiguration}
                        averages={props.averages}
                    />
                    {report.userHasAccess &&
                        <>
                        {(props.isReportUsingOverTime || props.isReportPartUsingOvertime) &&
                            <>
                                <AllVueDateRangePicker
                                    applicationConfiguration={props.applicationConfiguration}
                                    overtimeConfig={report.overTimeConfig}
                                    dropdownTitle={getDateRangePickerTitleFromDates(props.startDate, props.endDate)}
                                    onRangeSelected={(range, start, end) => props.setDates(start, end)}
                                    onCustomRangeSelected={(customRange, start, end) => props.setDates(start, end)}
                                    startDate={props.startDate}
                                    endDate={props.endDate}
                                    onDatesSelected={(start, end) => props.setDates(start, end)}
                                />
                                {props.isReportPartUsingOvertime && props.userVisibleAverages.length > 0 &&
                                    <AverageSelector
                                        average={props.overTimeAverage}
                                        userVisibleAverages={props.userVisibleAverages}
                                        updateFilterAverage={props.setOverTimeAverage}
                                    />
                                }
                            </>
                        }
                        {filters.length === 0 && metricsValidAsFilter.length === 0
                            ? getAddFiltersButton()
                            : <FiltersBar
                                user={props.user}
                                buttonType={FilterButtonType.ShowReportFilterModal}
                                openModalToFilterPage={props.openModalToFilterPage}
                            />
                        }
                        {canPickFilterInstances &&
                            <ConfigureReportPartFilterInstance
                                reportPart={props.selectedPart!}
                                canPickFilterInstances={canPickFilterInstances}
                                savePartChanges={(part: PartDescriptor) => {
                                    const partWithExtraData: PartWithExtraData = {
                                        ...props.selectedPart!,
                                        part: part
                                    };
                                    props.updatePart(partWithExtraData);
                                }}
                            />
                        }
                        </>
                    }
                </div>
                {report.userHasAccess &&
                    <div className="report-settings-and-weightings">
                        <DisplayAverage average={props.curatedFilters.average} />
                        <CrosstabLowSampleWarning isLowSample={props.highlightLowSample && props.isLowSample}
                            lowSampleThreshold={report.lowSampleThreshold ?? BrandVueOnlyLowSampleHelper.lowSampleForEntity}
                        />
                        {props.canEditReports &&
                            <button id='report-settings-button' className="hollow-button" onClick={() => props.showReportSettingsModal()}>
                                <i className="material-symbols-outlined">settings</i>
                                {!shouldShrinkButtons && <div className="report-settings-button-text">Settings</div>}
                            </button>
                        }
                        {report.reportType == ReportType.Table &&
                            <ReportPageExcelDownload
                                canExportData={props.canExportData}
                                metrics={props.metricsForReports}
                                curatedFilters={props.curatedFilters}
                                shrink={shouldShrinkButtons}
                                isDataInSyncWithDatabase={props.isDataInSyncWithDatabase}/>
                        }
                        {report.reportType == ReportType.Chart &&
                            <ReportPowerpointExportButton
                                metrics={props.metricsForReports}
                                curatedFilters={props.curatedFilters}
                                overTimeFilters={props.overTimeFilters}
                                isDataInSyncWithDatabase={props.isDataInSyncWithDatabase}
                            />
                        }
                    </div>
                }
            </div>
        </>
    )
}

export default ReportsPageTopBar;