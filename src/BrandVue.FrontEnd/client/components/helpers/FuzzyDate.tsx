import React from 'react';
import moment from 'moment';

interface FuzzDateProps {
    date: Date;
    lowerCase?: boolean;
    includePastFuture?: boolean;
}

const FuzzyDate = (props: FuzzDateProps) => {

    if (props.date == null) {
        return null;
    }
    const getFutureText = () => (props.includePastFuture ?? true) ? "In %s" : "%s";
    const getPastText = () => (props.includePastFuture ?? true) ? "%s ago" : "%s";

    moment.updateLocale('en', {
        relativeTime: {
            s: '1 second',
            ss: '%d seconds',
            m: "1 minute",
            mm: "%d minutes",
            h: "1 hour",
            hh: "%d hours",
            d: "1 day",
            dd: "%d days",
            M: "1 month",
            MM: "%d months",
            y: "1 year",
            yy: "%d years",    
            future: getFutureText(),
            past: getPastText()
        }
    });

    let relativeDate = moment(props.date).fromNow();

    if (props.lowerCase ?? false) {
        relativeDate = relativeDate.toLocaleLowerCase();
    }

    if (moment(props.date).year() === 0) {
        relativeDate = 'Not available';
    }

    return (
        <span title={props.date.toLocaleString()}>{relativeDate}</span>
    );
};

export default FuzzyDate;