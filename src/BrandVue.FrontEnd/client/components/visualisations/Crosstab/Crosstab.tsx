import React from "react";
import { useState } from "react";
import { Metric } from "../../../metrics/metric";
import {
    CrosstabCategory, InstanceResult, ICellResult, Significance, CrosstabSignificanceType, ReportOrder,
    BaseExpressionDefinition, CrosstabAverageResults, EntityInstance,
    DisplaySignificanceDifferences} from "../../../BrandVueApi";
import { CrosstabHeader } from "./CrosstabHeader";
import AllVueDescriptionFooter from "../AllVueDescriptionFooter";
import CrosstabCell from "./CrosstabCell";
import AverageCell from "./AverageCell";
import { getAverageDisplayText, isTypeOfMean } from "../AverageHelper";
import { NumberFormattingHelper } from "../../../helpers/NumberFormattingHelper";
import {CuratedCrosstabResult} from "./ResultsCuration";
import CrosstabHeaderCell from "./CrosstabHeaderCell";
import { StatisticType } from "client/components/enums/StatisticType";

interface IProps {
    metric: Metric;
    results: CuratedCrosstabResult;
    includeCounts: boolean;
    highlightLowSample?: boolean;
    highlightSignificance: boolean;
    displaySignificanceDifferences: DisplaySignificanceDifferences;
    significanceType: CrosstabSignificanceType;
    resultSortingOrder: ReportOrder;
    decimalPlaces: number;
    hideEmptyRows: boolean;
    hideEmptyColumns: boolean;
    showTop: number | undefined;
    isUserAdmin: boolean;
    allMetrics: Metric[];
    baseExpressionOverride?: BaseExpressionDefinition;
    isSurveyVue: boolean;
    averageResults: CrosstabAverageResults[];
    displayMeanValues: boolean;
    isDataWeighted: boolean;
    hideTotalColumn: boolean;
    hasBreaksApplied: boolean;
    lowSampleThreshold: number;
    displayStandardDeviation: boolean;
}

interface SampleSizeLookup {
    [key: string]: {
        sampleForCount: number;
        unweightedSampleForCount?: number;
    };
}

function createHeaderRows(categories: CrosstabCategory[]): CrosstabHeader[][] {
    const categoriesAsHeaders = categories.map(c => CrosstabHeader.fromApi(c));
    const maxDepth = Math.max(...categoriesAsHeaders.map(h => h.depth));
    const topLevelHeaders = categoriesAsHeaders.map(h => h.extendToDepth(maxDepth));
    const maxDepthToZero = Array.from({ length: maxDepth + 1 }, (_, i) => i).reverse(); // [maxDepth, ... 2, 1, 0]
    return maxDepthToZero.map(i => topLevelHeaders.reduce<CrosstabHeader[]>((atLevel, h) => atLevel.concat(h.getColumnsAtDepth(i)), []));
}

function getSampleSizeLookup(dataColumns: CrosstabHeader[], results: InstanceResult[]): SampleSizeLookup {
    const resultsArray = Array.from(results.values());
    return dataColumns.reduce((lookup, current) => {
        const values = resultsArray.map(v => v.values[current.id]);
        return {
            ...lookup,
            [current.id]: {
                sampleForCount: values[0]?.sampleForCount,
                unweightedSampleForCount: values[0]?.unweightedSampleForCount
            }
        };
    }, {});
}

function hasEqualSampleSizes(dataColumns: CrosstabHeader[], results: InstanceResult[]): boolean {
    const resultsArray = Array.from(results.values());
    return dataColumns.every((column: CrosstabHeader) => {
        var resultsForColumn = resultsArray.map(v => v.values[column.id]);
        var firstSampleSize = resultsForColumn[0]?.sampleSizeMetaData?.sampleSize.unweighted;
        return resultsForColumn.every(r => r?.sampleSizeMetaData?.sampleSize.unweighted == firstSampleSize);
    });
}

function removeTotalColumns(categories: CrosstabCategory[]): CrosstabCategory[] {
    return categories
        .filter(c => !c.isTotalCategory)
        .map(c => new CrosstabCategory({
            ...c,
            subCategories: c.subCategories.filter(sc => !sc.isTotalCategory)
        }));
}

const Crosstab: React.FunctionComponent<IProps> = (props: IProps) => {
    const [hoverColumnIndex, setHoverColumnIndex] = useState<number | undefined>(undefined);
    const hideTotalColumn = props.hideTotalColumn && props.hasBreaksApplied;
    const categories = hideTotalColumn ?
        removeTotalColumns(props.results.crosstabResult.categories) :
        props.results.crosstabResult.categories;
    const headerRows = createHeaderRows(categories);
    const dataColumns = headerRows[headerRows.length - 1].slice(1); // Excludes the entity instance
    const firstColumnSpan = 3;
    const lowSampleValue = props.lowSampleThreshold;
    const sampleSizeLookup = getSampleSizeLookup(dataColumns, props.results.crosstabResult.instanceResults);
    const rowsShareSampleSize = hasEqualSampleSizes(dataColumns, props.results.crosstabResult.instanceResults);

    const roundCountNumber = (sampleSize: number) => {
        return NumberFormattingHelper.formatCount(sampleSize);
    }

    const sampleSizeRow = (
        <tr key="SampleSizeRow">
            <th key="SampleSizeRowLabel" colSpan={firstColumnSpan}>{props.isDataWeighted && "Weighted "}Total</th>
            {dataColumns.map((c, i) => {
                const sampleSize = sampleSizeLookup[c.id].sampleForCount;
                const lowTotalSample = props.highlightLowSample && sampleSize != null && sampleSize >0 && sampleSize <= lowSampleValue;
                const roundedSampleSize = sampleSize == 0 ? "-" : roundCountNumber(sampleSize)
                const sampleCell = <td className={`data-cell ${lowTotalSample ? ' low-sample' : ''}`} key={`sample-${i}-${c.id}`}>{roundedSampleSize}</td>;
                return ([
                    sampleCell,
                ]);
            })}
        </tr>
    );

    const unweightedSampleSizeRow = (
        <tr key="UnweightedSampleSizeRow">
            <th key="UnweightedSampleSizeRowLabel" colSpan={firstColumnSpan}>Unweighted Total</th>
            {dataColumns.map((c, i) => {
                const sampleSize = sampleSizeLookup[c.id].unweightedSampleForCount;
                const lowTotalSample = props.highlightLowSample && sampleSize != null && sampleSize > 0 && sampleSize <= lowSampleValue;
                const roundedSampleSize = sampleSize == 0 ? "-" : roundCountNumber(sampleSize!)
                const sampleCell = <td className={`data-cell ${lowTotalSample ? ' low-sample' : ''}`} key={`sample-${i}-${c.id}`}>{roundedSampleSize}</td>;
                return ([
                    sampleCell,
                ]);
            })}
        </tr>
    );

    if (props.results.curatedResults.length == 0) {
        return (
            <></>
        );
    }

    const renderNumberOfEmptyRowsAndColumns = () => {
        const numberOfBlankRows = props.results.numberOfEmptyRows;
        const numberOfHiddenColumns = props.results.crosstabResult.hiddenColumns;
        let text = ""

        if(numberOfHiddenColumns > 0 && numberOfBlankRows > 0) {
            text = `${numberOfBlankRows} empty row${numberOfBlankRows == 1 ? "" : "s"} and ${numberOfHiddenColumns} empty column${numberOfHiddenColumns == 1 ? "" : "s"} hidden`
        }
        else if(numberOfHiddenColumns > 0) {
            text = `${numberOfHiddenColumns} empty column${numberOfHiddenColumns == 1 ? "" : "s"} hidden`
        }
        else if(numberOfBlankRows > 0) {
            text = `${numberOfBlankRows} empty row${numberOfBlankRows == 1 ? "" : "s"} hidden`
        }

        if(text != "") {
            return (
                <div className="blank-rows">
                    {text}
                </div>
            );    
        }
    }
    
    const hasLowSample = (data: ICellResult) =>
        props.highlightLowSample &&
        data?.sampleSizeMetaData?.sampleSize.unweighted != null &&
        data?.sampleSizeMetaData?.sampleSize.unweighted > 0 &&
        data?.sampleSizeMetaData?.sampleSize.unweighted <= lowSampleValue;

    const getRowLabel = (entityInstance: EntityInstance, displayMeanValues: boolean) => {
        if (displayMeanValues) {
            let meanValue = entityInstance.id.toString();
            if (props.metric.entityInstanceIdMeanCalculationValueMapping) {
                const mapping = props.metric.entityInstanceIdMeanCalculationValueMapping.mapping.find(m => m.entityId == entityInstance.id)

                if (mapping) {
                    if (!mapping.includeInCalculation) {
                        meanValue = "-";
                    } else {
                        meanValue = mapping.meanCalculationValue.toString();
                    }
                }
            }

            return `${entityInstance.name} (${meanValue})`;
        }
        else {
            return `${entityInstance.name}`;
        }
    }

    const shouldShowSignificance = (significance: Significance | undefined) => {
        if (props.highlightSignificance && 
            (props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowBoth
            ||  props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowUp && significance == Significance.Up
            || props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowDown && significance == Significance.Down)) {
            return true;
        }
        return false;
    }

    const footerName = headerRows[headerRows.length - 1][0]?.name;
    return (
        <div className="question-table-container bg-transparent">
            <div className="table-scroll-container">
                <table className="question-table">
                    <thead>
                        {headerRows.length > 1 && headerRows.slice(0, headerRows.length - 1).map((h, i) => {
                            return (
                                <tr key={`${i}-${props.metric.name}`}>
                                    {h.map((col, j) => {
                                        let columnSpan = col.columnSpan;
                                        if (j == 0) {
                                            columnSpan = firstColumnSpan;
                                        }
                                        const headerCell = (
                                            <th key={`${i}-${j}-${col.id}`} colSpan={columnSpan}>
                                                {col.name && col.subHeaders.length > 0 ? <div>{col.name}</div> : col.name}
                                            </th>
                                        );
                                        return ([
                                            headerCell
                                        ]);
                                    })}
                                </tr>
                            );
                        })}
                        {headerRows.slice(headerRows.length - 1).map((h, i) => {
                            const index = i + headerRows.length;
                            return (
                                <tr key={`${index}-${props.metric.name}`}>
                                    {h.map((col, j) => {
                                        let columnSpan = col.columnSpan;
                                        if (j == 0) {
                                            columnSpan = firstColumnSpan;
                                        }
                                        const header = (
                                            <th key={`${index}-${j}-${col.id}`} colSpan={columnSpan}>
                                                {col.name && col.subHeaders.length > 0 ? <div>{col.name}</div> : col.name}
                                            </th>
                                        );
                                        return header;
                                    })}
                                </tr>
                            );
                        })}
                    </thead>
                    <tbody>
                        {props.highlightSignificance && props.significanceType == CrosstabSignificanceType.CompareWithinBreak && (props.results.crosstabResult.categories.length > 2) &&
                            <tr>
                                <th colSpan={firstColumnSpan}>Significance</th>
                                {dataColumns.map((c, i) => {
                                    const cellSuffix = hoverColumnIndex === i ? " hover-highlight" : "";
                                    return (
                                        <CrosstabHeaderCell
                                            key={i}
                                            crosstabHeader={c}
                                            index={i}
                                            cellSuffix={cellSuffix}
                                            dataColumns={dataColumns}
                                            metric={props.metric}
                                            decimalPlaces={props.decimalPlaces}
                                            setHoverColumnIndex={setHoverColumnIndex}
                                            roundCountNumber={roundCountNumber}
                                        />
                                    )
                                })}
                            </tr>
                        }
                        {
                            props.results.curatedResults.map(r => {
                                return (
                                    <tr key={r.entityInstance.id}>
                                        <th colSpan={firstColumnSpan}>{getRowLabel(r.entityInstance, props.displayMeanValues)}</th>
                                        {dataColumns.map((c, i) => {
                                            const data: ICellResult = r.values[c.id];
                                            const lowSample = hasLowSample(data);
                                            const showSignificance = shouldShowSignificance(data.significance)
                                            const cellSuffix = `${lowSample ? ' low-sample' : ''}${hoverColumnIndex === i ? " hover-highlight" : ""}${showSignificance ? ' significant' : ''}`;
                                            return ([
                                                <CrosstabCell
                                                    key={i}
                                                    crosstabHeader={c}
                                                    index={i}
                                                    withToolTip={true}
                                                    cellSuffix={cellSuffix}
                                                    data={data}
                                                    showSampleSize={!rowsShareSampleSize}
                                                    includeCounts={props.includeCounts}
                                                    dataColumns={dataColumns}
                                                    metric={props.metric}
                                                    decimalPlaces={props.decimalPlaces}
                                                    setHoverColumnIndex={setHoverColumnIndex}
                                                    roundCountNumber={roundCountNumber}
                                                    showSignificance={showSignificance}
                                                />
                                            ]);
                                        })}
                                    </tr>
                                );
                            })}
                        {
                            props.averageResults.map(r => {
                                let averages = r.overallDailyResult.breakName !== undefined ?
                                    [r.overallDailyResult].concat(r.dailyResultPerBreak) : r.dailyResultPerBreak;
                                if (hideTotalColumn) {
                                    averages = averages.filter(a => a.breakName !== "Total");
                                }
                                const renderStandardDeviation = isTypeOfMean(r.averageType) && props.displayStandardDeviation;
                                
                                const renderAverageCells = (statisticType?: StatisticType) => 
                                    dataColumns.map((c, i) => {
                                        const data = averages[i];
                                        const cellSuffix = `$${hoverColumnIndex === i ? " hover-highlight" : ""}`;
                                        return (
                                            <AverageCell
                                                key={i}
                                                crosstabHeader={c}
                                                index={i}
                                                cellSuffix={cellSuffix}
                                                averageType={r.averageType}
                                                data={data}
                                                dataColumns={dataColumns}
                                                metric={props.metric}
                                                decimalPlaces={props.decimalPlaces}
                                                setHoverColumnIndex={setHoverColumnIndex}
                                                statisticType={statisticType}
                                            />
                                        );
                                    }
                                );

                                return (
                                    <>
                                        <tr className="average" key={r.averageType}>
                                            <th colSpan={firstColumnSpan}>{getAverageDisplayText(r.averageType)}</th>
                                            {renderAverageCells()}
                                        </tr>
                                        { renderStandardDeviation &&
                                        <>
                                            <tr className="average" key={r.averageType + "SD"}>
                                                <th colSpan={firstColumnSpan}>Standard deviation</th>
                                                {renderAverageCells(StatisticType.StandardDeviation)}
                                            </tr>
                                            <tr className="average" key={r.averageType + "variance"}>
                                                <th colSpan={firstColumnSpan}>Variance</th>
                                                {renderAverageCells(StatisticType.Variance)}
                                            </tr>
                                        </>
                                        }
                                    </>
                                )
                            })
                        }
                    </tbody>
                    <tfoot>
                        {rowsShareSampleSize && sampleSizeRow}
                        {props.isDataWeighted && unweightedSampleSizeRow}
                    </tfoot>
                </table>

                {props.isUserAdmin && renderNumberOfEmptyRowsAndColumns()}
            </div>
            <AllVueDescriptionFooter
                sampleSizeMeta={props.results.crosstabResult.sampleSizeMetadata}
                metric={props.metric}
                filterInstanceNames={footerName ? [footerName] : []}
                baseExpressionOverride={props.baseExpressionOverride}
                isSurveyVue={props.isSurveyVue}
                footerAverages={undefined} //crosstab shows averages as distinct rows
                decimalPlaces={0}
            />
        </div>
    );
};

export default Crosstab;
