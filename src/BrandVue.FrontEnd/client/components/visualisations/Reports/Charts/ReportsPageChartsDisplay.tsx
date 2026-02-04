import { CrossMeasure, MainQuestionType, PartDescriptor, Report } from '../../../../BrandVueApi';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { CatchReportAndDisplayErrors } from '../../../CatchReportAndDisplayErrors';
import ReportsPageCard, { ReportsPageCardType } from '../Cards/ReportsPageCard';
import { PartWithExtraData } from '../ReportsPageDisplay';
import {useEffect, useRef} from "react";
import { ApplicationConfiguration } from '../../../../ApplicationConfiguration';
import { PageHandler } from '../../../PageHandler';
import { useAppDispatch, useAppSelector } from 'client/state/store';
import { setReportErrorState } from '../../../../state/reportSlice';
import { initialReportErrorState } from '../../shared/ReportErrorState';
import { isUsingOverTime } from './ReportsChartHelper';
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageChartsDisplayProps {
    applicationConfiguration: ApplicationConfiguration;
    canEditReport: boolean;
    reportParts: PartWithExtraData[];
    curatedFilters: CuratedFilters;
    overTimeFilters: CuratedFilters;
    questionTypeLookup: {[key: string]: MainQuestionType};
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    getPartBreaks(part: PartWithExtraData): CrossMeasure[];
    getPartWaves(part: PartDescriptor): CrossMeasure | undefined;
    removePartFromReport(part: PartWithExtraData): void;
    viewChartPage(partToView: PartWithExtraData): void;
    showWeightedCounts: boolean;
    scrollY: number | undefined;
    setScrollY: (scrollY: number | undefined) => void;
    duplicatePart(partDescriptor: PartDescriptor): void;
}

const ReportsPageChartsDisplay = (props: IReportsPageChartsDisplayProps) => {
    const dispatch = useAppDispatch();
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    const scrollbarValueRef = useRef<HTMLDivElement>(null)

    const getPartBreaks = (part: PartWithExtraData): CrossMeasure[] | undefined => {
        const breaks = props.getPartBreaks(part);
        const useSingleBreak = breaks != null && breaks.length > 0 && breaks[0].filterInstances.length > 0;
        const useMultipleBreaks = breaks != null && breaks.length > 1;
        if (useSingleBreak || useMultipleBreaks) {
            return breaks;
        }
    }

    useEffect(() => {
        dispatch(setReportErrorState(initialReportErrorState)) //to reset the error that might be selected in single card view
        const handler = setTimeout(() => {
            if (scrollbarValueRef.current && props.scrollY && scrollbarValueRef.current?.scrollTop !== props.scrollY){
                scrollbarValueRef.current.scrollTop = props.scrollY
            }
        }, 50)
        return () => {clearTimeout(handler)}
    }, [props.scrollY])

    return (
        <div className="reports-grid" ref={scrollbarValueRef} onScroll={() => props.setScrollY(scrollbarValueRef.current?.scrollTop)}>
            {props.reportParts.length === 0 &&
                <div className="no-charts-message">
                    No charts
                </div>
            }
            {props.reportParts.map(p =>
                <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration}
                    childInfo={{
                        "Part": p.part.partType,
                        "Spec1": p.part.spec1,
                        "Spec2": p.part.spec2,
                        "Spec3": p.part.spec3,
                        "Report": report.savedReportId.toString()
                    }}
                    key={'catch-' + p.part.spec1 + p.part.spec2 + p.part.spec3}
                >
                    <ReportsPageCard
                        reportPart={p}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        curatedFilters={props.curatedFilters}
                        overTimeFilters={props.overTimeFilters}
                        questionTypeLookup={props.questionTypeLookup}
                        ref={p.ref}
                        cardType={ReportsPageCardType.Tile}
                        viewChartPage={() => props.viewChartPage(p)}
                        key={p.part.spec1 + p.part.spec2 + p.part.spec3}
                        reportOrder={report.reportOrder}
                        canEditReport={props.canEditReport}
                        removeFromReport={() => props.removePartFromReport(p)}
                        breaks={getPartBreaks(p)}
                        waves={props.getPartWaves(p.part)}
                        showWeightedCounts={props.showWeightedCounts}
                        updateBreaks={() => { }}
                        updateWave={() => { }}
                        duplicatePart={props.duplicatePart}
                        updatePart={() => {}}
                        isUsingOverTime={isUsingOverTime(report, p)}
                    />
                </CatchReportAndDisplayErrors>
            )}
            <div className="spacer"></div>
        </div>
    );
}

export default ReportsPageChartsDisplay;