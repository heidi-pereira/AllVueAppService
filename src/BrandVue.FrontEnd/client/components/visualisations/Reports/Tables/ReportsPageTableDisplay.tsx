import React from 'react';
import { ApplicationConfiguration } from '../../../../ApplicationConfiguration';
import { CrossMeasure, IApplicationUser, Report, MainQuestionType } from '../../../../BrandVueApi';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { CatchReportAndDisplayErrors } from '../../../CatchReportAndDisplayErrors';
import ReportsPageConfigureMenu from '../Configuration/ConfigureReportPartMenu';
import { PartWithExtraData } from '../ReportsPageDisplay';
import ReportsTable from './ReportsTable';
import { PaginationData } from '../../PaginationData';
import { PageHandler } from '../../../PageHandler';
import { useAppSelector } from 'client/state/store';
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageTableDisplayProps {
    canEditReport: boolean;
    focusedPart: PartWithExtraData | undefined;
    breaks: CrossMeasure[];
    curatedFilters: CuratedFilters;
    questionTypeLookup: {[key: string]: MainQuestionType};
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    user: IApplicationUser | null;
    applicationConfiguration: ApplicationConfiguration;
    setCanDownload(canDownload: boolean): void;
    updatePart(newPart: PartWithExtraData): void;
    setIsLowSample(isLowSample: boolean): void;
    isDataWeighted: boolean;
    setPagination: (pageNo: number, noOfTablesPerPage: number, totalNoOfTables: number) => void;
    maxNoOfTablesPerPage: number;
    paginationData: PaginationData;
}

const ReportsPageTableDisplay = (props: IReportsPageTableDisplayProps) => {

    const [configureMenuVisible, setConfigureChartMenuVisible] = React.useState<boolean>(true);
    props.setCanDownload(props.focusedPart != null);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    return (
        <div className={configureMenuVisible ? "table editing" : "table"}>
            {props.focusedPart &&
                <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration}
                    childInfo={{
                        "Part": props.focusedPart.part.partType,
                        "Spec1": props.focusedPart.part.spec1,
                        "Spec2": props.focusedPart.part.spec2,
                        "Spec3": props.focusedPart.part.spec3,
                        "Report": report.savedReportId.toString()
                    }}
                >
                    <ReportsTable curatedFilters={props.curatedFilters}
                        canEditReport={props.canEditReport}
                        selectedPart={props.focusedPart}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        categories={props.breaks}
                        isTableConfigurationVisible={configureMenuVisible}
                        setIsTableConfigurationVisible={setConfigureChartMenuVisible}
                        questionTypeLookup={props.questionTypeLookup}
                        setIsLowSample={(isLowSample) => props.setIsLowSample(isLowSample)}
                        isDataWeighted={props.isDataWeighted}
                        paginationData={props.paginationData}
                        setPagination={props.setPagination}
                        maxNoOfTablesPerPage={props.maxNoOfTablesPerPage}/>
                </CatchReportAndDisplayErrors>
            }
            {props.canEditReport && props.focusedPart && configureMenuVisible &&
                <ReportsPageConfigureMenu
                    reportPart={props.focusedPart}
                    visible={configureMenuVisible}
                    questionTypeLookup={props.questionTypeLookup}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    user={props.user}
                    updatePart={props.updatePart}
                    closeMenu={() => setConfigureChartMenuVisible(false)}
                />
            }
        </div>
    );
}


export default ReportsPageTableDisplay;