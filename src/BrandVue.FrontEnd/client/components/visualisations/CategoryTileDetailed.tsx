import React from 'react'
import chroma from "chroma-js";
import { NumberFormattingHelper } from '../../helpers/NumberFormattingHelper';
import Tooltip from "../Tooltip";
import { ICategoryTileBaseProps } from './ICategoryTileBaseProps';
import { CategoryExportResult, CategoryExportResultCard } from '../../BrandVueApi';
import { CategoryContext } from '../helpers/CategoryContext';
import { getDisplayValue } from '../../helpers/CategoryComparisonHelper';
import { getCategoryComparisonColorByName, getCategoryComparisonSecondaryColorByName } from '../helpers/ChromaHelper';

interface ICategoryDetailedTileProps {
    questionText: string | undefined;
}

interface IIndexPresentation {
    backgroundColor: string;
    fontColor: string;
}

function getIndexPresentation(index: number): IIndexPresentation {    

    // Generate colour scales, with 200 colour values, for over & under indexes
    // Scales are created between the two colours passed
    var overIndexColours = chroma.scale(['218234', 'ffffff']).colors(200);
    var underIndexColours = chroma.scale(['d70000', 'ffffff']).colors(200);

    // Over index is value of 120 and above
    if (index > 119) {
        // Closer to 120 we are the lighter we want the colour from the over index scale to be
        // The higher the index the darker the colour will be
        var newIndex = Math.round((119 / index) * 200);
        // Get the contrast between the chosen colour & black
        var contrast = chroma.contrast(overIndexColours[newIndex], '#000000');
        // If the contrast is insufficient for accessibility (less than 4.5) change font colour to white
        var fontColor;
        if (contrast > 4.5) {
            fontColor = "#000000";
        } else {
            fontColor = "#ffffff";
        }
        // Setting values to use in code below
        return {
            backgroundColor: overIndexColours[newIndex],
            fontColor: fontColor
        }
    }

    // Under index is value of 80 and below
    if (index < 81) {
        // Closer to 80 we are the lighter we want the colour from under index scale to be
        // The lower the index the darker the colour will be
        var newIndex = Math.round((index / 81) * 200);
        // Get the contrast between the chosen colour & black
        var contrast = chroma.contrast(underIndexColours[newIndex], '#000000');
        // If the contrast is insufficient for accessibility (less than 4.5) change font colour to white
        var fontColor;
        if (contrast > 4.5) {
            fontColor = "#000000";
        } else {
            fontColor = "#ffffff";
        }
        // Setting values to use in code below
        return {
            backgroundColor: underIndexColours[newIndex],
            fontColor: fontColor
        }
    }

    // Setting default values to use in code below
    return {
        backgroundColor: "rgba(255, 0, 0, 0)",
        fontColor: "rgba(0,0,0,0.24)"
    };
}

const CategoryTileDetailed = (props: ICategoryDetailedTileProps & ICategoryTileBaseProps) => {
    const { addCategoryExportResultCards } = React.useContext(CategoryContext);
    // Creating an index of the result value, where 100 is equal to average
    const getIndex = (firstValue: number, secondValue: number) => Math.round((firstValue / secondValue) * 100);

    React.useEffect(() => {
        const exportResults = props.results.map(r => new CategoryExportResult({
            name: r.displayName,
            firstBaseValue: r.firstBaseValue,
            secondBaseValue: r.secondBaseValue,
            index: getIndex(r.firstBaseValue, r.secondBaseValue),
        }));
        addCategoryExportResultCards(new CategoryExportResultCard({
            title: props.title,
            results: exportResults,
            questionText: props.questionText,
            isDetailed: true,
            containsMarketAverage: props.containsMarketAverage,
            paneIndex: props.paneIndex,
        }));
    }, []);

    return (
        <div className="category-section-detailed">
            <div className="question">{props.questionText}</div>
            <div className="heading-bar">
                    <div className="headings">
                        <div>Index</div>
                        <div>Score</div>
                        <div>Average</div>
                    </div>
                </div>
            {props.topResults.map(r => {
           
                const index = getIndex(r.firstBaseValue, r.secondBaseValue);
                const indexPresentation = getIndexPresentation(index);
                
                return (
                    <Tooltip placement="right" title={props.getTooltip(r)} >
                        <div key={r.displayName} className="row-bar">
                            <div className={"bar top "} style={{
                                backgroundColor: getCategoryComparisonColorByName("LightBlue"),
                                width: NumberFormattingHelper.formatNonCulturealDefaultFormat1Dp(r.firstBaseValue)
                            }} />
                            <div className="bar bottom" style={{
                                backgroundColor: getCategoryComparisonSecondaryColorByName("LightGrey"),
                                width: NumberFormattingHelper.formatNonCulturealDefaultFormat1Dp(r.secondBaseValue)
                            }} />
                            <div className="flex-container">
                                <div className="dot-and-label">
                                    <div className={"dot"} style={{ backgroundColor: getCategoryComparisonColorByName(props.color)}}></div>
                                    <div className="bar-label">{r.displayName}</div>
                                </div>
                                <div className="value">
                                    <div className="index" style={{
                                        backgroundColor: indexPresentation.backgroundColor,
                                        color: indexPresentation.fontColor
                                    }}>
                                        {index}
                                    </div>
                                    <div className="percent">{getDisplayValue(r.firstBaseValue)}%</div>
                                    <div className="percent">{getDisplayValue(r.secondBaseValue)}%</div>
                                </div>
                            </div>
                        </div>
                    </Tooltip>
                );
            })}
        </div>
    )
}

export default CategoryTileDetailed