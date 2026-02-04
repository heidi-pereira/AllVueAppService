import React from 'react';
import { CrossMeasure, MainQuestionType, ReportVariableAppendType, ReportWaveConfiguration, ReportWavesOptions } from "../../../../BrandVueApi";
import NoWavesMessage from './NoWavesMessage';
import ReportCrossMeasurePicker from './ReportCrossMeasurePicker';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { useState } from 'react';
import CrossMeasureInstanceSelector from './CrossMeasureInstanceSelector';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';

interface IReportWavesPickerProps {
    isDisabled: boolean;
    isReportSettings?: boolean;
    questionTypeLookup: {[key: string]: MainQuestionType};
    waveConfig: ReportWaveConfiguration | undefined;
    updateWaves(waves: ReportWaveConfiguration | undefined): void;
    showCreateVariableButton?: boolean | undefined;
    selectedPart?: string;
}

function wavesOptionToString(wavesOption: ReportWavesOptions | undefined) {
    switch (wavesOption) {
        case ReportWavesOptions.AllWaves: return "All waves";
        case ReportWavesOptions.MostRecentNWaves: return "Recent waves";
        case ReportWavesOptions.SelectedWaves: return "Selected waves";
    }
    return "Pick which waves to show";
}

const ReportWavesPicker = (props: IReportWavesPickerProps) => {
    const [isWaveOptionDropdownOpen, setWaveOptionDropownOpen] = useState<boolean>(false);
    const { metricsForReports } = useMetricStateContext();
    const waveMetrics = metricsForReports.filter(m => m.isWaveMeasure || m.isSurveyIdMeasure);
    const selectedMetric = props.waveConfig?.waves ? metricsForReports.find(m => m.name == props.waveConfig!.waves!.measureName) : undefined;

    const updateWaves = (crossMeasures: CrossMeasure[]) => {
        if (crossMeasures.length == 0) {
            props.updateWaves(undefined);
        } else {
            const newWaves = crossMeasures[0];
            if (props.waveConfig == null) {
                props.updateWaves(new ReportWaveConfiguration({
                    wavesToShow: ReportWavesOptions.SelectedWaves,
                    numberOfRecentWaves: 3,
                    waves: newWaves
                }));
            } else {
                props.updateWaves(new ReportWaveConfiguration({
                    ...props.waveConfig,
                    waves: newWaves
                }));
            }
        }
    }

    const updateWavesToShow = (wavesToShow: ReportWavesOptions) => {
        if (props.waveConfig) {
            props.updateWaves(new ReportWaveConfiguration({
                ...props.waveConfig,
                wavesToShow: wavesToShow
            }));
        }
    }

    const updateNumberOfRecentWaves = (numberOfWaves: number) => {
        if (props.waveConfig) {
            props.updateWaves(new ReportWaveConfiguration({
                ...props.waveConfig,
                numberOfRecentWaves: numberOfWaves
            }));
        }
    }

    const getMetricDropdown = () => {
        return (
            <ReportCrossMeasurePicker
                metricsForBreaks={waveMetrics}
                selectedCrossMeasure={props.waveConfig?.waves}
                setCrossMeasures={updateWaves}
                disabled={props.isDisabled}
                selectNoneText={"No waves"}
                selectedPart={props.selectedPart}
                showCreateVariableButton={props.showCreateVariableButton}
                forceablySelectTwo={true}
                reportVariableAppendType={ReportVariableAppendType.Waves}
            />
        );
    }

    const getWaveOptionDropdown = () => {
        const options = [ReportWavesOptions.AllWaves, ReportWavesOptions.MostRecentNWaves, ReportWavesOptions.SelectedWaves];
        if (props.waveConfig?.waves) {
            return (
                <ButtonDropdown isOpen={isWaveOptionDropdownOpen} toggle={() => setWaveOptionDropownOpen(!isWaveOptionDropdownOpen)} className="wave-option-dropdown">
                    <DropdownToggle className="toggle-button" disabled={props.isDisabled}>
                        <span>{wavesOptionToString(props.waveConfig?.wavesToShow)}</span>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        {options.map(wavesOption =>
                            <DropdownItem onClick={() => updateWavesToShow(wavesOption)} key={wavesOption}>{wavesOptionToString(wavesOption)}</DropdownItem>
                        )}
                    </DropdownMenu>
                </ButtonDropdown>
            );
        }
    }

    const getConfigurationSection = () => {
        if (!props.waveConfig?.waves || !selectedMetric) {
            return <NoWavesMessage isDisabled={props.isDisabled} isReportSettings={props.isReportSettings} />
        } else if (props.waveConfig.wavesToShow === ReportWavesOptions.AllWaves) {
            return <div className='hint'>Charts will update automatically when new waves are added</div>
        } else if (props.waveConfig.wavesToShow === ReportWavesOptions.MostRecentNWaves) {
            return (
                <>
                    <div className='recent-wave-number-input'>
                        Compare latest
                        <input type="number"
                            className="range-input"
                            autoComplete="off"
                            min="1"
                            step="1"
                            value={props.waveConfig.numberOfRecentWaves}
                            onChange={(e) => updateNumberOfRecentWaves(+e.target.value)}
                            disabled={props.isDisabled}
                        />
                        waves
                    </div>
                    <div className='hint'>Charts will update automatically when new waves are added</div>
                </>
            )
        } else if (props.waveConfig.wavesToShow === ReportWavesOptions.SelectedWaves) {
            return <CrossMeasureInstanceSelector
                selectedCrossMeasure={props.waveConfig.waves}
                selectedMetric={selectedMetric}
                activeEntityType={undefined}
                setCrossMeasures={updateWaves}
                disabled={props.isDisabled}
                forceablySelectTwo={true}
                includeSelectAll={true}
            />
        }
    }

    return (
        <div className="chart-break-container">
            {getMetricDropdown()}
            {getWaveOptionDropdown()}
            {getConfigurationSection()}
        </div>
    );
}

export default ReportWavesPicker;