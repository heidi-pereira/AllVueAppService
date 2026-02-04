import React from 'react';
import { AverageConfiguration, AverageStrategy, CompanyModel, MakeUpTo, TotalisationPeriodUnit, WeightAcross, WeightingMethod, WeightingPeriodUnit } from '../../BrandVueApi';
import { DataSubsetManager } from '../../DataSubsetManager';
import _ from 'lodash';
import { averageStrategyName, makeUpToName, totalisationPeriodUnitName, weightAcrossName, weightingMethodName, weightingPeriodUnitName } from './AverageConfigurationHelpers';
import { checkboxOption, dropdownOption, multiselectDropdownOption, numberInputOption, textInputOption } from '../HtmlConfigurationPages/ConfigurationInputOptions';

interface IAverageConfigurationPaneProps {
    selectedAverage: AverageConfiguration | undefined;
    editingAverage: AverageConfiguration | undefined;
    isNewAverage: boolean;
    isFallbackAverage: boolean;
    authCompanies: CompanyModel[];
    saveAverage(): void;
    copyAsNewAverage(): void;
    deleteAverage(): void;
    setEditingAverage(average: AverageConfiguration): void;
    isSurveyVue: boolean;
}

interface AuthCompany {
    displayName: string;
    shortcode: string | undefined;
}

const AverageConfigurationPane = (props: IAverageConfigurationPaneProps) => {
    const {selectedAverage, editingAverage, isNewAverage, isFallbackAverage} = props;
    const subsets = DataSubsetManager.getAll();

    const totalisationPeriodUnits = [TotalisationPeriodUnit.Day, TotalisationPeriodUnit.Month, TotalisationPeriodUnit.All];
    const weightingMethods = [WeightingMethod.None, WeightingMethod.QuotaCell];
    const weightAcrosses = [WeightAcross.AllPeriods, WeightAcross.SinglePeriod];
    const averageStrategies = [AverageStrategy.OverAllPeriods, AverageStrategy.MeanOfPeriods];
    const makeUpTos = [MakeUpTo.Day, MakeUpTo.WeekEnd, MakeUpTo.MonthEnd, MakeUpTo.QuarterEnd, MakeUpTo.HalfYearEnd, MakeUpTo.CalendarYearEnd];
    const weightingPeriodUnits = [WeightingPeriodUnit.SameAsTotalization, WeightingPeriodUnit.FullScheme];
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

    const getSelectedAuthCompany = (): AuthCompany | null => {
        if (editingAverage?.authCompanyShortCode == null) {
            return null;
        }
        const matched = authCompanies.find(c => c.shortcode == editingAverage.authCompanyShortCode);
        if (matched) {
            return matched;
        }
        return {
            displayName: editingAverage.authCompanyShortCode,
            shortcode: editingAverage.authCompanyShortCode
        };
    }

    const editAverageProperty = (updateProperty: (average: AverageConfiguration) => void) => {
        if (editingAverage) {
            const newAverage = new AverageConfiguration({...editingAverage});
            updateProperty(newAverage);
            props.setEditingAverage(newAverage);
        }
    }

    const getConfigurationTitle = () => {
        if (!selectedAverage) {
            return "Configure averages";
        }
        if (isFallbackAverage) {
            return `Viewing ${selectedAverage?.displayName} average (fallback)`;
        }
        if (isNewAverage) {
            return "Configuring new average";
        }
        return `Editing ${selectedAverage?.displayName} average`;
    }

    const getConfigurationPane = () => {
        if (!editingAverage) {
            return (
                <div className="nothing-selected-message">
                    Select an average configuration to update, or create a new average.
                    <br />
                    {!props.isSurveyVue && <>If no averages have been configured, the fallback averages will be used.</>}
                </div>
            );
        }
        const disabled = isFallbackAverage;
        return (
            <div id='configure-average' className='configure-average'>
                {textInputOption(editingAverage.averageId, disabled, "Average ID", text => editAverageProperty(avg => avg.averageId = text))}
                {textInputOption(editingAverage.displayName, disabled, "Display name", text => editAverageProperty(avg => avg.displayName = text))}
                {numberInputOption(editingAverage.order, disabled, "Order", number => editAverageProperty(avg => avg.order = number))}
                {textInputOption(editingAverage.group.join('|'), disabled, "Group", text => editAverageProperty(avg => avg.group = text.split('|')))}
                <div className='option-row'>
                    {numberInputOption(
                        editingAverage.numberOfPeriodsInAverage,
                        disabled,
                        "Number of periods in average",
                        number => editAverageProperty(avg => avg.numberOfPeriodsInAverage = number)
                    )}
                    {dropdownOption<TotalisationPeriodUnit>(
                        editingAverage.totalisationPeriodUnit,
                        totalisationPeriodUnits,
                        disabled,
                        "Totalisation period unit",
                        unit => totalisationPeriodUnitName(unit, editingAverage.numberOfPeriodsInAverage !== 1),
                        unit => editAverageProperty(avg => avg.totalisationPeriodUnit = unit)
                    )}
                </div>
                {dropdownOption<WeightingMethod>(
                    editingAverage.weightingMethod,
                    weightingMethods,
                    disabled,
                    "Weighting method",
                    weightingMethodName,
                    weightingMethod => editAverageProperty(avg => avg.weightingMethod = weightingMethod)
                )}
                {dropdownOption<WeightAcross>(
                    editingAverage.weightAcross,
                    weightAcrosses,
                    disabled,
                    "Weight across",
                    weightAcrossName,
                    weightAcross => editAverageProperty(avg => avg.weightAcross = weightAcross)
                )}
                {dropdownOption<AverageStrategy>(
                    editingAverage.averageStrategy,
                    averageStrategies,
                    disabled,
                    "Average strategy",
                    averageStrategyName,
                    averageStrategy => editAverageProperty(avg => avg.averageStrategy = averageStrategy)
                )}
                {dropdownOption<MakeUpTo>(
                    editingAverage.makeUpTo,
                    makeUpTos,
                    disabled,
                    "Make up to",
                    makeUpToName,
                    makeUpTo => editAverageProperty(avg => avg.makeUpTo = makeUpTo)
                )}
                {dropdownOption<WeightingPeriodUnit>(
                    editingAverage.weightingPeriodUnit,
                    weightingPeriodUnits,
                    disabled,
                    "Weighting period unit",
                    weightingPeriodUnitName,
                    unit => editAverageProperty(avg => avg.weightingPeriodUnit = unit)
                )}
                {multiselectDropdownOption(
                    editingAverage.subsetIds.map(id => subsets.find(s => s.id === id)),
                    subsets,
                    disabled,
                    'Survey subset',
                    subset => subset!.displayName,
                    selectedSubsets => editAverageProperty(avg => avg.subsetIds = selectedSubsets.map(s => s!.id)),
                    'Every subset'
                )}
                {dropdownOption<AuthCompany>(
                    getSelectedAuthCompany(),
                    authCompanies,
                    disabled,
                    "Auth company",
                    company => company.displayName,
                    company => editAverageProperty(avg => avg.authCompanyShortCode = company.shortcode)
                )}
                {checkboxOption(
                    editingAverage.disabled,
                    disabled,
                    'Disabled',
                    checked => editAverageProperty(avg => avg.disabled = checked)
                )}
                {checkboxOption(
                    editingAverage.isDefault,
                    disabled,
                    'Default',
                    checked => editAverageProperty(avg => avg.isDefault = checked)
                )}
                {checkboxOption(
                    editingAverage.includeResponseIds,
                    disabled,
                    'Include response IDs',
                    checked => editAverageProperty(avg => avg.includeResponseIds = checked)
                )}
                {checkboxOption(
                    editingAverage.allowPartial,
                    disabled,
                    'Allow partial',
                    checked => editAverageProperty(avg => avg.allowPartial = checked)
                )}
            </div>
        );
    }

    const getConfigurationButtons = () => {
        if (editingAverage) {
            const canSave = isNewAverage || !_.isEqual(selectedAverage, editingAverage);
            return (
                <div className='configuration-buttons'>
                    {!isFallbackAverage &&
                        <button className='hollow-button' disabled={!canSave} onClick={props.saveAverage}>
                            {isNewAverage ? 'Save new' : 'Save updated'} average
                        </button>
                    }
                    {(isFallbackAverage || !isNewAverage) &&
                        <button className='hollow-button' onClick={props.copyAsNewAverage}>
                            Copy as new average
                        </button>
                    }
                    {!isFallbackAverage && !isNewAverage &&
                        <button className='negative-button' onClick={props.deleteAverage}>
                            Delete average
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

export default AverageConfigurationPane;