import React from "react";
import gravatar from 'gravatar';
import styles from './AccountOptions.module.scss';
import DropDownMenu from "./DropDownMenu";
import { IDropDownMenuItem } from "../Types/IDropDownMenuItem";

interface IAccountOptionsProps {
    username: string;
    menuItems?: Array<IDropDownMenuItem>;
}

function useOutsideClick(ref: HTMLDivElement | null, onClickOut: () => void) {
    console.log("useOutsideClick", ref);
    const onClick = ({ target }: any) => !ref?.contains(target) && onClickOut?.()
    document.addEventListener("click", onClick);
    return () => document.removeEventListener("click", onClick);
}

const AccountOptions = (props: IAccountOptionsProps) => {
    const [isGravatarImageValid, setIsGravatarImageValid] = React.useState(true);
    const [dropDownOpen, setDropDownOpen] = React.useState(false);

    const ref = React.useRef<HTMLDivElement>(null);

    React.useEffect(() => {
        useOutsideClick(ref.current, () => {
            setDropDownOpen(false);
        });
    }, [ref.current]);

    const openCloseDropDown = () => {
        setDropDownOpen(!dropDownOpen);
    };

    const handleImage404Error = () => {
        setIsGravatarImageValid(false);
    };

    return (
        <div ref={ref} className={styles["account-options"]}>
            <div className="dropdown">
                <div>
                    <button className={ styles["nav-button"]} onClick={openCloseDropDown} type="button" aria-haspopup="true" aria-expanded="false" title="My Account">
                        <div className={styles["circular-nav-button"]}>
                            <div className={styles["circle"]}>
                                {(!props.username || !isGravatarImageValid) &&
                                    <i className="material-symbols-outlined">account_circle</i>
                                }
                                {(props.username && isGravatarImageValid) &&
                                    <img
                                        src={gravatar.url(props.username, { s: '32', r: 'g', d: '404' }, true)}
                                        onError={() => {
                                            handleImage404Error();
                                        }}
                                    />
                                }
                            </div>
                            <div className={styles["text"]}>My Account</div>
                        </div>
                    </button>
                    {dropDownOpen && 
                        <div className={styles["dropdownContainer"]}>
                        <DropDownMenu username={props.username} menuItems={props.menuItems} track={props.track}></DropDownMenu>
                        </div>
                    }
                </div>
            </div>
        </div>
    );
}

export default AccountOptions