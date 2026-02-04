import { IApplicationUser } from '../../../../BrandVueApi';
import AddFilterButton, { FilterButtonType } from './AddFilterButton';
import SelectedFilterButton from './SelectedFilterButton';
import { useFilterStateContext } from '../../../../filter/FilterStateContext';
import { groupMetricFiltersByMeasureName } from './FilterHelper';

interface IFiltersBarProps {
    user: IApplicationUser | null;
    separateVariables?: boolean;
    buttonType: FilterButtonType;
    openModalToFilterPage? : () => void;
}

const FiltersBar = (props: IFiltersBarProps) => {

    const { filters } = useFilterStateContext();
    const groupedFilters = groupMetricFiltersByMeasureName(filters);

    return (
        <div className="filter-buttons">
            {groupedFilters.map(group =>
                <SelectedFilterButton
                    key={group[0].name}
                    selectedMetric={group[0].metric}
                    selectedFiltersForMetric={group}
                />
            )}

            <AddFilterButton
                separateVariables={props.separateVariables}
                buttonType={props.buttonType}
                openModalToFilterPage={props.openModalToFilterPage}
            />
        </div>
    );
}

export default FiltersBar;