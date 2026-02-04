import React from 'react';
import * as PageHandler from './PageHandler';
import FilterMenus from "./filters/FilterMenus";
import {dsession} from "../dsession";
import FixedPeriodFilterMenus from "./filters/FixedPeriodFilterMenus";
import ComparisonPeriodSelector from './ComparisonPeriodSelector';
import {IAverageDescriptor, PaneDescriptor} from "../BrandVueApi";
import {ViewTypeEnum} from "./helpers/ViewTypeHelper";
import {ActionEventName, ICommonVariables, IGoogleTagManager} from '../googleTagManager';
import * as moment from "moment";
import { QueryStringParamNames, useReadVueQueryParams, useWriteVueQueryParams } from './helpers/UrlHelper';
import {ApplicationConfiguration} from '../ApplicationConfiguration';
import ToggleSwitch from './checkboxes/ToggleSwitch';
import Tooltip from './Tooltip';
import {isBrandAnalysisPage, isBrandAnalysisSubPage} from './helpers/PagesHelper';
import {useLocation, useNavigate} from "react-router-dom";

interface IStandardSelectorsAndFiltersProps {
    session: dsession;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    pageHandler: PageHandler.PageHandler;
    panesToRender: PaneDescriptor[];
    averages: IAverageDescriptor[];
}

const StandardSelectorsAndFilters = (props: IStandardSelectorsAndFiltersProps) => {
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    const { getQueryParameterInt } = useReadVueQueryParams();
    const isFixedReportingPeriods = () => {
        var selectedView = props.session.coreViewType;
        let fixedPeriods = true;
        if (selectedView === ViewTypeEnum.ProfileOverTime || selectedView === ViewTypeEnum.OverTime) {
            fixedPeriods = false;
        }
        return fixedPeriods;
    }

    const utcifyMeWithoutChangingDate = (source: Date) : Date => {
        return moment.utc(moment.utc(source).format("YYYY-MM-DD")).toDate();
    }

    const addEvent = (eventName: ActionEventName, values?: ICommonVariables | null) => {
        props.googleTagManager.addEvent(eventName, props.pageHandler, { ...values, parentComponent: "FilterMenus" });
    }

    const updateFilterStartEnd = (start: Date, end: Date) => {
        const startDate = utcifyMeWithoutChangingDate(start);
        const endDate = utcifyMeWithoutChangingDate(end);
        props.session.activeView.curatedFilters.setDates(startDate, endDate);
    }

    const updateFilterAverage = (averageDescriptor: IAverageDescriptor) => {
        addEvent("changeAverage", { value: averageDescriptor.displayName });
        setQueryParameter(QueryStringParamNames.average, averageDescriptor.averageId);
        props.session.activeView.curatedFilters.average = averageDescriptor;
    }

    const getDataLabelsVisible = getQueryParameterInt(QueryStringParamNames.showDataLabels) == 1 ? true : false;
    const updateDataLabelVisibility = () => {
        setQueryParameter(QueryStringParamNames.showDataLabels, !getDataLabelsVisible ? 1 : undefined)
    };

    const showRollingPeriods = !isBrandAnalysisPage(props.session.activeDashPage) && !isBrandAnalysisSubPage(props.session.activeDashPage);

    const renderPreviousPeriodToggle = props.pageHandler.currentPanesCanShowPeriodOnPeriod();
    const renderDataLabelToggle = props.pageHandler.currentPanesCanToggleDataLabels();

    const filter = isFixedReportingPeriods()
        ? <FixedPeriodFilterMenus
            pageHandler={props.session.pageHandler}
            userVisibleAverages={props.averages}
            activeMetrics={props.session.activeView.activeMetrics}
            curatedFilters={props.session.activeView.curatedFilters}
            applicationConfiguration={props.applicationConfiguration}
            googleTagManager={props.googleTagManager}
            showRollingAverages={showRollingPeriods}
        />
        : <FilterMenus
            userVisibleAverages={props.averages}
            average={props.session.activeView.curatedFilters.average}
            configuration={props.applicationConfiguration}
            addEvent={addEvent}
            updateFilterAverage={updateFilterAverage}
            updateFilterStartEnd={updateFilterStartEnd}
        />;

    return(
        <React.Fragment>
            <div className="not-exported filter-row">
                {filter}
                {renderPreviousPeriodToggle &&
                    <ComparisonPeriodSelector session={props.session} 
                        googleTagManager={props.googleTagManager} 
                        comparisonPeriodSelection={props.session.activeView.curatedFilters.comparisonPeriodSelection} />}
                {renderDataLabelToggle &&
                    <Tooltip placement="top" title={"Show data labels"}>
                        <div>
                            <ToggleSwitch height={22} width={50} onChange={updateDataLabelVisibility} checked={getDataLabelsVisible} aria-label="Show data labels" />
                        </div>
                    </Tooltip>}
            </div>
        </React.Fragment>);
}

export default StandardSelectorsAndFilters;
