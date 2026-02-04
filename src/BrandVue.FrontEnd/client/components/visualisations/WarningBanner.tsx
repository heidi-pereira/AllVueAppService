import React from 'react';

interface IProps {
    message: any;
    materialIconName: string;
    isClosable? : boolean;
    isFreestanding?: boolean
}

const WarningBanner = (props: IProps) => {
    const [isOpen, setIsOpen] = React.useState(true);
    
    return (
        <>
            {isOpen && <div className={`warning-banner ${props.isFreestanding ? "warning-banner-freestanding" : ""}`} >
                <i className='material-symbols-outlined'>{props.materialIconName}</i>
                <div className="message">
                    {props.message}
                </div>
                {props.isClosable && <div className="remove-button" onClick={() => setIsOpen(false)}>
                    <i className="material-symbols-outlined">close</i>
                </div>}
            </div>}
        </>
    );
}

export default WarningBanner;