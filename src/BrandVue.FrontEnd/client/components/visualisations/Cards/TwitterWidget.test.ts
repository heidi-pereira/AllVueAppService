import { getHandleFromTokenisedString } from "./TwitterWidget"

const twitterHandleTestCases: [string, string, string][] = [
    ["AllSubsetFeed", "All", "AllSubsetFeed"],
    ["UK:UKFeed|US:USFeed", "US", "USFeed"],
    ["UK:UKFeed|AllSubsetFeed","US","AllSubsetFeed"],
    ["UK:UKFeed|AllSubsetFeed","UK","UKFeed"],
    ["UK:UKFeed|US:USFeed","FR",""],
    ["UK:UKFeed","US",""],
    ["AllSubsetFeed|UK:UKFeed|US:USFeed|FR:FRFeed","UK","UKFeed"],
    ["UK:UKFeed|US:USFeed|AllSubsetFeed","","AllSubsetFeed"],
    ["","",""],
    ["","UK",""]
];

test.each(twitterHandleTestCases)("Should parse twitter handle string and return expected value",
    (handle: string, subsetId: string, expected:string) => {
        expect(getHandleFromTokenisedString(handle,subsetId)).toBe(expected);
    });
