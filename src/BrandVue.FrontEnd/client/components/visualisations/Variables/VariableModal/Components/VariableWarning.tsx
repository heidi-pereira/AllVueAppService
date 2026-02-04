import React from "react";
import {ModalContent} from "../VariableContentModal";
import { ObjectThatReferencesVariable, VariableWarningModel} from "../../../../../BrandVueApi";

interface IVariableWarningProps {
    content: ModalContent;
    variableWarnings: VariableWarningModel[] | undefined;
    variableIdToView?: number;
}

const VariableWarning = (props: IVariableWarningProps) => {
    const localStorageKeyForVariableWarning: string = "variableWarningDialogState";

    const getDefaultStateoOfMinimizeMaximizeButton = () : string => {
        let state = window.localStorage.getItem(localStorageKeyForVariableWarning);
        if (!state) {
            state = "minimized";
        }
        return state;
    }

    const showWarning = props.variableIdToView !== undefined;
    const [isMinimized, setIsMinimized] = React.useState<string>(getDefaultStateoOfMinimizeMaximizeButton);

    const setMinimizedButton = (state:string) => {
        window.localStorage.setItem(localStorageKeyForVariableWarning, state);
        setIsMinimized(state);
    }

    const minimizedDetails = () => {
        const total = props.variableWarnings?.map(x => x.names.length).reduce((partialSum, a) => partialSum + a, 0);
        const message = total == 1 ? `This variable is used by a reports or other variable` : `This variable is used by ${total} reports and/or other variables`;
        return (
            <div className="warning-container">
                <div className="top">
                    <i className="material-symbols-outlined help_icon">info</i>
                    <span>{message}</span>
                    <button onClick={() => setMinimizedButton('expanded')} className="warning_open_close_button">
                        <i className="material-symbols-outlined">expand_less</i>
                    </button>
                </div>
            </div>);
    };

    const getDetailsText = (reference: ObjectThatReferencesVariable) => {
        switch(reference) {
            case ObjectThatReferencesVariable.Report: {
                return `This variable is used as a chart or table by the following reports`;
            }
            case ObjectThatReferencesVariable.Weighting: {
                return `This variable is used for weighting in the following subsets`;
            }
            case ObjectThatReferencesVariable.Variable: {
                return `This variable is referenced by the following variables`;
            }
            case ObjectThatReferencesVariable.Filter: {
                return `This variable is being used as a filter in the following reports`;
            }
            case ObjectThatReferencesVariable.Wave: {
                return `This variable is being used as a wave in the following reports`;
            }
            case ObjectThatReferencesVariable.Break: {
                return `This variable is being used as a break in the following reports`;
            }
            case ObjectThatReferencesVariable.Base: {
                return `This variable is being used as a base in the following reports`;
            }
            default: {
                return `This variable is being used in the following reports`;
            }
        }
    }

    const fullDetails = () => {
        return (
            <div className="warning-container">
                {props.variableWarnings!.map((warningVariableModel, i) => {
                    return (
                        <div className="variable-modal-warning">
                            <div className="top">
                                <i className="material-symbols-outlined help_icon">info</i>
                                <span>
                                    {getDetailsText(warningVariableModel.objectThatReferencesVariable)}
                                </span>
                                {i == 0 &&
                                    <button onClick={() => setMinimizedButton('minimized')} className="warning_open_close_button">
                                        <i className="material-symbols-outlined">expand_more</i>
                                    </button>
                                }
                            </div>
                            <ul className="bullets">
                                {warningVariableModel.names.map(n => {
                                    return <li className="bullet">{n}</li>
                                })}
                            </ul>
                            <div className="bottom">Any changes you make will be applied everywhere it is used</div>
                        </div>
                    )
                })}
            </div>
        )
    };


    if (showWarning && props.variableWarnings && props.variableWarnings.length > 0) {
        if (isMinimized == 'minimized') {
            return minimizedDetails();
        }
        return fullDetails();

    }

    return <></>;
}

export default VariableWarning