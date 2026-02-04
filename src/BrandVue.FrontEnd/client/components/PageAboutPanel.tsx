import React from "react";
import * as BrandVueApi from "../BrandVueApi";
import { PageAbout, PageDescriptor } from "../BrandVueApi";
import { UiAboutItem } from "./AboutItem";
import AboutPanel from "./AboutPanel";

interface IPageAboutPanelProps {
    page: PageDescriptor;
    userCanEdit: boolean;
    brand: string | undefined;
    sampleSizeDescription: string;
    visible: boolean;
}

const PageAboutPanel: React.FunctionComponent<IPageAboutPanelProps> = (props) => {
    const pageAboutClient = BrandVueApi.Factory.PagesClient(error => error());

    const transformToUiPageAboutItem = (p: PageAbout) => {
        return {
            about: p,
            displayTitle: p.aboutTitle,
            displayContent: p.aboutContent,
            originalTitle: p.aboutTitle,
            originalContent: p.aboutContent
        }
    }

    const transformPageAboutsToUiAbouts = (pageAbouts: PageAbout[]) => {
        return pageAbouts.map(transformToUiPageAboutItem);
    }

    const getPageAboutRecords = (): Promise<UiAboutItem[]> => {
        return pageAboutClient.getPageAbouts(props.page.id)
            .then(transformPageAboutsToUiAbouts);
    }

    const addPageAboutRecord = (): Promise<UiAboutItem> => {
        return pageAboutClient.createPageAbout(BrandVueApi.PageAbout.fromJS({
            aboutTitle: "New About Information",
            aboutContent: "",
            pageId: props.page.id,
            editable: true
        })).then(transformToUiPageAboutItem);
    }

    const deletePageAboutItem = (pageAboutItem: UiAboutItem) => {
        return pageAboutItem.about instanceof PageAbout
            ? pageAboutClient.deletePageAbout(pageAboutItem.about)
            : Promise.resolve(BrandVueApi.HttpStatusCode.OK);
    }

    const extractUpdatedPageAbout = (pageAboutItem: UiAboutItem) => {
        const newPageAboutItem = { ...pageAboutItem };
        newPageAboutItem.about.aboutTitle = newPageAboutItem.displayTitle;
        newPageAboutItem.about.aboutContent = newPageAboutItem.displayContent;
        if ("pageId" in newPageAboutItem.about)
            return newPageAboutItem.about;
    }

    const saveChanges = (pageAboutItems: UiAboutItem[]): Promise<UiAboutItem[]> => {
        const newPageAboutsToSave = pageAboutItems.map(extractUpdatedPageAbout)
            .filter(m => m !== undefined) as PageAbout[];

        return pageAboutClient.updatePageAboutList(newPageAboutsToSave).then(transformPageAboutsToUiAbouts)
    }

    return <AboutPanel
        userCanEdit={props.userCanEdit}
        brand={props.brand}
        sampleSizeDescription={props.sampleSizeDescription}
        visible={props.visible}
        getAbouts={getPageAboutRecords}
        addAbout={addPageAboutRecord}
        updateAbouts={saveChanges}
        deleteAbout={deletePageAboutItem}
    />;
}

export default PageAboutPanel;