import React from 'react';
import { useEffect, useState } from 'react';
import toast, { Toaster } from 'react-hot-toast';
import { Factory, SwaggerException, VariableConfigurationModel, QuestionVariableDefinition } from '../../BrandVueApi';
import Footer from '../../components/Footer';
import Throbber from '../../components/throbber/Throbber';
import { TabContent, TabPane, Nav, NavItem, NavLink } from 'reactstrap';
import SearchInput from '../../components/SearchInput';
import QuestionVariableDefinitionConfigurationPane from './QuestionVariableDefinitionConfigurationPane';

interface IQuestionVariableDefinitionConfigurationPageProps {
    nav: React.ReactNode;
}

function orderVariables(variables: VariableConfigurationModel[]): VariableConfigurationModel[] {
    return variables.sort((a, b) => a.displayName.localeCompare(b.displayName));
}

function filterVariable(variable: VariableConfigurationModel, searchText: string) {
    const loweredText = searchText.toLowerCase();
    return variable.displayName.toLowerCase().includes(loweredText)
}

const QuestionVariableDefinitionConfigurationPage = (props: IQuestionVariableDefinitionConfigurationPageProps) => {
    const variableConfigClient = Factory.VariableConfigurationClient(() => { });
    const [variables, setVariables] = useState<VariableConfigurationModel[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [selectedVariable, setSelectedVariable] = useState<VariableConfigurationModel | undefined>();
    const [editingVariable, setEditingVariable] = useState<VariableConfigurationModel | undefined>();
    const [searchText, setSearchText] = useState<string>("");

    const selectVariable = (variable: VariableConfigurationModel) => {
        setSelectedVariable(variable);
        setEditingVariable(new VariableConfigurationModel({ ...variable }));
    }

    useEffect(() => {
        setIsLoading(true);
        variableConfigClient.getQuestionVariableDefinitionVariables()
            .then(variables => {
                const configurations = orderVariables(variables);
                setVariables(configurations);
                if (configurations.length > 0) {
                    selectVariable(configurations[0]);
                }
            })
            .catch(error => toast.error("Couldn't get variable configurations"))
            .finally(() => setIsLoading(false))
    }, []);

    if (isLoading) {
         return (
            <div id="ld" className="loading-container">
                <Throbber />
            </div>
        );
    }

    const saveVariable = () => {
        if (editingVariable) {
            const action = () => variableConfigClient.update(editingVariable.id, editingVariable.displayName, editingVariable.definition, null);
            toast.promise(action(), {
                loading: 'Saving...',
                success: `${editingVariable.displayName} saved`,
                error: err => handleError(err)
            });
        }
    }

    const handleError = (error): string => {
        if (error && SwaggerException.isSwaggerException(error)) {
            const swaggerException = error as SwaggerException;
            const responseJson = JSON.parse(swaggerException.response);
            return responseJson.message;
        }
        return `An error occurred trying to update this variable`;
    }

    const getVariableListElement = (variable: VariableConfigurationModel): JSX.Element => {
        const className = 'variable-list-element' +
            (variable == selectedVariable ? ' selected' : '');
        return (
            <div key={variable.id} className={className} title={variable.displayName} onClick={() => selectVariable(variable)}>
                <div className='variable-name'>{variable.displayName}</div>
            </div>
        );
    }

    return (
        <div id="question-variable-definition-configuration-page">
            {props.nav}
            <Toaster position='bottom-center' toastOptions={{duration: 5000}} />
            <div className="view-variables-configuration">
                <div className='variable-list'>
                    <Nav tabs>
                        <NavItem>
                            <NavLink className={'tab-active'}>
                                Question variable definitions
                            </NavLink>
                        </NavItem>
                    </Nav>
                    <SearchInput id='variable-search' className='flat-search' text={searchText} onChange={text => setSearchText(text)} />
                    <TabContent>
                        <TabPane>
                            {variables.filter(a => filterVariable(a, searchText)).map(getVariableListElement)}
                            {variables.length == 0 &&
                                <div className='no-variables-message'>
                                    No question variable definitions found.
                                </div>
                            }
                        </TabPane>
                    </TabContent>
                </div>
                <QuestionVariableDefinitionConfigurationPane
                    selectedVariable={selectedVariable}
                    editingVariable={editingVariable}
                    saveVariable={saveVariable}
                    setEditingVariable={setEditingVariable}
                />
            </div>
            <Footer />
        </div>
    );
}

export default QuestionVariableDefinitionConfigurationPage;