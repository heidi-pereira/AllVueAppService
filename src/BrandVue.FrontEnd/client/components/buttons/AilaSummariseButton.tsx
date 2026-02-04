import React from "react";
import Tooltip from "../Tooltip";
import { Factory } from "../../BrandVueApi";
import { getSubsetToMaxLength } from "../../helpers/LlmContextHelper";
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import { MixPanel } from "../mixpanel/MixPanel";

interface IAilaSummariseButtonProps {
    disabled?: boolean;
    loading?: boolean;
    tooltipContent?: string;
    results?: string[];
    onSummariseComplete?: (summary: string) => void;
}

const AilaSummariseButton: React.FunctionComponent<IAilaSummariseButtonProps> = (props: IAilaSummariseButtonProps) => {
    const [isSummarising, setIsSummarising] = React.useState(false);
    const ailaClient = Factory.AilaClient(err => err());

    const onClick = () => {
        if (!props?.results) {
            // This should not be possible as the button should be disabled if there are no results
            return
        }

        // Hardcoded context max size. This isn't great, we should
        // ideally control this from config along side the model/endpoint.
        //
        // For gpt-4o use 100,000 bytes.
        // For gemini-2.0-flash use 2,000,000 bytes which will be about 500k tokens / 50% context approx.
        const maxContextSize = 2000000;
        const subset = getSubsetToMaxLength(props.results, maxContextSize);

        setIsSummarising(true);

        const resultPromise = ailaClient.summarise(subset.join("\n"), true, navigator.language)
        resultPromise.then(summary => {
            setIsSummarising(false);
            props.onSummariseComplete?.(summary);
        }).catch(err => {
            setIsSummarising(false);
            handleError(err);
        });
        MixPanel.track("aiSummariseClicked");
    }

    const noResults = !props?.results || props.results.length === 0;
    const buttonDisabled = props.loading || props.disabled || noResults || isSummarising;
    const showTooltipDeclineMessage = !props.loading && props.disabled

    const tooltipTitleStatic = (
        <>
            {showTooltipDeclineMessage && <div>You can only summarise if new data is collected</div>}
            {!showTooltipDeclineMessage &&
                <div>
                    <p className="tight">AI summarisation</p>
                    <p className="text-light-small">Powered by Aila</p>
                </div>
            }
        </>
    );
    const tooltipTitle = props?.tooltipContent ?? tooltipTitleStatic;

    return (
        <>
            <Tooltip placement="top" title={tooltipTitle}>
                <button disabled={buttonDisabled} className={`hollow-button ${isSummarising ? "loading" : ""}`} onClick={() => onClick()}>
                    <i className="material-symbols-outlined">star</i>
                    <div>Summarise</div>
                </button>
            </Tooltip> 
        </>
    );
};

export default AilaSummariseButton;