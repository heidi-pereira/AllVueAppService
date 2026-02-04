import React from 'react';
import _ from 'lodash';
import { VariableConfigurationModel, SqlRoundingType, QuestionVariableDefinition } from '../../BrandVueApi';
import { dropdownOption, readOnlyOption } from '../HtmlConfigurationPages/ConfigurationInputOptions';

interface IQuestionVariableDefinitionConfigurationPaneProps {
    selectedVariable: VariableConfigurationModel | undefined;
    editingVariable: VariableConfigurationModel | undefined;
    saveVariable(): void;
    setEditingVariable(variable: VariableConfigurationModel): void;
}

const QuestionVariableDefinitionConfigurationPane = (props: IQuestionVariableDefinitionConfigurationPaneProps) => {
    const { editingVariable, selectedVariable } = props;

    const sqlRoundingTypes = [SqlRoundingType.Round, SqlRoundingType.Ceiling, SqlRoundingType.Floor];

    const editVariableDefinition = (updateDefinition: (variable: VariableConfigurationModel) => void) => {
        if (editingVariable) {
            const newVariable = new VariableConfigurationModel({ ...editingVariable });
            updateDefinition(newVariable);
            props.setEditingVariable(newVariable);
        }
    }

    const getConfigurationPane = () => {
        if (!props.editingVariable) {
            return (
                <div className="nothing-selected-message">
                    Select a question variable definition to update.
                </div>
            );
        }
        const editingVariableDefinition = new QuestionVariableDefinition({ ...props.editingVariable?.definition as QuestionVariableDefinition });
        return (
            <div id='configure-variable' className='configure-variable'>
                {readOnlyOption("Question varcode", editingVariableDefinition.questionVarCode) }
                {dropdownOption<SqlRoundingType>(
                    editingVariableDefinition.roundingType,
                    sqlRoundingTypes,
                    false,
                    "Sql rounding type",
                    s => s.toString(),
                    rounding => {
                        editingVariableDefinition.roundingType = rounding;
                        editVariableDefinition(v => v.definition = editingVariableDefinition);
                    }
                )}
            </div>
        );
    }

    const getConfigurationButtons = () => {
        const canSave = !_.isEqual(selectedVariable, editingVariable);
        if (editingVariable) {
            return <div className='configuration-buttons'>
                <button className='hollow-button' onClick={props.saveVariable} disabled={!canSave}>
                    Save updated variable definition
                </button>
            </div>
        }
    }

    return (
        <div className='configuration-area'>
            <div className='configuration-title'>
                Configure question variable definitions
            </div>
            <div className='configuration-sub-title'>
                ⚠️ Note: These changes won't take effect until the dashboard is fully restarted
            </div>
            {getConfigurationPane()}
            {getConfigurationButtons()}
        </div>
    );
}

export default QuestionVariableDefinitionConfigurationPane;