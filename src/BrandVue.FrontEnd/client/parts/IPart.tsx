import { IPartDescriptor, PartDescriptor } from "../BrandVueApi";
import { IDashPartProps } from "../components/DashBoard";
import { BasePart } from "./BasePart";
import { AnalysisScorecardPart } from "./AnalysisScorecardPart";
import { BrandAnalysisScorecardPart } from "./BrandAnalysisScorecardPart";
import { TextPart } from "./TextPart";
import { ICardProps } from "../components/panes/Card";
import { CategoryComparisonPart } from "./CategoryComparisonPart";
import { StackedChartPart } from "./StackedChartPart";
import { BrandAnalysisScoreOverTimePart } from "./BrandAnalysisScoreOverTimePart";
import { BrandAnalysisWhereNextPart } from "./BrandAnalysisWhereNextPart";
import { BrandAnalysisPotentialScorePart } from "./BrandAnalysisPotentialScorePart";
import { ErrorTestPart } from "./ErrorTestPart";
import { PartType } from "../components/panes/PartType";
import { Location } from "react-router-dom";
import { IReadVueQueryParams } from "../components/helpers/UrlHelper";
import { ReactElement } from "react";

export interface IPart {
    getPartComponent(props: IDashPartProps): ReactElement|null;
    getCardComponent(props: ICardProps, location: Location, readVueQueryParams: IReadVueQueryParams): ReactElement|null;
    descriptor: IPartDescriptor;
}

export const getTypedPart = (part: IPartDescriptor) : IPart => {
    switch (part.partType) {
        case "AnalysisScorecard":
            return new AnalysisScorecardPart(part);
        case "BrandAnalysisScorecard":
            return new BrandAnalysisScorecardPart(part);
        case "BrandAnalysisScoreOverTime":
            return new BrandAnalysisScoreOverTimePart(part);
        case "BrandAnalysisWhereNext":
            return new BrandAnalysisWhereNextPart(part);
        case PartType.CategoryComparison:
            return new CategoryComparisonPart(part);
        case "StackedChart":
            return new StackedChartPart(part);
        case "BrandAnalysisPotentialScore":
            return new BrandAnalysisPotentialScorePart(part);
        case "Text":
            return new TextPart(part);
        case PartType.ErrorTest:
            return new ErrorTestPart(part);
        default:
            return new BasePart(part);
    }
}