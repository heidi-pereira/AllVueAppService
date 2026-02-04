import React from "react";
import { CuratedFilters } from "../../filter/CuratedFilters";
import Wordle from "./Wordle/Wordle";
import { Metric } from "../../metrics/metric";
import { EntityInstance } from "../../entity/EntityInstance";
import TileTemplate from "./shared/TileTemplate";
import { useSize } from "../../helpers/DOMSizeHelper";

interface IProps {
    openAssociationMetric: Metric;
    openAssociationPageUrl: string;
    activeBrand: EntityInstance | undefined;
    filters: CuratedFilters;
}

const OpenAssociationsCard = ({ openAssociationMetric, openAssociationPageUrl, activeBrand, filters }: IProps) => {

    if (!activeBrand) {
        throw new Error("You must specify a focus brand");
    }

    const wordCloudRef = React.useRef<HTMLDivElement>(null);
    const wordCloudContainerSize = useSize(wordCloudRef);

    return (
        <TileTemplate
            description={`Unprompted associations for ${activeBrand.name}`}
            nextPageUrl={openAssociationPageUrl}
            linkText="Discover more associations"
        >
            <div ref={wordCloudRef} style={{ minHeight: "276px" }}>
                <Wordle
                    activeBrand={activeBrand}
                    filters={filters}
                    metrics={[openAssociationMetric]}
                    size={wordCloudContainerSize}
                />
            </div>
        </TileTemplate>
    );
}

export default OpenAssociationsCard;