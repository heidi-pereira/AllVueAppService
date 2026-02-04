import React from 'react';
import { useLocalStorage } from '../helpers/LocalStorageContext';

interface IProps {
    id: string;
    message: React.ReactElement;
    materialIconName: string;
    isClosable? : boolean;
}

const NotificationBanner = (props: IProps) => {
    const DoNotShowAgain = 'doNotShowAgain';
    const [isMessageHidden, setIsMessageHiden] = useLocalStorage(props.id, undefined);

    function doNotShowAgain(){
        setIsMessageHiden(props.id, DoNotShowAgain);
    }

    return (
        <>
            {!isMessageHidden && <div className='notification-banner' >
                <div className="icon">
                    <i className='material-symbols-outlined'>{props.materialIconName}</i>
                </div>
                <div className="message">
                    {props.message}
                </div>
                {props.isClosable && <div className="remove-button" onClick={() => doNotShowAgain()}>
                    <i className="material-symbols-outlined close">close</i>
                </div>}
            </div>}
        </>
    );
}

export default NotificationBanner;