import { GetApiCompaniesByCompanyIdAncestornamesApiResponse } from "@/rtk/apiSlice";

const getAllCompanyNames = (companyName: string | undefined, companyAncestorNames: GetApiCompaniesByCompanyIdAncestornamesApiResponse | undefined) => {
    if (!companyAncestorNames || companyAncestorNames.length === 0 || !companyName) {
    return companyName || "";
    }
    const companyNames = [companyName, ...companyAncestorNames];
    return companyNames.slice(0, -1).join(", ") + " & " + companyNames[companyNames.length - 1];
};

export { getAllCompanyNames };

export function joinCapitalised(strings: string[]): string {
    return strings.map(s => s.charAt(0).toLocaleUpperCase() + s.slice(1)).join(', ');
}