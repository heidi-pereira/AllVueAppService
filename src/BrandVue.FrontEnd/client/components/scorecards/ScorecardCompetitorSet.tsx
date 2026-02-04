import React from "react";
import { EntityInstance } from "../../entity/EntityInstance";
import {IEntityInstanceGroup} from "../../entity/IEntityInstanceGroup";

interface IScorecardCompetitorSetProps { 
    mainInstance: EntityInstance; 
    instanceGroup: IEntityInstanceGroup; 
    title : string }

const ScorecardCompetitorSet = (props: IScorecardCompetitorSetProps) => {
    return (
        <div className="subsection scorecardCompetitors">
            <header>{props.title}</header>
            <div>
                {props.instanceGroup.getAll().map((b, i) =>
                    <span key={b.id} className={b.id === props.mainInstance.id ? "active" : ""}>
                        {b.name + (i < props.instanceGroup.getAll().length - 1 ? ", " : "")}
                    </span>)}
            </div>
        </div>
    );
}
export default ScorecardCompetitorSet;