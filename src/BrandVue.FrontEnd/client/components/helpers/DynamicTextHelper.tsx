import React from "react";
import {Link, useLocation} from "react-router-dom";
import { StringHelper } from "../../helpers/StringHelper";
import { getUrlForMetricOrPageDisplayName } from "./PagesHelper";
import { IReadVueQueryParams } from "./UrlHelper";

export class DynamicTextHelper {
    private _brand: string | undefined;
    private _sampleSizeDescription: string;

    constructor(brand: string | undefined, sampleSizeDescription: string) {
        this._brand = brand;
        this._sampleSizeDescription = sampleSizeDescription;
    }

    getLinkFromMetricName(metricName: string, readVueQueryParams: IReadVueQueryParams): JSX.Element {
        const location = useLocation();
        return <Link to={{
            pathname: getUrlForMetricOrPageDisplayName(metricName, location, readVueQueryParams),
            search: location.search
        }}>{metricName}</Link>;
    }

    replaceMetricsWithLinks(originalText: string, readVueQueryParams: IReadVueQueryParams): JSX.Element {
        // Expect metric names to be placed inside square brackets []
        const regExpForPlaceholders = /(\[)[^\][]+(])/g;
        // Find all instances in originalText of things wrapped in square brackets
        const matches = Array.from(new Set(originalText.matchAll(regExpForPlaceholders)));

        // For everything we found, strip off the square brackets and then turn it into the matching link
        const stringReplacements = matches.map(m => {
            const placeholder = m[0].toString();
            const metricName = placeholder.substring(1, placeholder.length - 1);
            const metricLink = this.getLinkFromMetricName(metricName, readVueQueryParams);

            return {
                searchString: placeholder,
                replace: metricLink
            }
        });

        const replacementJSX = stringReplacements.length > 0 && StringHelper.replaceJSXMulti(originalText, stringReplacements);

        return <div>{(replacementJSX && replacementJSX.length > 0) ? replacementJSX : originalText}</div>;
    }

    replaceText(originalText: string): string {
        var replacedText: string;
        if (typeof this._brand === "string") {
            replacedText = originalText.replaceAll("[BRAND]", this._brand);
        } else {
            replacedText = originalText;
        }
        replacedText = replacedText.replaceAll("[SAMPLESIZE]", this._sampleSizeDescription);
        replacedText = replacedText.replaceAll("[N]", this._sampleSizeDescription);

        return replacedText;
    }

    replaceTextWithJSX(originalText: string, readVueQueryParams: IReadVueQueryParams): JSX.Element {
        return this.replaceMetricsWithLinks(originalText, readVueQueryParams);
    }
}