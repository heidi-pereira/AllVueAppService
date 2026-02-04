import React from "react";
import * as BrandVueApi from "../BrandVueApi";
import { toast } from 'react-hot-toast';
import AboutItem, { UiAboutItem } from "./AboutItem";
import style from "./About.module.less";
import { DynamicTextHelper } from "./helpers/DynamicTextHelper";
import Throbber from "./throbber/Throbber";
import Tooltip from "./Tooltip";
import SidePanelHeader from "./SidePanelHeader";

interface IAboutPanelProps {
    userCanEdit: boolean;
    brand: string | undefined;
    sampleSizeDescription: string;
    visible: boolean;
    getAbouts(): Promise<UiAboutItem[]>;
    addAbout(): Promise<UiAboutItem>;
    updateAbouts(aboutItems: UiAboutItem[]): Promise<UiAboutItem[]>;
    deleteAbout(aboutItem: UiAboutItem): Promise<BrandVueApi.HttpStatusCode>;
}

const AboutPanel: React.FunctionComponent<IAboutPanelProps> = (props) => {
    const [aboutItems, setAboutItems] = React.useState<UiAboutItem[]>([]);
    const [currentlyEditingAboutItem, setCurrentlyEditingAboutItem] = React.useState<UiAboutItem>();
    const [addInformationPanelOpen, setAddInformationPanelOpen] = React.useState(false);
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const toastError = (userFriendlyText: string) => {
        toast.error(userFriendlyText);
    };
    const toastSuccess = (userFriendlyText: string) => {
        toast.success(userFriendlyText);
    };

    const getTooltip = () => {
        return (
            <div className="brandvue-tooltip">
                <div className="tooltip-header">For dynamic text</div>
                <div className="tooltip-label">Brand:</div>
                <div className="tooltip-value">[BRAND]</div>
                <div className="tooltip-label">Sample size:</div>
                <div className="tooltip-value">[N]</div>
            </div>
        );
    }

    const getAboutItems = () => {
        setIsLoading(true);
        props.getAbouts()
            .then(a => setAboutItems(a))
            .then(e => setIsLoading(false));
    }

    const dynamicTextHelper = new DynamicTextHelper(props.brand, props.sampleSizeDescription);

    const addAboutItem = () => {
        props.addAbout()
            .then(a => setAboutItems(aboutItems.concat(a)))
            .catch((e: Error) => toastError("An error occurred trying to add a new About block"));
    }

    const deleteAboutItem = (aboutItem: UiAboutItem) => {
        props.deleteAbout(aboutItem)
            .then(() => setAboutItems(aboutItems.filter(m => m !== aboutItem)))
            .then(() => toastSuccess("About block deleted"))
            .catch((e: Error) => toastError("An error occurred trying to delete the About block"));
    }

    React.useEffect(() => {
        getAboutItems();
    }, []);

    React.useEffect(() => {
        if (!props.visible) {
            setAddInformationPanelOpen(false);
        }
    },
        [props.visible]);

    if (isLoading) {
        return (
            <div className="throbber-container-fixed">
                <Throbber />
            </div>
        );
    }

    const updateAboutItem = (aboutItem: UiAboutItem) => {
        const newAboutItems = aboutItems.map(a => a.about.id === aboutItem.about.id ? aboutItem : a);

        setAboutItems(newAboutItems);
    }

    const unsavedChangesInAboutItem = (aboutItem: UiAboutItem) => {
        return aboutItem.originalTitle !== aboutItem.displayTitle ||
            aboutItem.originalContent !== aboutItem.displayContent;
    }

    const unsavedChangesInAboutItems = (aboutItems: UiAboutItem[]) => {
        return aboutItems.some(unsavedChangesInAboutItem);
    }

    const getSaveButtons = () => {
        const unsavedChanges = unsavedChangesInAboutItems(aboutItems);
        return (
            <div className={style.controlContainer}>
                <div className={style.controlButtons}>
                    <button className="modal-button primary-button" disabled={!unsavedChanges} onClick={() =>
                        saveChanges(aboutItems)}>Save changes</button>
                    <button className="modal-button secondary-button" disabled={!unsavedChanges} onClick={() =>
                        discardChanges(aboutItems)}>Cancel</button>
                </div>
                <Tooltip placement="top" title={getTooltip()}>
                    <i className={`${style.dynamicTooltip} material-symbols-outlined`}>help</i>
                </Tooltip>
            </div>);
    }

    const resetAboutItem = (aboutItem: UiAboutItem): UiAboutItem => {
        const newAboutItem = { ...aboutItem };
        newAboutItem.displayTitle = newAboutItem.about.aboutTitle;
        newAboutItem.displayContent = newAboutItem.about.aboutContent;

        return newAboutItem;
    }

    const saveChanges = (aboutItems: UiAboutItem[]) => {
        aboutItems = aboutItems.filter((a: UiAboutItem) => a.about.editable);
        setCurrentlyEditingAboutItem(undefined);
        props.updateAbouts(aboutItems)
            .then(() => toastSuccess("Saved successfully"))
            .catch((e: Error) => toastError("An error occurred trying to update the About block"));
    }

    const discardChanges = (aboutItemList: UiAboutItem[]) => {
        const newAboutItems = aboutItemList.map(m => unsavedChangesInAboutItem(m) ? resetAboutItem(m) : m);

        setAboutItems(newAboutItems);
        toastSuccess("Changes discarded");
    }

    const getItem = (aboutItem: UiAboutItem) => {
        return <AboutItem
            key={aboutItem.about.id}
            aboutItem={aboutItem}
            userCanEdit={props.userCanEdit}
            isEditing={currentlyEditingAboutItem?.about.id === aboutItem.about.id}
            setIsEditing={setCurrentlyEditingAboutItem}
            updateAboutItem={updateAboutItem}
            deleteAboutItem={deleteAboutItem}
            dynamicTextHelper={dynamicTextHelper}
        />;
    }

    const getMainAboutItem = (aboutItem: UiAboutItem) => {
        if (aboutItem.about.aboutContent.length > 0) {
            return getItem(aboutItem);
        }
    }

    const getInfoAboutItem = (aboutItem: UiAboutItem) => {
        if (aboutItem.about.aboutContent.length === 0) {
            return getItem(aboutItem);
        }
    }

    const getMainAboutItems = aboutItems.map(getMainAboutItem);

    const aboutItemsShown = getMainAboutItems.filter(m => m !== undefined).length !== 0;

    return (
        <>
            <div className={`${style.selectorMain} ${addInformationPanelOpen ? style.hide : ""}`}>
                <div className={`${style.selectorMainContent} ${addInformationPanelOpen ? style.hide : ""}`}>
                    {!aboutItemsShown && <div className={style.noAboutPlaceholder}>
                        No information
                    </div>}
                    {aboutItemsShown && <div className={style.textBlockList}>
                        {getMainAboutItems}
                    </div>}
                    {props.userCanEdit && getSaveButtons()}
                    <div>
                        {props.userCanEdit && <button className="button primary-button add-information-button" onClick={() =>
                            setAddInformationPanelOpen(true)}>+ Add information</button>}
                    </div>
                </div>
            </div>
            <div className={`${style.addInformationPanel} ${addInformationPanelOpen ? style.visible : ""}`}>
                <SidePanelHeader returnButtonHandler={() => setAddInformationPanelOpen(false)}>Add information</SidePanelHeader>
                <div className={style.textBlockList}>
                    {aboutItems.map(getInfoAboutItem)}
                </div>
                <div>
                    <button type="button" className="hollow-button" onClick={addAboutItem}>Add</button>
                    {getSaveButtons()}
                </div>
            </div>
        </>
    );
}

export default AboutPanel