import React from 'react';
import { NumberFormattingHelper } from '../../helpers/NumberFormattingHelper';
import Tooltip from "../Tooltip";
import {
    getFirstLegendDescription,
    getSecondLegendDescription,
    getDisplayValue
} from '../../helpers/CategoryComparisonHelper';
import { CategoryExportResult, CategoryExportResultCard } from '../../BrandVueApi';
import { ICategoryTileBaseProps } from './ICategoryTileBaseProps';
import { CategoryContext } from '../helpers/CategoryContext';
import { getCategoryComparisonColorByName, getCategoryComparisonSecondaryColorByName } from '../helpers/ChromaHelper';

interface ICategoryTileProps {
    averageName: string;
    baseDisplayName1: string | undefined;
    baseDisplayName2: string | undefined;
}

const CategoryTile = (props: ICategoryTileProps & ICategoryTileBaseProps) => {
    const { addCategoryExportResultCards } = React.useContext(CategoryContext);

    React.useEffect(() => {
        const exportResults = props.results.map(r => new CategoryExportResult({
            name: r.displayName,
            firstBaseValue: r.firstBaseValue,
            secondBaseValue: r.secondBaseValue,
        }));
        addCategoryExportResultCards(new CategoryExportResultCard({
            title: props.title,
            results: exportResults,
            isDetailed: false,
            containsMarketAverage: props.containsMarketAverage,
            paneIndex: props.paneIndex,
        }));
    }, []); 

    return (
        <div className="category-section">
            {props.topResults.map(r => {
                const categoryResultDisplay = r.displayName;
                return (
                    <Tooltip placement="right" title={props.getTooltip(r)} >
                        <div key={categoryResultDisplay} className="metric-value-box">
                            <div className="choice-text">
                                <span className="data fixed flex">{getDisplayValue(r.firstBaseValue)}<div className="percentage">%</div></span>
                                <span>{categoryResultDisplay}</span>
                            </div>
                            <div className="spark-line-background">
                                <div className="spark-line spark-line-active-entity-value" style={{
                                    backgroundColor: getCategoryComparisonColorByName(props.color),
                                    width: NumberFormattingHelper.formatNonCulturealDefaultFormat1Dp(r.firstBaseValue)
                                }} />
                                <div className="spark-line spark-line-comparison-value" style={{
                                    backgroundColor: getCategoryComparisonSecondaryColorByName(props.baseDisplayName2 ? props.color : "Grey"),
                                    width: NumberFormattingHelper.formatNonCulturealDefaultFormat1Dp(r.secondBaseValue)
                                }} />
                            </div>
                        </div>
                    </Tooltip>
                );
            })}
            <div className="inline-legend">
                <div className="legend-container">
                    <div>
                        <div className="legend-icon" style={{ backgroundColor: getCategoryComparisonColorByName(props.color)}}/>
                        {getFirstLegendDescription(props.baseDisplayName1, props.activeBrandName)}
                    </div>
                    <div>
                        <div className="legend-icon" style={{ backgroundColor: getCategoryComparisonSecondaryColorByName(props.baseDisplayName2 ? props.color : "Grey")}}/>
                        {getSecondLegendDescription(props.baseDisplayName1, props.baseDisplayName2)}
                    </div>
                </div>
                <p>{props.baseDisplayName2 ? props.averageName : `Average: ${props.averageName}`}</p>
            </div>
        </div>
    )
}

export default CategoryTile