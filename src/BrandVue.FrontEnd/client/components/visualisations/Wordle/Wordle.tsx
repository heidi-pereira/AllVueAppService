import { useState, useEffect } from 'react';
import ReactWordCloud from "react-wordcloud";
import { useResizeDetector } from 'react-resize-detector';
import { Word, MinMaxPair } from "react-wordcloud";
import { Metric } from "../../../metrics/metric";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import {ComparisonPeriodSelection, Factory, MakeUpTo, WordleResults} from "../../../BrandVueApi";
import { ViewHelper } from "../ViewHelper";
import { EntityInstance } from "../../../entity/EntityInstance";
import 'tippy.js/dist/tippy.css';
import 'tippy.js/animations/scale.css'
import { NoDataError } from "../../../NoDataError";
import { selectSubsetId } from 'client/state/subsetSlice';
import { useAppSelector } from 'client/state/store';

import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface IProps {
    activeBrand: EntityInstance;
    metrics: Metric[];
    filters: CuratedFilters;
    hiddenWords?: string[];
    onWordClick?: (word: Word) => void;
    onWordleResultsReceived?: (results: WordleResults) => void;
    size?: DOMRectReadOnly;
}

const Wordle = ({ metrics, filters, activeBrand, hiddenWords, onWordClick, onWordleResultsReceived, size }: IProps) => {

    const [words, setWords] = useState<Array<Word>>([]);
    const [dataLoaded, setDataLoaded] = useState<boolean>(false);
    const [lastLoad, setLoading] = useState<string | null>(null);
    const { width, height, ref } = useResizeDetector<HTMLDivElement>();
    const [error, setError] = useState<boolean>(false);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    
    useEffect(() => {
        // Daily averages are not supported
        if (filters.average.makeUpTo === MakeUpTo.Day) {
            return;
        }

        const overrideParams = { comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly };

        const curatedResultsModel = ViewHelper.createCuratedRequestModel([activeBrand.id],
            metrics,
            filters,
            activeBrand.id,
            overrideParams,
            subsetId,
            timeSelection);
        const loading = lastLoad;
        setLoading(JSON.stringify(curatedResultsModel));
        if (loading != JSON.stringify(curatedResultsModel)) {
            setDataLoaded(false);
            setError(false);
            Factory.DataClient(throwError => throwError())
                .getWordleResults(curatedResultsModel)
                .then(
                    resp => {
                        const filteredWords = resp.results[0].weightedDailyResults.map(r => ({
                            text: r.text,
                            value: r.unweightedSampleSize
                        }));
                        setWords(filteredWords);
                        setDataLoaded(true);
                        if (onWordleResultsReceived) {
                            onWordleResultsReceived(resp);
                        }
                    }).catch((e: any) => {
                        if (e.typeDiscriminator !== NoDataError.typeDiscriminator) {
                            setError(true);
                        }
                        setWords(new Array<Word>());
                        setDataLoaded(true);
                    })
        }
    }, [activeBrand, metrics, filters, timeSelection]);


    let infoResponseElement: JSX.Element | null = null;

    if ((!infoResponseElement) && (filters.average.makeUpTo === MakeUpTo.Day)) {
        infoResponseElement = <span>Please select a fixed time period.</span>;
    }

    if ((!infoResponseElement) && !dataLoaded) {
        infoResponseElement = <span>Generating... please wait...</span>;
    }

    if ((!infoResponseElement) && error) {
        infoResponseElement = <span>Error generating word cloud, please try again later</span>;
    }

    const visibleWords = hiddenWords ? words.filter(k => hiddenWords.indexOf(k.text) < 0) : words;

    if ((!infoResponseElement) && (visibleWords.length === 0)) {
        infoResponseElement = <span>No data for given time period and filters</span>;
    }

    const wordleOptions = {
        fontFamily: 'Segoe UI',
        deterministic: false,
        rotationAngles: [0, 0] as MinMaxPair,
        rotations: 1,
        transitionDuration: 0,
        padding: 10,
        fontSizes: [20, 120] as MinMaxPair
    };

    const getWordleSize = (): [number, number] | undefined => {
        if (size) {
            return [size.width, size.height];
        }

        return [width ?? 0, height ?? 0];
}

    return <div ref={ref} style={{ "position": "relative", "height": "100%" }}>
        <div style={{ "position": "absolute" }}>
            {(infoResponseElement)
                ? infoResponseElement
                : <ReactWordCloud
                    words={visibleWords}
                    callbacks={{ onWordClick: onWordClick }}
                    maxWords={50}
                    options={wordleOptions}
                    size={getWordleSize()}/>}
        </div>
    </div>;

}

export default Wordle;
