import React from "react";
import {useEffect, useState} from "react";
import {
    BaseFieldExpressionVariableDefinition,
    FieldExpressionVariableDefinition,
    Factory,
    VariableDefinition,
    VariableSampleResult
} from "../../../../../BrandVueApi";
import AceEditor from "react-ace";
import { Ace } from "ace-builds";
import Throbber from "../../../../../components/throbber/Throbber";
import "ace-builds/src-noconflict/mode-python";
import "ace-builds/src-noconflict/theme-crimson_editor";
import "ace-builds/src-noconflict/ext-language_tools";
import { getFieldExpressionCountAndSample } from "../Utils/VariableComponentHelpers";
import EstimatedResultBar from "../Components/EstimatedResultBar";

interface IFieldExpressionVariableModalContentProps {
    isDisabled?: boolean;
    isBase?: boolean;
    errorMessage: string;
    handleError(error: Error): void;
    clearError(): void;
    subsetId: string;
    variableDefinition: FieldExpressionVariableDefinition | BaseFieldExpressionVariableDefinition;
    setVariableDefinition: (variableDefinition: VariableDefinition) => void;
}

const FieldExpressionVariableModalContent = (props: IFieldExpressionVariableModalContentProps) => {
    const [activeFieldExpression, setActiveFieldExpression] = useState<string>(props.variableDefinition.expression);
    const variableConfigClient = Factory.VariableConfigurationClient(error => error());
    const [loading, setLoading] = useState<boolean>(true);
    const [staticWordCompleter, setStaticWordCompleter] = useState<Ace.Completer>();
    const [sample, setSample] = useState<VariableSampleResult[]>([]);
    const baseOrField = props.isBase ? "Base" : "Field"

    const loadCompleter = () => {
        setLoading(true);

        variableConfigClient.getFieldVariables().then(variables => {
            const staticWordCompleter : Ace.Completer = {
                getCompletions (editor, session, pos, prefix, callback) {

                    var curLine : string = session.getDocument().getLine(pos.row);
                    var referencedVariables = variables.filter(f => curLine.includes(f.identifier));

                    const completions: Ace.Completion[] = [];

                    if (referencedVariables.length > 0) {
                        const entityTypes = referencedVariables.flatMap(v => v.responseEntityTypes.map(e => e.identifier));
                        completions.push(...entityTypes.map(type => ({
                            caption: type,
                            value: type,
                            meta: "entity-type",
                            score: 100,
                        })));
                    }

                    completions.push(...variables.map(variable => ({
                        caption: variable.identifier,
                        value: variable.identifier,
                        meta: "variable",
                        score: 0,
                        docText: `Returns a list of values for the respondent, optionally filtered by result entities: e.g. response.${variable.identifier}(${variable.responseEntityTypes.map(e => e.identifier + " = result." + e.identifier).join(", ")})`
                    })));

                    completions.push(...supportedKeywords);

                    callback(null, completions);
                }
            }

            setStaticWordCompleter(staticWordCompleter);
            setLoading(false);
        });
    }

    useEffect(() => {
        loadCompleter();
    }, []);

    useEffect(() => {
        if (props.variableDefinition.expression !== activeFieldExpression){
            const newDefinition = props.isBase ?
                new BaseFieldExpressionVariableDefinition({expression: activeFieldExpression, resultEntityTypeNames: []}) :
                new FieldExpressionVariableDefinition({expression: activeFieldExpression});
            //result entity type names should be set on backend
            props.setVariableDefinition(newDefinition);
        }
    }, [activeFieldExpression])

    useEffect(() => {
        setSample([]);

        let isCancelled = false;
        const debounceTime = 1000;

        setTimeout(() => {
            if (!isCancelled) {
                getFieldExpressionCountAndSample(props.subsetId, props.variableDefinition).then(result => {
                    if (!isCancelled) {
                        if (result.length == 0 || result.some(s => s.hasMultiEntityFilterInstances)) {
                            setSample(result);
                        } else {
                            //remap to a single result to mimic behaviour with grouped variables
                            const count = result.reduce((total, sample) => total += sample.count, 0);
                            const sample = result[0].sample;
                            const remapped = new VariableSampleResult({
                                count: count,
                                sample: sample,
                                splitByEntityInstanceName: "",
                                hasMultiEntityFilterInstances: false
                            });
                            setSample([remapped]);
                        }
                        props.clearError();
                    }
                }).catch(error => props.handleError(error));
            }
        }, debounceTime);

        return () => { isCancelled = true; };
    }, [props.variableDefinition])

    if (loading) {
        return <div className="throbber-container"><Throbber /></div>
    }

    return (
        <div className="field-expression-stage">
            <div className="field-expression-container">
                <div className="text-danger">{props.errorMessage}</div>
                <label className="variable-page-label">{baseOrField} expression</label>
                    <AceEditor
                        className="field-expression-editor"
                        height="100%"
                        width="100%"
                        mode="python"
                        theme="crimson_editor"
                        name="ace-editor"
                        value={activeFieldExpression}
                        onChange={setActiveFieldExpression}
                        fontSize={14}
                        showPrintMargin={false}
                        showGutter={true}
                        highlightActiveLine={false}
                        wrapEnabled={true}
                        readOnly={props.isDisabled}
                        setOptions={{
                            showLineNumbers: true,
                            showFoldWidgets: true,
                            enableBasicAutocompletion: true,
                            enableLiveAutocompletion: true,
                            tabSize: 8,
                        }}
                        onLoad={(editor) => {
                            editor.completers = staticWordCompleter ? [staticWordCompleter] : [];
                        }}
                    />
            </div>
            <EstimatedResultBar sample={sample} forFieldExpression={true} />
        </div>
    );
}

export default FieldExpressionVariableModalContent;

// See https://morar.sharepoint.com/sites/SavantaWiki/SitePages/fieldExpressions.aspx for reference
const supportedKeywords : Ace.Completion[] = [
    {
        value: "True",
        score: 4,
        meta: "keyword",
        docText: "Boolean value representing true (or 1)"
    },
    {
        value: "False",
        score: 4,
        meta: "keyword",
        docText: "Boolean value representing false (or 0)"
    },
    {
        value: "get",
        score: 5,
        meta: "function",
        docText: "Returns the value for a given key from a dictionary"
    },
    {
        value: "len",
        score: 5,
        meta: "function",
        docText: "Returns the length of a list"
    },
    {
        value: "min",
        score: 5,
        meta: "function",
        docText: "Returns the smallest item in a list, e.g. min(something, default=None)"
    },
    {
        value: "max",
        score: 5,
        meta: "function",
        docText: "Returns the largest item in a list, e.g. max(something, default=None)"
    },
    {
        value: "sum",
        score: 5,
        meta: "function",
        docText: "Returns the sum of all values in a list"
    },
    {
        value: "any",
        score: 4,
        meta: "function",
        docText: "Returns True if any item in the list is True"
    },
    {
        value: "count",
        score: 4,
        meta: "function",
        docText: "Returns the number of occurrences of a value in a list"
    },
    {
        value: "if",
        score: 5,
        meta: "keyword",
        docText: "Conditional statement of the form: <ValueIfTrue> if <Condition> else <ValueIfFalse>"
    },
    {
        value: "else",
        score: 5,
        meta: "keyword",
        docText: "Conditional statement of the form: <ValueIfTrue> if <Condition> else <ValueIfFalse>"
    },
    {
        value: "for",
        score: 5,
        meta: "keyword",
        docText: "Used in list comprehensions"
    },
    {
        value: "in",
        score: 5,
        meta: "keyword",
        docText: "Checks if a value is present in a list, also used in list comprehensions"
    },
    {
        value: "response",
        score: 10,
        meta: "object",
        docText: "Contains a function for each variable"
    },
    {
        value: "result",
        score: 10,
        meta: "object",
        docText: "Contains a property for each entity"
    }
]