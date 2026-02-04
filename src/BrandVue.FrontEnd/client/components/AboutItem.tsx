import React from "react";
import TextareaAutosize from "react-autosize-textarea";
import { MetricAbout, PageAbout } from "../BrandVueApi";
import { DynamicTextHelper } from "./helpers/DynamicTextHelper";
import DeleteModal from "./DeleteModal";
import style from "./About.module.less";
import { useReadVueQueryParams } from "./helpers/UrlHelper";

interface IAboutItemProps {
    aboutItem: UiAboutItem;
    userCanEdit: boolean;
    isEditing: boolean;
    setIsEditing(about: UiAboutItem): void;
    updateAboutItem(about: UiAboutItem): void;
    deleteAboutItem(about: UiAboutItem): void;
    dynamicTextHelper: DynamicTextHelper;
}

export type UiAboutItem = {
    about: MetricAbout | PageAbout,
    displayTitle: string,
    displayContent: string,
    originalTitle: string,
    originalContent: string
}

const AboutItem: React.FunctionComponent<IAboutItemProps> = (props) => {
    const [deleteConfirmationModalVisible, setDeleteConfirmationModalVisible] = React.useState(false);
    const hasUnsavedChanges = props.aboutItem.displayTitle !== props.aboutItem.originalTitle
        || props.aboutItem.displayContent !== props.aboutItem.originalContent;

    const onChangeTitleText = (aboutItem: UiAboutItem, newTitle: string) => {
        const newAboutItem = { ...aboutItem, displayTitle: newTitle };
        
        props.updateAboutItem(newAboutItem);
    }

    const onChangeContentText = (aboutItem: UiAboutItem, newContent: string) => {
        const newAboutItem = { ...aboutItem, displayContent: newContent };
        
        props.updateAboutItem(newAboutItem);
    }

    const key = `tb-${props.aboutItem.about.id}`;

    const aboutType = props.aboutItem.about instanceof MetricAbout ? "metric about" : "page about";

    const aboutItemEditable = () => {
        return (
            <div key={key} className={`${style.textBlock} ${props.isEditing ? style.editing : ""}`}>
                <DeleteModal
                    isOpen={deleteConfirmationModalVisible}
                    thingToBeDeletedName={aboutType}
                    thingToBeDeletedType={aboutType}
                    delete={() => props.deleteAboutItem(props.aboutItem)}
                    closeModal={() => setDeleteConfirmationModalVisible(false)}
                    affectAllUsers
                />
                <div className={style.header}>
                <TextareaAutosize key={`${key}-title`} placeholder="New About Information" className={`${style.aboutTitle} title`} value={props.isEditing ? props.aboutItem.displayTitle : props.dynamicTextHelper.replaceText(props.aboutItem.displayTitle)} readOnly={!props.isEditing} onChange={e => onChangeTitleText(props.aboutItem, e.currentTarget.value)} onPointerEnterCapture={undefined} onPointerLeaveCapture={undefined}></TextareaAutosize>
                {props.userCanEdit && props.aboutItem.about.editable &&
                    <div className={style.buttons}>
                        <div className={style.notifications}>
                            <div className={`${style.notificationsUnsaved} ${hasUnsavedChanges && style.visible}`} title="Unsaved changes"><i className="material-symbols-outlined">save</i></div>
                        </div>
                        <div className={style.actions}>
                            <div title="Edit text" onClick={e => props.setIsEditing(props.aboutItem)}>
                                <i className="material-symbols-outlined">edit</i>
                            </div>
                            <div title="Delete" onClick={e => setDeleteConfirmationModalVisible(true)}>
                                <i className="material-symbols-outlined">delete</i>
                            </div>
                        </div>
                    </div>
                }
                </div>
                <TextareaAutosize key={`${key}-content`} placeholder="New About Information" className={style.content} value={props.isEditing ? props.aboutItem.displayContent : props.dynamicTextHelper.replaceText(props.aboutItem.displayContent)} readOnly={!props.isEditing} onChange={e => onChangeContentText(props.aboutItem, e.currentTarget.value)} onPointerEnterCapture={undefined} onPointerLeaveCapture={undefined}></TextareaAutosize>
            </div>
            );
    };

    const aboutItemDisplayOnly = () => {
        const readVueQueryParams = useReadVueQueryParams();
        return (
            <div key={key} className={style.textBlock}>
                <div className={style.header}>
                    <div key={`${key}-title`} className={`${style.aboutTitle} title`}>{props.dynamicTextHelper.replaceTextWithJSX(props.aboutItem.displayTitle, readVueQueryParams)}</div>
                </div>
                <div key={`${key}-content`} className={style.content}>{props.dynamicTextHelper.replaceTextWithJSX(props.aboutItem.displayContent, readVueQueryParams)}</div>
            </div>
        );
}

    return props.aboutItem.about.editable ? aboutItemEditable() : aboutItemDisplayOnly();
}

export default AboutItem;