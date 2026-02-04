import React from 'react';
import { StringHelper } from './StringHelper';

const stringReplacementsEmpty = [{
    searchString: "",
    replace: <a></a>
}];

const stringReplacementsSingle = [{
    searchString: "[placeholder]",
    replace: <a>replacement</a>
}];

const stringReplacementsMulti = [{
    searchString: "[placeholder1]",
    replace: <a>replacement1</a>
}, {
    searchString: "[placeholder2]",
    replace: <a>replacement2</a>
}];

const getElementContent = (element: JSX.Element | string | undefined) => {
    return (element && typeof element === "object") ? element.props.children : undefined;
};

describe("replaceJSXMulti", () => {
    it("should return a string that matches the input if no placeholders specified", () => {
        const inputString_noPlaceholder_otherText = "no place holder, some other text";

        const result = StringHelper.replaceJSXMulti(inputString_noPlaceholder_otherText, stringReplacementsEmpty);

        expect(typeof result[0] === "string");
        expect(result.join()).toBe(inputString_noPlaceholder_otherText);
    });

    it("should return the provided JSX element in place of a placeholder in a single-placeholder-only scenario", () => {
        const inputString_singlePlaceholder_noOtherText = "[placeholder]";

        const result = StringHelper.replaceJSXMulti(inputString_singlePlaceholder_noOtherText, stringReplacementsSingle);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");

        expect(result[0]).toBe("");
        expect(getElementContent(result[1])).toBe("replacement");
    });

    it("should return the provided JSX in place of a placeholder when placeholder preceded by text", () => {
        const inputString_singlePlaceholder_otherTextBefore = "before test [placeholder]";

        const result = StringHelper.replaceJSXMulti(inputString_singlePlaceholder_otherTextBefore, stringReplacementsSingle);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");

        expect(result[0]).toBe("before test ");
        expect(getElementContent(result[1])).toBe("replacement");
    });

    it("should return the provided JSX in place of a placeholder when placeholder succeeded by text", () => {
        const inputString_singlePlaceholder_otherTextAfter = "[placeholder] after test";

        const result = StringHelper.replaceJSXMulti(inputString_singlePlaceholder_otherTextAfter, stringReplacementsSingle);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");

        expect(result[0]).toBe("");
        expect(getElementContent(result[1])).toBe("replacement");
        expect(result[2]).toBe(" after test");
    });

    it("should return the provided JSX in place of a placeholder when placeholder preceded and succeeded by text", () => {
        const inputString_singlePlaceholder_otherTextBeforeAndAfter = "before test [placeholder] after test";

        const result = StringHelper.replaceJSXMulti(inputString_singlePlaceholder_otherTextBeforeAndAfter, stringReplacementsSingle);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");

        expect(result[0]).toBe("before test ");
        expect(getElementContent(result[1])).toBe("replacement");
        expect(result[2]).toBe(" after test");
    });

    it("should return the provided JSX elements in place of their respective placeholders", () => {
        const inputString_multiPlaceholder_noOtherText = "[placeholder1] [placeholder2]";

        const result = StringHelper.replaceJSXMulti(inputString_multiPlaceholder_noOtherText, stringReplacementsMulti);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");

        expect(result[0]).toBe("");
        expect(getElementContent(result[1])).toBe("replacement1");
        expect(result[2]).toBe(" ");
        expect(getElementContent(result[3])).toBe("replacement2");
    });

    it("should return the provided JSX elements in place of their respective placeholders when placeholders preceded by text", () => {
        const inputString_multiPlaceholder_otherTextBefore = "before test [placeholder1] [placeholder2]";

        const result = StringHelper.replaceJSXMulti(inputString_multiPlaceholder_otherTextBefore, stringReplacementsMulti);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");

        expect(result[0]).toBe("before test ");
        expect(getElementContent(result[1])).toBe("replacement1");
        expect(result[2]).toBe(" ");
        expect(getElementContent(result[3])).toBe("replacement2");
    });

    it("should return the provided JSX elements in place of their respective placeholders when placeholders succeeded by text", () => {
        const inputString_multiPlaceholder_otherTextAfter = "[placeholder1] [placeholder2] after test";

        const result = StringHelper.replaceJSXMulti(inputString_multiPlaceholder_otherTextAfter, stringReplacementsMulti);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");
        expect(typeof result[4]).toBe("string");

        expect(result[0]).toBe("");
        expect(getElementContent(result[1])).toBe("replacement1");
        expect(result[2]).toBe(" ");
        expect(getElementContent(result[3])).toBe("replacement2");
        expect(result[4]).toBe(" after test");
    });

    it("should return the provided JSX elements in place of their respective placeholders when placeholders preceded and succeeded by text", () => {
        const inputString_multiPlaceholder_otherTextBeforeAndAfter = "before test [placeholder1] [placeholder2] after test";

        const result = StringHelper.replaceJSXMulti(inputString_multiPlaceholder_otherTextBeforeAndAfter, stringReplacementsMulti);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");
        expect(typeof result[4]).toBe("string");

        expect(result[0]).toBe("before test ");
        expect(getElementContent(result[1])).toBe("replacement1");
        expect(result[2]).toBe(" ");
        expect(getElementContent(result[3])).toBe("replacement2");
        expect(result[4]).toBe(" after test");
    });

    it("should return the provided JSX element in place of each duplicate placeholder", () => {
        const inputString_duplicatePlaceholder_noOtherText = "[placeholder] [placeholder]";

        const result = StringHelper.replaceJSXMulti(inputString_duplicatePlaceholder_noOtherText, stringReplacementsSingle);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");
        expect(typeof result[4]).toBe("string");

        expect(result[0]).toBe("");
        expect(getElementContent(result[1])).toBe("replacement");
        expect(result[2]).toBe(" ");
        expect(getElementContent(result[3])).toBe("replacement");
    });

    it("should return the provided JSX element in place of each duplicate placeholder when placeholders preceded by text", () => {
        const inputString_duplicatePlaceholder_otherTextBefore = "before test [placeholder] [placeholder]";

        const result = StringHelper.replaceJSXMulti(inputString_duplicatePlaceholder_otherTextBefore, stringReplacementsSingle);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");
        expect(typeof result[4]).toBe("string");

        expect(result[0]).toBe("before test ");
        expect(getElementContent(result[1])).toBe("replacement");
        expect(result[2]).toBe(" ");
        expect(getElementContent(result[3])).toBe("replacement");
    });

    it("should return the provided JSX element in place of each duplicate placeholder when placeholders preceded by text", () => {
        const inputString_duplicatePlaceholder_otherTextAfter = "[placeholder] [placeholder] after test";

        const result = StringHelper.replaceJSXMulti(inputString_duplicatePlaceholder_otherTextAfter, stringReplacementsSingle);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");
        expect(typeof result[4]).toBe("string");

        expect(result[0]).toBe("");
        expect(getElementContent(result[1])).toBe("replacement");
        expect(result[2]).toBe(" ");
        expect(getElementContent(result[3])).toBe("replacement");
        expect(result[4]).toBe(" after test");
    });

    it("should return the provided JSX element in place of each duplicate placeholder when placeholders preceded and succeeded by text", () => {
        const inputString_duplicatePlaceholder_otherTextBeforeAndAfter = "before test [placeholder] [placeholder] after test";

        const result = StringHelper.replaceJSXMulti(inputString_duplicatePlaceholder_otherTextBeforeAndAfter, stringReplacementsSingle);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");
        expect(typeof result[4]).toBe("string");

        expect(result[0]).toBe("before test ");
        expect(getElementContent(result[1])).toBe("replacement");
        expect(result[2]).toBe(" ");
        expect(getElementContent(result[3])).toBe("replacement");
        expect(result[4]).toBe(" after test");
    });

    it("should return the provided JSX element for the respective placeholders when input is a mixture of duplicate, non-dupliate and text", () => {
        const inputString_duplicatePlaceholder_dupeNonDupeMix = "first [placeholder1] second [placeholder2] duplicate [placeholder1]";

        const result = StringHelper.replaceJSXMulti(inputString_duplicatePlaceholder_dupeNonDupeMix, stringReplacementsMulti);

        expect(typeof result[0]).toBe("string");
        expect(typeof result[1]).toBe("object");
        expect(typeof result[2]).toBe("string");
        expect(typeof result[3]).toBe("object");
        expect(typeof result[4]).toBe("string");
        expect(typeof result[5]).toBe("object");

        expect(result[0]).toBe("first ");
        expect(getElementContent(result[1])).toBe("replacement1");
        expect(result[2]).toBe(" second ");
        expect(getElementContent(result[3])).toBe("replacement2");
        expect(result[4]).toBe(" duplicate ");
        expect(getElementContent(result[5])).toBe("replacement1");
    });
});