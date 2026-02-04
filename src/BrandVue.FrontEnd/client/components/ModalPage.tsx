import React from "react";

export interface IModalPage {
    children?: React.ReactNode
    className?: string
    subHeader?: string
    actionButtonText?: string
    actionButtonCss?: string
    actionButtonHandler?: () => void
    setActionButtonIsDisabled?: (boolean) =>  void
    actionButtonIsDisabled?: boolean
    cancelButtonText?: string
    cancelButtonCss?: string
    cancelButtonHandler?: () => void
}

const ModalPage = (props: IModalPage) => {
    React.useEffect(()=> {
        if (props.setActionButtonIsDisabled){
            props.setActionButtonIsDisabled(props.actionButtonIsDisabled)
        }
    }, [props.actionButtonIsDisabled])
    
    return (
        <div className={props.className}>
            {props.children}
        </div>
    );
}

export default ModalPage