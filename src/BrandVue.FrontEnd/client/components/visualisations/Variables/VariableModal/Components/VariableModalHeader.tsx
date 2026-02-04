import React from "react";
import { ModalContent } from "../VariableContentModal";
import DeleteVariableModal from "./DeleteVariableModal";
import { useContext } from "react";
import { VariableContext } from "../Utils/VariableContext";
import BaseModalHeader from "./BaseModalHeader";
interface IVariableModalHeaderProps {
    title: string,
    content: ModalContent,
    flattenMultiEntity: boolean;
    goBackHandler: () => void;
    closeHandler: () => void;
    canGoBack?: boolean;
    variableId?: number;
    isBase?: boolean;
    isDeleteButtonHidden?: boolean;
}

const VariableModalHeader = (props: IVariableModalHeaderProps) => {
    const { googleTagManager, pageHandler} = useContext(VariableContext);
    const [isDeleteModalOpen, setIsDeleteModalOpen] = React.useState<boolean>(false);
    const canShowDelete: boolean | undefined =
        !!props.variableId && (!props.isDeleteButtonHidden);

    const getTitleText = () => {
        let titleText = "";
        if (props.flattenMultiEntity) {
            titleText = "Convert " + props.title + " into multiple variables by row";
        } else if (props.variableId) {
            titleText = "Edit " + props.title;
        } else {
            titleText = "Create new " + props.title;
        }
        return titleText;
    }

    const closeAllModals = () => {
        setIsDeleteModalOpen(false);
        props.closeHandler();
    }

    return (
        <>
            <BaseModalHeader canGoBack={props.canGoBack}
                canShowDelete={canShowDelete}
                setIsDeleteModalOpen={setIsDeleteModalOpen}
                isBase={props.isBase}
                closeHandler={props.closeHandler}
                goBackHandler={props.goBackHandler}
                title={getTitleText()}
            />
            <DeleteVariableModal
                isOpen={isDeleteModalOpen}
                variableId={props.variableId}
                variableName={props.title}
                googleTagManager={googleTagManager}
                pageHandler={pageHandler}
                closeModal={() => setIsDeleteModalOpen(false)}
                isBase={props.isBase}
                closeAllModals={closeAllModals}
            />
        </>
    );
}

export default VariableModalHeader