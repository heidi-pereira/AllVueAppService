import React from "react";
import TileTemplate from "../shared/TileTemplate";
import {PartWithExtraData} from "../Reports/ReportsPageDisplay";

export const NothingSelected = () =>{
    return (
        <div className={"no-answers-text"}>
            No answers selected
        </div>
    );
}

interface  INothingSelectedCardTemplate {
    descriptionNode?: React.ReactNode;
}

export const NothingSelectedCard = (props: INothingSelectedCardTemplate) => {
    return (
        <TileTemplate descriptionNode={props.descriptionNode}>
            <NothingSelected/>
        </TileTemplate>
    );
}

export const hasNoAnswersSelected = (reportPart: PartWithExtraData | undefined) => {
    return reportPart?.part.selectedEntityInstances?.selectedInstances.length !== undefined && 
        reportPart.part.selectedEntityInstances?.selectedInstances.length === 0;
}