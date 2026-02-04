import Throbber from '../../../../throbber/Throbber';
import ReportOverTimeSettingsPicker from '../../Components/ReportOverTimeSettingsPicker';
import ReportWavesPicker from '../../Components/ReportWavesPicker';
import { ApplicationConfiguration } from '../../../../../ApplicationConfiguration';
import { MainQuestionType, ReportOverTimeConfiguration, ReportWaveConfiguration } from '../../../../../BrandVueApi';

export interface IOverTimeStepProps {
    numberOfPages: number;
    isOverTimeFeatureEnabled: boolean;
    isCreatingReport: boolean;
    applicationConfiguration: ApplicationConfiguration;
    overTimeConfig: ReportOverTimeConfiguration | undefined;
    setOverTimeConfig: (cfg: ReportOverTimeConfiguration | undefined) => void;
    waves: ReportWaveConfiguration | undefined;
    setWaves: (w: ReportWaveConfiguration | undefined) => void;
    questionTypeLookup: { [key: string]: MainQuestionType };
    subsetId: string;
    isDataWeighted: boolean;
    onBack: () => void;
    onCreate: () => void;
};

const OverTimeStep = (props: IOverTimeStepProps) => {
    return (
        <>
            <div className="details">3 of {props.numberOfPages}: {props.isOverTimeFeatureEnabled ? 'Over time data' : 'Waves to compare'}</div>
            <div className="content-and-buttons">
                {!props.isCreatingReport && (
                    <>
                        <div className='content'>
                            {props.isOverTimeFeatureEnabled && (
                                <div className='bordered-section'>
                                    <label className="report-label">Time series</label>
                                    <ReportOverTimeSettingsPicker
                                        applicationConfiguration={props.applicationConfiguration}
                                        config={props.overTimeConfig}
                                        setConfig={props.setOverTimeConfig}
                                        disabled={props.waves != undefined}
                                        unsavedSubsetId={props.subsetId}
                                        isDataWeighted={props.isDataWeighted}
                                    />
                                </div>
                            )}
                            <div className='waves-section bordered-section'>
                                <label className="report-label">Waves</label>
                                <div className="new-report-waves">
                                    <ReportWavesPicker
                                        isDisabled={props.isOverTimeFeatureEnabled && props.overTimeConfig != undefined}
                                        isReportSettings
                                        questionTypeLookup={props.questionTypeLookup}
                                        waveConfig={props.waves}
                                        updateWaves={props.setWaves}
                                        showCreateVariableButton={false}
                                    />
                                </div>
                            </div>
                        </div>
                        <div className="modal-buttons">
                            <button className="modal-button secondary-button" onClick={props.onBack}>Back</button>
                            <button className="modal-button primary-button" onClick={props.onCreate}>Create report</button>
                        </div>
                    </>
                )}
                {props.isCreatingReport && (
                    <div className="throbber-container-fixed">
                        <Throbber />
                    </div>
                )}
            </div>
        </>
    )
};

export default OverTimeStep;