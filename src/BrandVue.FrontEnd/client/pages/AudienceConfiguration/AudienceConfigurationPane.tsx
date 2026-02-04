import _ from 'lodash';
import React from 'react';
import toast from 'react-hot-toast';
import { CompanyModel, CrossMeasure, SavedBreakCombination } from '../../BrandVueApi';
import { Metric } from '../../metrics/metric';
import { dropdownOption, textAreaOption, textInputOption } from '../HtmlConfigurationPages/ConfigurationInputOptions';

interface IAudienceConfigurationPaneProps {
    selectedAudience: SavedBreakCombination | undefined;
    validMetrics: Metric[];
    authCompanies: CompanyModel[];
    saveAudience(audience: SavedBreakCombination): void;
    deleteAudience(): void;
}

type MetricSelectOption = {
    metricName: string;
    displayName: string;
}

interface AuthCompany {
    displayName: string;
    shortcode: string | undefined;
}

function copyAudience(audience: SavedBreakCombination | undefined) {
    if (audience) {
        return new SavedBreakCombination({...audience});
    }
}

const MAX_DESCRIPTION_LENGTH = 750;

const AudienceConfigurationPane = (props: IAudienceConfigurationPaneProps) => {
    const [editingAudience, setEditingAudience] = React.useState<SavedBreakCombination | undefined>(copyAudience(props.selectedAudience))
    const isNewAudience = editingAudience && editingAudience.id <= 0;
    const metricOptions: MetricSelectOption[] = props.validMetrics.map(m => getMetricSelectOption(m));
    const selectNoneAuthCompany: AuthCompany = {
        displayName: "None",
        shortcode: undefined,
    }
    const authCompanies: AuthCompany[] = [selectNoneAuthCompany].concat(props.authCompanies.map(c => {
        return {
            displayName: c.displayName,
            shortcode: c.shortCode,
        }
    }));

    React.useEffect(() => setEditingAudience(copyAudience(props.selectedAudience)), [props.selectedAudience]);

    const saveAudience = () => {
        if (editingAudience) {
            if (!editingAudience.name || editingAudience.name.trim().length === 0) {
                toast.error('Name is required');
            } else if (!editingAudience.breaks || editingAudience.breaks.length === 0) {
                toast.error('A metric must be selected');
            } else if (editingAudience?.description && editingAudience.description.length > MAX_DESCRIPTION_LENGTH) {
                toast.error('Description should be no longer than 750 characters')
            } else{
                props.saveAudience(editingAudience);
            }
        }
    }

    const getSelectedMetricSelectOption = (): MetricSelectOption | null => {
        //currently only handling single depth CrossMeasures
        const selectedName = editingAudience?.breaks[0]?.measureName;
        const metric = props.validMetrics.find(m => m.name === selectedName);
        if (metric) {
            return getMetricSelectOption(metric);
        }
        return null;
    }

    const getSelectedAuthCompany = (): AuthCompany | null => {
        if (editingAudience?.authCompanyShortCode == null) {
            return null;
        }
        const matched = authCompanies.find(c => c.shortcode == editingAudience.authCompanyShortCode);
        if (matched) {
            return matched;
        }
        return {
            displayName: editingAudience.authCompanyShortCode,
            shortcode: editingAudience.authCompanyShortCode
        };
    }

    function getMetricSelectOption(metric: Metric): MetricSelectOption {
        let subset = '';
        if (metric.subset.length > 0) {
            const pluralized = metric.subset.length > 1 ? 'Subsets' : 'Subset';
            subset = ` (${pluralized} ${metric.subset.map(s => s.displayNameShort).join(', ')})`
        }
        return {
            metricName: metric.name,
            displayName: `${metric.varCode}${subset}`
        };
    }

    const editAudienceProperty = (updateProperty: (audience: SavedBreakCombination) => void) => {
        if (editingAudience) {
            const newAudience = new SavedBreakCombination({...editingAudience});
            updateProperty(newAudience);
            setEditingAudience(newAudience);
        }
    }

    const getSubsetMessage = () => {
        //currently only handling single depth CrossMeasures
        const selectedName = editingAudience?.breaks[0]?.measureName;
        const metric = props.validMetrics.find(m => m.name === selectedName);
        if (metric && metric.subset.length > 0) {
            const pluralized = metric.subset.length > 1 ? 'subsets' : 'subset';
            return (
                <div className='option subset-message'>This audience will only be available in {pluralized} {metric.subset.map(s => s.displayName).join(', ')}</div>
            )
        }
        return null;
    }

    const getConfigurationTitle = () => {
        if (!props.selectedAudience) {
            return "Configure audiences";
        }
        if (isNewAudience) {
            return "Configuring new audience";
        }
        return `Editing ${props.selectedAudience.name} audience`;
    }

    const descriptionPlaceholder = `A description about this audience and how it could be interpreted.

You may want to include details about what each option represents, or how comparing them could be useful to the user.`

    const getConfigurationPane = () => {
        if (!editingAudience) {
            return (
                <div className="nothing-selected-message">
                    Select an audience configuration to update, or create a new audience.
                </div>
            );
        }
        const hasMultipleBreaks = editingAudience.breaks != null &&
            (editingAudience.breaks.length > 1 || editingAudience.breaks[0]?.childMeasures?.length > 0);
        return (
            <div className='configure-audience'>
                {textInputOption(editingAudience.name, false, "Name", text => editAudienceProperty(a => a.name = text))}
                {textInputOption(editingAudience.category ?? '', false, "Category", text => editAudienceProperty(a => a.category = text))}
                {hasMultipleBreaks &&
                    <div className='option'>
                        <label>{"Metric"}</label>
                        <div className="multiple-breaks-warning">
                            <i className='material-symbols-outlined'>warning</i>
                            Saved breaks with more than one metric cannot be configured here.
                        </div>
                    </div>
                }
                {!hasMultipleBreaks && dropdownOption<MetricSelectOption>(
                    getSelectedMetricSelectOption(),
                    metricOptions,
                    false,
                    "Metric",
                    m => m.displayName,
                    m => editAudienceProperty(a => a.breaks = [new CrossMeasure({
                        measureName: m.metricName,
                        filterInstances: [],
                        childMeasures: [],
                        multipleChoiceByValue: false,
                    })])
                )}
                {getSubsetMessage()}
                {dropdownOption<AuthCompany>(
                    getSelectedAuthCompany(),
                    authCompanies,
                    false,
                    "Auth company",
                    company => company.displayName,
                    company => editAudienceProperty(a => a.authCompanyShortCode = company.shortcode)
                )}
                {textAreaOption(editingAudience.description ?? '', false, "Description", text => editAudienceProperty(a => a.description = text), MAX_DESCRIPTION_LENGTH, descriptionPlaceholder)}
            </div>
        );
    }

    const getConfigurationButtons = () => {
        if (editingAudience) {
            const canSave = isNewAudience || !_.isEqual(props.selectedAudience, editingAudience);
            return (
                <div className='configuration-buttons'>
                    <button className='hollow-button' disabled={!canSave} onClick={saveAudience}>
                        Save audience
                    </button>
                    {!isNewAudience &&
                        <button className='negative-button' onClick={props.deleteAudience}>
                            Delete audience
                        </button>
                    }
                </div>
            );
        }
    }

    return (
        <div className='configuration-area'>
            <div className='configuration-title'>
                {getConfigurationTitle()}
            </div>
            {getConfigurationPane()}
            {getConfigurationButtons()}
        </div>
    );
}

export default AudienceConfigurationPane;