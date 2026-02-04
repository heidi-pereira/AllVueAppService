import React from "react";
import { Timeline } from 'react-twitter-widgets'
import { useResizeDetector } from 'react-resize-detector';

interface ITwitterWidgetProps {
    selectedSubsetId: string;
    twitterHandle: string;
}

export function getHandleFromTokenisedString(candidateHandle : string, selectedSubsetId: string) {
    let handle = "";
    let candidateHandleAsTokens = candidateHandle.split("|");

    for (let x of candidateHandleAsTokens ) {
        let tokens = x.split(":");
        if (tokens.length === 2) {
            if (tokens[0] === selectedSubsetId) {
                // best case is exact subset match
                handle = tokens[1];
                break;
            }
        } else if (tokens.length == 1) {
            // fallback is single token which is the handle if either no subset is 
            // specified or no handle that corresponds to the specified subset
            // can be found but can be overridden by an exact subset
            // match later in the candidate handles string
            handle = tokens[0];
        }
    }
    return handle;
}

// Timeline (with options)
const TwitterWidget = (props: ITwitterWidgetProps) => {
    const handle = getHandleFromTokenisedString(props.twitterHandle, props.selectedSubsetId);
    const { height, ref } = useResizeDetector<HTMLDivElement>({ refreshMode: 'debounce', refreshRate: 100 });
    return (
        <div className="tweet-container" ref={ref}>
            <Timeline
                dataSource={{ sourceType: "profile", screenName: handle }}
                options={{ chrome: "transparent", height: height }}
            />
        </div>
    );
}

export default TwitterWidget;