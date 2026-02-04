import { IMixPanelClient } from "./IMixPanelClient";

export type VueEventName = 
    "pageLoaded"

export class VueEventProps {
    Category: string;
    SubCategory: string;
    Tag?: string | undefined;
    Page?: string;

    constructor(Category: string, subCategory: string, tag?: string | undefined, page?: string) {
        this.Category = Category;
        this.SubCategory = subCategory;
        this.Tag = tag;
        this.Page = page;
    }
}

export type MixPanelProps = { [TKey in VueEventName]: VueEventProps };

export type MixPanelModel = {
    userId: string; 
    projectId: string; 
    client: IMixPanelClient; 
    productName: string;
}
