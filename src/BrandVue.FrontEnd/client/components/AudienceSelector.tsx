import { useEntityConfigurationStateContext } from '../entity/EntityConfigurationStateContext';
import React from 'react';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import {
    CrossMeasure,
    MainQuestionType,
    IEntityType,
    SavedBreakCombination,
} from '../BrandVueApi';
import { IGoogleTagManager } from '../googleTagManager';
import { MetricSet } from '../metrics/metricSet';
import { useMetricStateContext } from '../metrics/MetricStateContext';
import {
    filterAudiences,
    getActiveBreaksFromSelection,
    getCrossMeasureForAudience,
    groupAudiencesByCategory,
    UNCATEGORIZED_AUDIENCE_NAME,
    setBreaksAndPeriod
} from './helpers/AudienceHelper';
import { getAvailableCrossMeasureFilterInstances } from './helpers/SurveyVueUtils';
import { PageHandler } from './PageHandler';
import SearchInput from './SearchInput';
import { useSavedBreaksContext } from './visualisations/Crosstab/SavedBreaksContext';
import CrossMeasureInstanceSelector from './visualisations/Reports/Components/CrossMeasureInstanceSelector';
import { MixPanel } from './mixpanel/MixPanel';
import { IActiveBreaks } from 'client/state/entitySelectionSlice';
import { useAppDispatch } from 'client/state/store';
import { useWriteVueQueryParams } from "./helpers/UrlHelper";
import { useLocation, useNavigate } from 'react-router-dom';

interface IAudienceSelectorProps {
    activeBreaks: IActiveBreaks | undefined;
    activeEntityType: IEntityType;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
}

const LOCALSTORAGE_RECENT_AUDIENCES = "recent_audiences";

const AudienceSelector = (props: IAudienceSelectorProps) => {
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const [searchQuery, setSearchQuery] = React.useState<string>("");
    const [hoveredAudience, setHoveredAudience] = React.useState<SavedBreakCombination | undefined>(undefined);

    const { enabledMetricSet, questionTypeLookup } = useMetricStateContext();
    const { savedBreaks } = useSavedBreaksContext();
    const availableAudiences = getAvailableAudiences(enabledMetricSet, savedBreaks);

    const selectedAudience = savedBreaks.find(b => b.id === props.activeBreaks?.audienceId);
    const selectedMetric = getSelectedMetric(enabledMetricSet, selectedAudience?.breaks[0]?.measureName);
    const crossMeasure = getCrossMeasureForAudience(selectedAudience, props.activeBreaks?.selectedInstanceOrMappingIds,
        props.activeBreaks?.multipleChoiceByValue, selectedMetric, questionTypeLookup);
    const selectedAudienceName = selectedAudience == null ? "Everyone" : selectedAudience.name;
    const recentAudiences = getRecentAudiences(availableAudiences);
    const {entityConfiguration} = useEntityConfigurationStateContext();
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    const dispatch = useAppDispatch();
    
    const toggleAudienceDropdown = () => {
        setDropdownOpen(!dropdownOpen);
        if (!dropdownOpen) {
            setHoveredAudience(undefined);
        }
    }

    const updateAudience = (audience: SavedBreakCombination | undefined) => {
        if (audience?.id !== selectedAudience?.id) {
            const metric = getSelectedMetric(enabledMetricSet, audience?.breaks[0]?.measureName);
            let crossMeasure: CrossMeasure | undefined = undefined;
            if (metric && audience?.breaks[0]) {
                const isBasedOnSingleChoice = questionTypeLookup[metric.name] == MainQuestionType.SingleChoice;
                crossMeasure = new CrossMeasure({
                    ...audience.breaks[0],
                    filterInstances: getAvailableCrossMeasureFilterInstances(metric, entityConfiguration, false, isBasedOnSingleChoice).slice(0, 2),
                    multipleChoiceByValue: false
                });
            }
            if (audience?.id) {
                const recentAudienceIds = recentAudiences.map(a => a.id);
                recentAudienceIds.push(audience.id);
                setRecentAudiences(recentAudienceIds);
                MixPanel.track('selectedAudience');
                props.googleTagManager.addEvent('audiencesSelectAudience', props.pageHandler, { value: audience.id.toString() });
            } else {
                MixPanel.track('removeAudienceBreaks');
                props.googleTagManager.addEvent('audiencesSelectNone', props.pageHandler);
            }

            var breaks = getActiveBreaksFromSelection(audience, crossMeasure, metric, questionTypeLookup);
            setBreaksAndPeriod(breaks, setQueryParameter, props.pageHandler.session.activeView.curatedFilters, props.pageHandler, dispatch);
        }
    }
    
    const updateInstances = (crossMeasure: CrossMeasure | undefined) => {
        var breaks = getActiveBreaksFromSelection(selectedAudience, crossMeasure, selectedMetric, questionTypeLookup);
        setBreaksAndPeriod(breaks, setQueryParameter, props.pageHandler.session.activeView.curatedFilters, props.pageHandler, dispatch);
        props.googleTagManager.addEvent('audiencesToggleInstances', props.pageHandler);
    }

    const getNoAudienceMessage = () => {
        return (
            <aside className="no-audience-message">
                <i className="material-symbols-outlined">group</i>
                <p>Choose an audience to compare the results of different groups of respondents for this metric.</p>
                <p>For example, you can compare Millenials to Gen Z, or your current customers to lapsed customers, and more.</p>
            </aside>
        );
    }

    const getBadAudienceMessage = () => {
        return (
            <aside className="bad-audience-message">
                <i className="material-symbols-outlined">warning</i>
                <p>An error occurred when displaying this audience. The question it uses may have had its name changed.</p>
            </aside>
        );
    }

    const getInstanceListAndDescription = () => {
        if (!selectedAudience) {
            return getNoAudienceMessage();
        }

        if (crossMeasure) {
            if (!selectedMetric) {
                return getBadAudienceMessage();
            }

            return (
                <div className='chart-break-container'>
                    <CrossMeasureInstanceSelector
                        selectedCrossMeasure={crossMeasure}
                        selectedMetric={selectedMetric}
                        activeEntityType={props.activeEntityType}
                        setCrossMeasures={(crossMeasures) => updateInstances(crossMeasures[0])}
                        disabled={false}
                        includeSelectAll={true}
                    />
                    <div className='audience-description'>{selectedAudience.description}</div>
                </div>
            );
        }
    }

    const getAudienceDropdownItem = (audience: SavedBreakCombination, category: string, tabbed: boolean) => {
        return (
            <DropdownItem
                key={`${category}_${audience.name}`}
                onClick={() => updateAudience(audience)}
                onMouseOver={() => setHoveredAudience(audience)}
                className={tabbed ? 'tabbed' : ''}
            >
                <span title={audience.name}>{audience.name}</span>
            </DropdownItem>
        )
    }

    const filteredRecentAudiences = filterAudiences(recentAudiences, searchQuery).reverse();
    const categorizedAudiences = groupAudiencesByCategory(filterAudiences(availableAudiences, searchQuery));
    const categoryNames = Object.keys(categorizedAudiences)
        .filter(name => name != UNCATEGORIZED_AUDIENCE_NAME)
        .sort((a,b) => a.localeCompare(b));
    const uncategorizedAudiences = categorizedAudiences[UNCATEGORIZED_AUDIENCE_NAME];
    const hasAnyCategories = categoryNames.length > 0 || filteredRecentAudiences.length > 0;
    return (
        <div className="audience-selector">
            <div className="metric-dropdown-menu">
                <ButtonDropdown isOpen={dropdownOpen} toggle={toggleAudienceDropdown} className="metric-dropdown">
                    <DropdownToggle className="metric-selector-toggle toggle-button">
                        <i className="material-symbols-outlined">group</i>
                        <div className="title">{selectedAudienceName}</div>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu className="full-width audience-dropdown">
                        <SearchInput id="metric-search-input" onChange={(text) => setSearchQuery(text)} autoFocus={true} text={searchQuery} />
                        <DropdownItem divider />
                        <div className="audience-items">
                            <DropdownItem onClick={() => updateAudience(undefined)} onMouseOver={() => setHoveredAudience(undefined)}><span title='Everyone'>Everyone</span></DropdownItem>
                            {filteredRecentAudiences.length > 0 &&
                                <>
                                    <DropdownItem header key='recent'><span title='Recently used'>Recently used</span></DropdownItem>
                                    {filteredRecentAudiences.map(a => getAudienceDropdownItem(a, 'Recently used', true))}
                                </>
                            }
                            {categoryNames.map(category => {
                                return (
                                    <div key={category}>
                                        <DropdownItem header key={category}><span title={category}>{category}</span></DropdownItem>
                                        {categorizedAudiences[category].group.map(a => getAudienceDropdownItem(a, category, true))}
                                    </div>
                                );
                            })}
                            {uncategorizedAudiences &&
                                <>
                                    {hasAnyCategories && <DropdownItem header><span title={UNCATEGORIZED_AUDIENCE_NAME}>{UNCATEGORIZED_AUDIENCE_NAME}</span></DropdownItem>}
                                    {uncategorizedAudiences.group.map(a => getAudienceDropdownItem(a, UNCATEGORIZED_AUDIENCE_NAME, hasAnyCategories))}
                                </>
                            }
                        </div>
                        {hoveredAudience?.description != null && hoveredAudience.description.trim().length > 0 &&
                            <>
                                <DropdownItem divider />
                                <div className='dropdown-focused-audience'>
                                    <div className='audience-name'>{hoveredAudience.name}</div>
                                    <div className='audience-description'>{hoveredAudience.description}</div>
                                </div>
                            </>
                        }
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
            {getInstanceListAndDescription()}
        </div>
    );
}

function getSelectedMetric(enabledMetricSet: MetricSet, measureName: string | undefined) {
    if (measureName) {
        return enabledMetricSet.getMetric(measureName);
    }
}

function getAvailableAudiences(enabledMetricSet: MetricSet, audiences: SavedBreakCombination[]) {
    //some audiences may reference a metric which has had its name changed, or a metric not available in the current subset
    //these can be hidden from the dropdown list as they won't be usable if selected
    return audiences.filter(a => {
        const metric = getSelectedMetric(enabledMetricSet, a.breaks[0]?.measureName);
        return metric != null;
    });
}

function getRecentAudiences(existingAudiences: SavedBreakCombination[]): SavedBreakCombination[] {
    const recentAudienceIds = JSON.parse(localStorage.getItem(LOCALSTORAGE_RECENT_AUDIENCES) ?? '[]');
    if (Array.isArray(recentAudienceIds) && recentAudienceIds.every(id => !isNaN(id))) {
        const audiences = recentAudienceIds.map(id => existingAudiences.find(b => b.id === id));
        return audiences.filter((a): a is SavedBreakCombination => !!a);
    }
    return [];
}

function setRecentAudiences(recentAudienceIds: number[]) {
    const mostRecentThreeUnique = recentAudienceIds
        .filter((id, index) => recentAudienceIds.lastIndexOf(id) === index)
        .slice(-3);
    localStorage.setItem(LOCALSTORAGE_RECENT_AUDIENCES, JSON.stringify(mostRecentThreeUnique));
}

export default AudienceSelector;