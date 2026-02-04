import React from "react";
import { VariableSampleResult } from "../../../../../BrandVueApi";
import EstimatedResultBarMultiEntity from "./EstimatedResultBarMultiEntity";
import EstimatedResultBarSingleEntity from "./EstimatedResultBarSingleEntity";

interface IEstimatedResultBarProps {
    sample: VariableSampleResult[] | undefined;
    forFieldExpression: boolean;
}

const EstimatedResultBar = (props: IEstimatedResultBarProps) => {
    if(!props.sample) {
        return <></>
    }

    const isMultiEntity = (props.sample && props.sample.length > 1) ?? false;
    const samplePreviewHelptext = "The sample shown here is for all respondents.";
    const samplePreviewHelptextMulti = "The sample shown here is for all respondents for the first option of this question.";

    return isMultiEntity 
         ? <EstimatedResultBarMultiEntity sample={props.sample} helpText={props.forFieldExpression ? samplePreviewHelptextMulti : undefined}/>
         : <EstimatedResultBarSingleEntity sample={props.sample[0]} includeLabel={true} helptext={props.forFieldExpression ? samplePreviewHelptext : undefined}/>
}

export default EstimatedResultBar;