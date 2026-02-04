import React from "react";
import { useState, useRef, useEffect, MouseEvent } from "react";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { EntityInstance } from "../../entity/EntityInstance";
import { WordleResults, SampleSizeMetadata } from "../../BrandVueApi";
import { ChartFooterInformation } from "./ChartFooterInformation";
import Wordle from "./Wordle/Wordle";
import { Word } from "react-wordcloud";
import { useReadVueQueryParams, useWriteVueQueryParams } from "../helpers/UrlHelper";
import { useLocation, useNavigate } from "react-router-dom";

interface IWordleProps {
    height: number;
    curatedFilters: CuratedFilters;
    metrics: Metric[];
    activeBrand: EntityInstance;
}

const wordleQueryString = "Wordle";

const WordleChart = (props: IWordleProps) => {
    const { getQueryParameterArray } = useReadVueQueryParams();
    const hiddenWordsFromQueryString = getQueryParameterArray<string>(wordleQueryString) || [];
    const [hiddenWords, setHiddenWords] = useState<string[]>(hiddenWordsFromQueryString);
    const [resultsSampleSizeMeta, setResultsSampleSizeMeta] = useState(new SampleSizeMetadata());
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    useEffect(() => {
        setQueryParameter(wordleQueryString, hiddenWords);
    }, [hiddenWords]);

    const wordClick = (w: Word) => {
        setHiddenWords(oldWords => [ ...oldWords, w.text]); //Lambda here ensures the latest state. There is a strange bug without this.
    }

    const resetHidden = (e: MouseEvent<HTMLAnchorElement>) => {
        e.preventDefault();
        setHiddenWords([]);
    }

    const onWordleResultsReceived = (results: WordleResults) => {
        setResultsSampleSizeMeta(results.sampleSizeMetadata);
    }

    const wordCloudRef = useRef<HTMLDivElement>(null);

    return (
        <>
            <div ref={wordCloudRef} style={{ height: props.height, overflow: 'hidden' }}>
                <Wordle
                    activeBrand={props.activeBrand}
                    metrics={props.metrics}
                    filters={props.curatedFilters}
                    hiddenWords={hiddenWords}
                    onWordClick={wordClick}
                    onWordleResultsReceived={onWordleResultsReceived}
                />
            </div>
            {hiddenWords.length > 0 &&
                <div><a href="#" className="wordle-resetHidden" onClick={resetHidden}>Excluded words: {hiddenWords.join(', ')}</a></div>}
            <div className="not-exported">
                <ChartFooterInformation sampleSizeMeta={resultsSampleSizeMeta} average={props.curatedFilters.average} activeBrand={props.activeBrand} metrics={props.metrics} />
            </div>
        </>
    );
};

export default WordleChart;
