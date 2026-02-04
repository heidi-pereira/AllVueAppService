import React from "react";
import { Metric } from "../../metrics/metric";
import { CuratedFilters } from "../../filter/CuratedFilters";
import * as BrandVueApi from "../../BrandVueApi";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { EntitySet } from "../../entity/EntitySet";
import _ from 'lodash';
import { EntityInstance } from "../../entity/EntityInstance";
import { CategoryComparisonPlaceholder } from "../throbber/CategoryComparisonPlaceholder";
import TileTemplate from "./shared/TileTemplate";
import { Link } from "react-router-dom";
import CategoryTile from "./CategoryTile";
import CategoryTileDetailed from "./CategoryTileDetailed";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import { BaseVariableContext } from "./Variables/BaseVariableContext";
import { CategoryResult, CategorySortKey } from "../../BrandVueApi";
import { StringHelper } from "../../helpers/StringHelper";
import { CategoryContext } from "../helpers/CategoryContext";
import { useAppSelector } from "../../state/store";
import { selectSubsetId } from "client/state/subsetSlice";
import { selectTimeSelection } from "client/state/timeSelectionStateSelectors";
import { ITimeSelectionOptions } from "../../state/ITimeSelectionOptions";

interface ICategoryComparisonProps {
    isDetailedTile : boolean;
    linkUrl: string | undefined;
    entityConfiguration: IEntityConfiguration;
    activeBrand: EntityInstance;
    metrics: Metric[];
    curatedFilters: CuratedFilters;
    brandSet: EntitySet;
    colorName: string;
    questionText: string | undefined;
    baseVariableId1: number;
    baseVariableId2: number;
    title: string;
    paneIndex: number;
    defaultBaseVariableIdentifier: string | undefined;
    updateBaseVariableNames: (firstName: string | undefined, secondName: string | undefined) => void;
}

export class CategoryDisplayResult {
    public readonly displayName: string;
    public readonly firstBase: string;
    public readonly secondBase: string;
    public firstBaseValue: number;
    public secondBaseValue: number;

    constructor(displayName: string, firstBase: string, secondBase: string) {
        
        this.displayName = displayName;
        this.firstBase = firstBase;
        this.secondBase = secondBase;
    }
}

type SortKeySelector = (result: CategoryDisplayResult) => number;

const _sortingFunctions: Map<CategorySortKey, SortKeySelector> = new Map<CategorySortKey, SortKeySelector>([
    [CategorySortKey.BestScores, r => r.firstBaseValue],
    [CategorySortKey.WorstScores, r => -r.firstBaseValue],
    [CategorySortKey.OverPerforming, r => r.firstBaseValue - r.secondBaseValue],
    [CategorySortKey.UnderPerforming, r => r.secondBaseValue - r.firstBaseValue]
]);

const getTooltip = (result: CategoryDisplayResult) => {
    return <div className="brandvue-tooltip">
        <div className="tooltip-header">{result.displayName}</div>
        <div className="tooltip-label">{StringHelper.formatBaseVariableName(result.firstBase)}:</div><div className="tooltip-value">{NumberFormattingHelper.formatPercentage1Dp(result.firstBaseValue)}</div>
        <div className="tooltip-label">{StringHelper.formatBaseVariableName(result.secondBase)}:</div><div className="tooltip-value">{NumberFormattingHelper.formatPercentage1Dp(result.secondBaseValue)}</div>
    </div>;
}

const createMultiEntityProfileModel = (curatedFilters: CuratedFilters,
    metrics: Metric[],
    brandSet: EntitySet,
    activeEntityId: number,
    bases: number[],
    includeMarketAverage: boolean,
    subsetId: string, timeSelection: ITimeSelectionOptions) => {
    const splitByEntityInstanceIds = brandSet.getInstances().getAll().map(b => b.id);
    if (brandSet.type.isBrand &&
        brandSet.mainInstance != null &&
        !splitByEntityInstanceIds.includes(brandSet.mainInstance.id)) {
        splitByEntityInstanceIds.push(brandSet.mainInstance.id);
    }

    return new BrandVueApi.MultiEntityProfileModel({
        subsetId: subsetId,
        period: new BrandVueApi.Period({
            average: curatedFilters.average.averageId,
            comparisonDates: curatedFilters.comparisonDates(false, timeSelection, false, curatedFilters.comparisonPeriodSelection),
        }),
        dataRequest: new BrandVueApi.EntityInstanceRequest({
            type: brandSet.type.identifier,
            entityInstanceIds: splitByEntityInstanceIds
        }),
        activeEntityId: activeEntityId,
        measureNames: metrics.map(m => m.name),
        overriddenBaseVariableIds: bases,
        includeMarketAverage: includeMarketAverage
    });
}

export const transformResultsForDisplay = (categoryResults: CategoryResult[], baseName1: string | undefined, baseName2: string | undefined, baseVariableId1: number | undefined, activeBrandName: string): CategoryDisplayResult[] => {
    let displayResults = new Array<CategoryDisplayResult>();
    categoryResults.forEach(r => {
        const displayName = r.entityInstanceName
            ? `${r.measureName}: ${r.entityInstanceName}`
            : r.measureName;

        const existingResult = displayResults.find(c => c.displayName == displayName);
        if (existingResult == null) {
            const newResult = new CategoryDisplayResult(displayName, baseName1 ?? activeBrandName, baseName2 ?? "Average");
            populateCategoryDisplayResult(newResult, r, baseName1, baseName2, baseVariableId1);
            displayResults.push(newResult);
        } else {
            populateCategoryDisplayResult(existingResult, r, baseName1, baseName2, baseVariableId1);
        }
    })
    return displayResults;
}

const populateCategoryDisplayResult = (displayResult: CategoryDisplayResult, result: CategoryResult, baseName1: string | undefined, baseName2: string | undefined, baseVariableId1: number | undefined): CategoryDisplayResult => {
    // No overridden bases setin the dropdown
    if (!baseName1 && !baseName2) {
        displayResult.firstBaseValue = result.result;
        displayResult.secondBaseValue = result.averageValue ?? -1;
    } // First base set, second unset - second value is average
    else if (baseName1 && !baseName2) {
        displayResult.firstBaseValue = result.result;
        displayResult.secondBaseValue = result.averageValue ?? -1;
    } // Both bases set
    else {
        if (baseVariableId1 == result.baseVariableConfigurationId) {
            displayResult.firstBaseValue = result.result;
        } else {
            displayResult.secondBaseValue = result.result;
        }
    }
    return displayResult;
}

const CategoryComparison = (props: ICategoryComparisonProps) => {
    const [isLoading, setIsLoading] = React.useState(true);
    const [results, setResults] = React.useState([new CategoryDisplayResult("", "", "")]);
    const [baseName1, setBaseName1] = React.useState<string | undefined>(undefined);
    const [baseName2, setBaseName2] = React.useState<string | undefined>(undefined);
    const { baseVariablesLoading, baseVariables } = React.useContext(BaseVariableContext);
    const { clearCategoryExportResultCards } = React.useContext(CategoryContext);
    const categorySortKey = useAppSelector(state => state.entitySelection.categorySortKey);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    React.useEffect(() => {
        clearCategoryExportResultCards();
        if (!baseVariablesLoading) {
            setIsLoading(true);
            const client = BrandVueApi.Factory.DataClient(throwErr => throwErr());

            const baseVariable1 = baseVariables.find(b => b.id == props.baseVariableId1)
                ?? baseVariables.find(b => b.identifier == props.defaultBaseVariableIdentifier);
            const baseVariable2 = baseVariables.find(b => b.id == props.baseVariableId2);
            const baseIds = [baseVariable1?.id, baseVariable2?.id].flatMap(b => b ? [b] : []);
            setBaseName1(baseVariable1?.displayName);
            setBaseName2(baseVariable2?.displayName);
            props.updateBaseVariableNames(baseVariable1?.displayName, baseVariable2?.displayName);

            const includeMarketAverage = baseVariable2 == undefined;

            //Because firing out 6 requests simultaneously is crashing the server, "temporarily" stagger the requests so that they fire every 15 seconds
            setTimeout(() => {
                client.getProfileResultsForMultipleEntities(
                    createMultiEntityProfileModel(props.curatedFilters, props.metrics, props.brandSet, props.activeBrand.id, baseIds, includeMarketAverage, subsetId, timeSelection)
                    )
                .then(clientResults => {
                    setResults(
                        transformResultsForDisplay(clientResults, baseVariable1?.displayName, baseVariable2?.displayName, props.baseVariableId1, props.activeBrand.name)
                        );
                    setIsLoading(false);
                });
            }, props.paneIndex * 15000);

        }
    }, [props.activeBrand.id, JSON.stringify(props.brandSet), props.baseVariableId1, props.baseVariableId2, baseVariablesLoading]);

    const topResults = props.isDetailedTile
        ? _(results).orderBy(_sortingFunctions.get(categorySortKey), "desc").take(10).value()
        : _(results).orderBy(_sortingFunctions.get(categorySortKey), "desc").take(3).value();

    const containsMarketAverage = baseName2 == undefined;

    const tile = props.isDetailedTile
        ? <CategoryTileDetailed
            title={props.title}
            results={results}
            topResults={topResults}
            color={props.colorName}
            activeBrandName={props.activeBrand.name}
            getTooltip={getTooltip}
            questionText={props.questionText}
            containsMarketAverage={containsMarketAverage}
            paneIndex={props.paneIndex}
        />
        : <CategoryTile
            title={props.title}
            averageName={props.curatedFilters.average.displayName}
            results={results}
            topResults={topResults}
            color={props.colorName}
            activeBrandName={props.activeBrand.name}
            getTooltip={getTooltip}
            baseDisplayName1={baseName1}
            baseDisplayName2={baseName2}
            containsMarketAverage={containsMarketAverage}
            paneIndex={props.paneIndex}
        />;

    const renderElement = props.linkUrl
        ? <Link to={props.linkUrl}>{tile}</Link>
        : tile;

    return (
        <TileTemplate>
            {
                isLoading ? <CategoryComparisonPlaceholder /> :  renderElement
            }
        </TileTemplate>
    );
}

export default CategoryComparison