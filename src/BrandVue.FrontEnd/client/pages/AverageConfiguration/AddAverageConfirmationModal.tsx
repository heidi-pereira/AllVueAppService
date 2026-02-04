import { AverageConfiguration } from "client/BrandVueApi";
import Throbber from "client/components/throbber/Throbber";
import React from "react";
import { Modal, ModalBody } from "reactstrap";
import style from './AddAverageConfirmationModal.module.less';

interface IProps {
    isOpen: boolean;
    average: AverageConfiguration;
    add(options: AverageConfirmationOption[]): void;
    cancel(): void;
}

export enum AverageConfirmationOption {
    Weighted,
    Unweighted
};

const AddAverageConfirmationModal = (props: IProps) => {
    const [hasBeenClicked, setHasBeenClicked] = React.useState(false);
    const [options, setOptions] = React.useState<AverageConfirmationOption[]>([AverageConfirmationOption.Weighted, AverageConfirmationOption.Unweighted]);

    const toggleOption = (option: AverageConfirmationOption) => {
        const newOptions = [...options];
        const index = newOptions.indexOf(option);
        if (index >= 0) {
            newOptions.splice(index, 1);
        } else {
            newOptions.push(option);
        }
        setOptions(newOptions);
    };

    const isOptionSelected = (option: AverageConfirmationOption) => options.includes(option);

    const getContent = () => {
        const checkboxOptions = [AverageConfirmationOption.Weighted, AverageConfirmationOption.Unweighted];
        return (
            <div>
                <div className={style.row}>
                    {checkboxOptions.map(option => {
                        const optionName = AverageConfirmationOption[option.toString()];
                        return (
                            <div key={optionName}>
                                <input type="checkbox" className="checkbox" id={optionName} checked={isOptionSelected(option)} onChange={() => toggleOption(option)} />
                                <label htmlFor={optionName}>{optionName}</label>
                            </div>
                        );
                    })}
                </div>
                <p className={style.hint}>Only weighted or unweighted averages will be shown in each report depending on whether weighting is configured and enabled for that report.</p>
            </div>
        );
    };

    const addAverage = () => {
        setHasBeenClicked(true);
        props.add(options);
    };

    const getThrobber = () => {
        return(
            <div className="throbber">
                <Throbber />
            </div>
        )
    };

    return (
        <Modal isOpen={props.isOpen} centered={true} className="delete-modal" autoFocus={false}>
            <h3>Add {props.average.displayName} average?</h3>
            <ModalBody>
                {hasBeenClicked ? getThrobber() : getContent()}
                <div className="button-container">
                    <button onClick={props.cancel} className="secondary-button" autoFocus={true}>Cancel</button>
                    <button disabled={hasBeenClicked || options.length === 0} onClick={addAverage} className="primary-button">Add</button>
                </div>
            </ModalBody>
        </Modal>
    );
}
export default AddAverageConfirmationModal;