import React from "react";
import { useState } from "react";
import * as BrandVueApi from "../BrandVueApi";
import { SwaggerException, Subset, Factory } from "../BrandVueApi";
import Footer from "../components/Footer";
import AceEditor from "react-ace";
import { IGoogleTagManager } from "../googleTagManager";
import { DataSubsetManager } from "../DataSubsetManager";
import { MultipleSelectPicker } from "../components/SelectPicker";
import EntityInstance = BrandVueApi.EntityInstance;
import Throbber from "../components/throbber/Throbber";
import { Input, Label } from "reactstrap";

interface IWeightingProps {
    nav: React.ReactNode;
}

interface IMultipleSubsetSelectorProps {
    id: string;
    onChange: (subsets: Subset[] | null) => void;
    activeValue: Subset[];
    optionValues: Subset[];
    className?: string;
    title?: string;
    disabled?: boolean;
    placeholder?: string;
}

class MultipleSubsetSelectPicker extends MultipleSelectPicker<Subset> { }

export const MultipleEntityInstanceSelector = (props: IMultipleSubsetSelectorProps) => {
    return (
        <MultipleSubsetSelectPicker onChange={props.onChange}
                                            id={props.id}
                                            activeValue={props.activeValue}
                                            optionValues={props.optionValues}
                                            className={props.className}
                                            title={props.title}
                                            disabled={props.disabled}
                                            placeholder={props.placeholder}
                                            getValue={(s) => `${s.id}`}
                                            getLabel={(s) => s.displayName} />
    );
}

export const WeightingGeneration: React.FunctionComponent<IWeightingProps> = ((props: IWeightingProps) => {

    const weightingsConfigClient = BrandVueApi.Factory.WeightingAlgorithmsClient(error => error());
    const [generatedWeights, setGeneratedWeights] = React.useState<string>();
    const [generatedWarnings, setGeneratedWarnings] = React.useState<string[]>([]);
    const [generatedErrors, setGeneratedErrors] = React.useState<string[]>([]);
    const [isCurrentlyGenerating, setIsCurrentlyGenerating] = React.useState(false);
    const [userSelectedSubsets, setUserSelectedSubsets] = React.useState<Subset[]>([]);
    const [allAvailableSubsets, setAllAvailableSubsets] = React.useState<Subset[]>([]);
    const [file, setFile] = React.useState<File>()
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [isModifyWeightingDirectly, setIsModifyWeightingDirectly] = useState<boolean>(false);

    const reloadDataSubsetManagerSubsets = () => {
        setIsLoading(true);

        const api = Factory.MetaDataClient(error => error());
        return api.getSubsets().then(subsets => {
            DataSubsetManager.Initialize(subsets, "");
            subsets = DataSubsetManager.getAll();
            setUserSelectedSubsets(subsets.filter(s => !s.disabled));
            setAllAvailableSubsets (subsets);
            setIsLoading(false);
        });
    }
    React.useEffect(() => {
        reloadDataSubsetManagerSubsets();
    }, []);

    function clearDownResults() {
        setGeneratedWarnings([]);
        setGeneratedErrors([]);
        setGeneratedWeights(undefined);

    }
    function handleChange(event) {
        clearDownResults();
        setFile(event.target.files[0])
    }
    function RemoveIds(plan: any) {
        delete plan.id;

        for (const target of plan.uiChildTargets) {
            delete target.id; // Displaying id is not needed - id's are changed on POST anyway.
            for (const childPlan of target.uiChildPlans) {
                RemoveIds(childPlan);
            }
        }
    }


    const handleSubmit = () => {
        if (file && userSelectedSubsets) {
            clearDownResults();
            setIsCurrentlyGenerating(true);
            return weightingsConfigClient
                .generateTargetWeights({ data: file, fileName: file.name }, userSelectedSubsets.map(s => s.id), isModifyWeightingDirectly)
                .then((generated) => {
                    const weightings = generated.plans;
                    const replacer = (key, value) => typeof value === 'undefined' ? null : value;

                    const unneededPropertiesRemoved = weightings.map(w => {
                        const json = w.toJSON();
                        for (const plan of json.uiWeightingPlans) {
                            RemoveIds(plan);
                        }
                        return json;
                    });

                    setGeneratedWarnings(generated.warnings);
                    setGeneratedErrors(generated.errors);

                    const weightingString = JSON.stringify(unneededPropertiesRemoved, replacer, 2);
                    setGeneratedWeights(weightingString);
                    if (weightingString == "[]") {
                        setGeneratedWeights(undefined);
                    }
                    
                    setIsCurrentlyGenerating(false);
                }).catch((e: SwaggerException) => {
                    const defaultErrorMessage = "An error occurred trying to create target weighting";
                    const response = e.response ? JSON.parse(e.response) : null;;
                    const extraErrorContext: string = response ? response.error ? response.error.message : response.message : null;
                    setGeneratedWeights(undefined);
                    setGeneratedErrors([extraErrorContext ?? defaultErrorMessage]);
                    setIsCurrentlyGenerating(false);
                    throw e;
                });
        }
    };

    if (isLoading) {
        return <Throbber />;
    }

    return <div className="configuration-page">
               {props.nav}
               <div className="weighting-generation-content">
                   <p>You must set up the RIM scheme that correctly classifies respondents into cells before using this form.</p>
                   <form className="weighting-generation-form" onSubmit={e => {
                           e.preventDefault();
                           handleSubmit();
                       }}>
                <div className="singleLine">
                       <label htmlFor="subset-chooser">Select segments to generate weights for:</label>
                       <MultipleEntityInstanceSelector
                    id="subset-chooser"
                    optionValues={allAvailableSubsets}
                    activeValue={userSelectedSubsets}
                    onChange={s => {
                        setUserSelectedSubsets(s || []);
                        clearDownResults();
                        }} />
                </div>
                <div className="singleLine" >
                    <input id="file-chooser" type="file" onChange={handleChange} accept=".csv" title="Select file with &quot;ResponseId&quot; and &quot;Weight&quot; column headers" />
                </div>
                <div className="singleLine">
                    <Input id="direct-post-into-database" className="postIntoDatabase" type="checkbox" checked={isModifyWeightingDirectly} onChange={() => { setIsModifyWeightingDirectly(!isModifyWeightingDirectly) }} />
                    <Label htmlFor="direct-post-into-database">Post directly into database</Label>
                </div>
                       <button className="primary-button" type="submit" disabled={isCurrentlyGenerating}>{isCurrentlyGenerating ? "Generating..." : "Generate target weights"}</button>
            </form>
            {((generatedErrors.length + generatedWarnings.length) > 0) &&
                <div className="wanings_and_errors">
                    {generatedErrors.length > 0 && generatedErrors.map(e => <p className="error">{e}</p>)}

                    {generatedWarnings.length > 0 && generatedWarnings.map(e => <p className="warning">{e}</p>)}
                </div>
            }
                {generatedWeights &&
                       <div className="json-output"><AceEditor
                           height="100%"
                           width="100%"
                           mode="json"
                           theme="monokai"
                           name="ace-editor"
                           value={generatedWeights}
                           readOnly={true}
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
                               }}/></div>
                   }
               </div>
               <Footer/>
           </div>;
});
