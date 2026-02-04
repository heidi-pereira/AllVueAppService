import React from "react";
import {Modal, ModalBody} from 'reactstrap';
import {IModalPage} from "./ModalPage";
import {AriaRoles} from "../helpers/ReactTestingLibraryHelpers";

interface IMultiPageModalProps {
    isOpen: boolean
    setIsOpen: (isOpen: boolean) => void;
    header: string
    headerButtons?: React.ReactElement<HTMLButtonElement>[] | React.ReactElement<HTMLButtonElement>
    children?: React.ReactElement<IModalPage>[] | React.ReactElement<IModalPage>
}

const MultiPageModal = (props: IMultiPageModalProps) => {
    const [pageIndex, setPageIndex] = React.useState(0);
    const [actionButtonDisabled, setActionButtonDisabled] = React.useState(false);
    
    const getChildrenWithPropsArray = () => {
        const transformedChildren = React.Children.map(props.children, child => {
            if (React.isValidElement(child)) {
                return React.cloneElement(child, {setActionButtonIsDisabled: setActionButtonDisabled})
            }
        })?.filter(n => n)
        if (transformedChildren){
            return transformedChildren
        }
        return []
    }
    
    const childrenWithProps = getChildrenWithPropsArray()
    
    const getCurrentPage = () => {
        if (pageIndex < childrenWithProps.length) {
            return childrenWithProps[pageIndex]
        }
    }
    
    const getSubHeader = () => {
        let subHeader: string | undefined
        if (childrenWithProps.length > 1){
            subHeader =`${pageIndex + 1} of ${childrenWithProps.length}`
        }
        if (getCurrentPage()?.props.subHeader) {
            return subHeader ? `${subHeader}: ${getCurrentPage()?.props.subHeader}` : getCurrentPage()?.props.subHeader
        }
        return subHeader
    }
    
    const onCloseHandler = () => {
        setPageIndex(0)
        props.setIsOpen(false);
    }
    
    const actionButtonHandler = () => {
        const currentPage = getCurrentPage()
        if (currentPage && currentPage.props.actionButtonHandler) {
            currentPage.props.actionButtonHandler()
        }
        if (pageIndex < childrenWithProps.length - 1){
            setPageIndex(pageIndex + 1)
        }else {
            onCloseHandler()
        }
    }

    const cancelButtonHandler = () => {
        const currentPage = getCurrentPage()
        if (currentPage && currentPage.props.cancelButtonHandler) {
            currentPage.props.cancelButtonHandler()
        }
        if (pageIndex > 0){
            setPageIndex(pageIndex - 1)
        }else {
            onCloseHandler()
        }
    }
    
    const getActionButtonText = () => {
        const defaultText = pageIndex === childrenWithProps.length ? "Do action" : "Next"
        return getCurrentPage()?.props.actionButtonText ? getCurrentPage()?.props.actionButtonText : defaultText
    }
    
    const getCancelButtonText = () => {
        const defaultText = pageIndex === 0 ? "Cancel" : "Back"
        return getCurrentPage()?.props.cancelButtonText ? getCurrentPage()?.props.cancelButtonText : defaultText
    }
    
    return (
        <Modal isOpen={props.isOpen} toggle={onCloseHandler} centered backdrop="static" keyboard={false}
               autoFocus={false} className="entity-set-modal">
            <ModalBody>
                <div className="header-buttons">
                    {props.headerButtons}
                    <button onClick={onCloseHandler} className="btn btn-close" title="Close">
                      
                    </button>
                </div>
                <div role={AriaRoles.HEADER} className="header">
                    {props.header}
                    <div className="sub-header">
                        {getSubHeader()}
                    </div>
                </div>
                <div className="content">
                    {getCurrentPage()}
                </div>
                <div className="control-buttons">
                    <button className={`modal-button ${getCurrentPage()?.props.cancelButtonCss ? getCurrentPage()?.props.cancelButtonCss : "secondary-button"}`} onClick={cancelButtonHandler}>
                        {getCancelButtonText()}
                    </button>
                    <button className={`modal-button ${getCurrentPage()?.props.actionButtonCss ? getCurrentPage()?.props.actionButtonCss : "primary-button"}`} disabled={actionButtonDisabled} onClick={actionButtonHandler}>
                        {getActionButtonText()}
                    </button>
                </div>
            </ModalBody>
        </Modal>
    );
}

export default MultiPageModal;