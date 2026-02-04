import React from "react";
import { ConfigurationElement } from "../pages/ConfigurationList";
import { Modal, ModalFooter, ModalHeader, ModalBody, Button } from "reactstrap";
import AceEditor from "react-ace";

import "ace-builds/src-noconflict/mode-json";
import "ace-builds/src-noconflict/theme-monokai";
import "ace-builds/src-noconflict/ext-language_tools";

export class Completions {
    field: string;
    meta: string;
}

interface IProps {
    configurationObjectTypeName: string;
    configElement?: ConfigurationElement;
    create(configObject: object): Promise<any>;
    update(configElementId: number, configObject: object): Promise<any>;
    delete(configElementId: number): Promise<any>;
    closeJsonConfigurator(boolean): void;
    isCreate: boolean;
    completions: { [property: string]: Completions[] };
    showDowntimeWarning?: boolean;
    deletionWarningMessage?: string;
}

const validJsonMessage = 'JSON is valid';
const emptyJsonObjectString = '{}';

const JsonConfigurator = ((props: IProps) => {
    const [editorContent, setEditorContent] = React.useState(emptyJsonObjectString);
    const [hasError, setHasError] = React.useState(false);
    const [validationMessage, setValidationMessage] = React.useState(validJsonMessage);
    const [showConfirmationDialog, setShowConfirmationDialog] = React.useState(false);
    const [confirmationDialogMessage, setConfirmationDialogMessage] = React.useState<string|undefined>();
    const [operationType, setOperationType] = React.useState('');
    const [operation, setOperation] = React.useState<() => Promise<any>>(() => Promise.resolve(''));

    // Use a ref to store the AceEditor instance
    const editorRef = React.useRef<any>(null);

    const replacer = (key, value) =>
        typeof value === 'undefined' ? null : value;

    React.useEffect(() => {
        const content = props.configElement ? JSON.stringify(props.configElement.configObject, replacer) : emptyJsonObjectString;
        updateEditor(content);
        prettifyContent(content);
    }, [props.configElement]);

    React.useEffect(() => {
        if (editorRef.current) {
            // Reassign completions whenever props.completions changes
            editorRef.current.completers = [staticWordCompleter];
        }
    }, [props.completions]);

    const numberOfIndentationSpaces = 8;

    const prettifyContent = (editorContent: string) => {
        try {
            const newConfigObject = JSON.parse(editorContent);
            const newPrettyContent = JSON.stringify(newConfigObject, replacer, numberOfIndentationSpaces);
            setEditorContent(newPrettyContent);
        } catch (error) {
            console.error(`Unable to prettify json - ${(error as Error).message}`);
        }
    }

    const updateEditor = (editorContent: string)=> {
        try {
            JSON.parse(editorContent);
            setEditorContent(editorContent);
            setValidationMessage(validJsonMessage);
            setHasError(false);
        }
        catch (e) {
            setEditorContent(editorContent);
            setValidationMessage((e as Error).message);
            setHasError(true);
        }
    }

    const saveConfig = () => {
        if (!hasError) {
            const jsonObject = JSON.parse(editorContent);
            var operation;
            if(props.isCreate) {
                setOperationType('create');
                operation = () => () => props.create(jsonObject);
            }
            else {
                setOperationType('update');
                operation = () => () => props.update(props.configElement!.id, jsonObject);
            }
            setOperation(operation);
            setConfirmationDialogMessage("");
            setShowConfirmationDialog(true);
        }
    }

    const deleteConfig = () => {
        setOperationType('delete');
        setOperation(() => () => props.delete(props.configElement!.id));
        setConfirmationDialogMessage(props.deletionWarningMessage);
        setShowConfirmationDialog(true);
    }

    // Completer referencing the current props in each render
    const staticWordCompleter = {
        getCompletions(editor, session, pos, prefix, callback) {
            const line = session.getLine(pos.row);
            // Simple parse for property name before the colon:
            const propertyMatches = line.match(/"([^\d][^"]+)"\s*:/);
            const relevantCompletions = propertyMatches
                ? props.completions[propertyMatches[1]] ?? []
                : Object.values(props.completions).flat();

            let prefixMatchedCompletions = relevantCompletions
                .filter(c => c.field.includes(prefix));

            if (prefixMatchedCompletions.length === 0) {
                prefixMatchedCompletions = relevantCompletions;
            }
            
            callback(null, prefixMatchedCompletions
                .map(c => ({
                    caption: c.field,
                    value: c.field,
                    meta: c.meta
                }))
            );
        }
    };

    const doOperation = async () => {
        setShowConfirmationDialog(false);
        try {
            await operation();
            setHasError(false);
            setValidationMessage("Operation succeeded");
        }
        catch (error: any) {
            setHasError(true);
            let message = "Unknown error occurred";

            if (error.response != undefined) {
                const errorResponse = JSON.parse(error.response);

                // If the response body contains ErrorApiResponse model
                if (errorResponse.message != undefined) {
                    message = errorResponse.message;
                }
            } else if (error.message != undefined) {
                message = error.message;
            }

            setValidationMessage(message);
        }
    }

    return (
        <>
            <Modal isOpen={showConfirmationDialog} toggle={() => setShowConfirmationDialog(false)} centered={true} className="json-config-modal">
                <ModalHeader toggle={() => setShowConfirmationDialog(false)}>Configuration changes confirmation</ModalHeader>
                <ModalBody>
                    <p>Are you sure you want to {operationType}?</p>
                    {confirmationDialogMessage &&
                        <p>{confirmationDialogMessage}</p>
                    }
                    </ModalBody>
                <ModalFooter>
                    {props.showDowntimeWarning && <p className="downtime-warning">If you {operationType} this {props.configurationObjectTypeName}, you may cause up to a minute of downtime. </p>}
                    <Button className="secondary-button" onClick={() => setShowConfirmationDialog(false)}>Close</Button>
                    <Button className="primary-button" onClick={doOperation}>Confirm to {operationType}</Button>
                </ModalFooter>
            </Modal>

            <div className="existing-configurations">
                <div className="configuration-header">
                    <h3>{props.isCreate ? "Create new " + props.configurationObjectTypeName
                        : `Update ${props.configurationObjectTypeName} "${props.configElement && props.configElement.displayName}" (id: ${props.configElement && props.configElement.id})`}</h3>
                    
                    <i className="material-symbols-outlined close-icon" onClick={() => props.closeJsonConfigurator(false)}>close</i>
                </div>
                <div className={`text-${hasError ? "danger" : "success" }`}>{validationMessage}</div>
                <div className="configuration-input form-control">
                    <AceEditor
                        height="100%"
                        width="100%"
                        mode="json"
                        theme="monokai"
                        name="ace-editor"
                        value={editorContent}
                        onChange={updateEditor}
                        fontSize={14}
                        showPrintMargin={false}
                        showGutter={true}
                        highlightActiveLine={false}
                        enableBasicAutocompletion={true}
                        wrapEnabled={true}
                        setOptions={{
                            showLineNumbers: true,
                            showFoldWidgets: true,
                            tabSize: 8,
                        }}
                        onLoad={(editor) => {
                            editorRef.current = editor;
                        }}

                    />
                </div>
                <div className="configuration-footer">
                    {props.configElement?.id ?? 0 > 0 ? <i className="material-symbols-outlined delete-icon" onClick={() => deleteConfig()}>delete</i> : <></>}
                    <button className="primary-button float-end" onClick={() => saveConfig()}>{props.isCreate ? "Create" : "Update"}</button>
                </div>
            </div>
        </>
    );
});

export default JsonConfigurator



