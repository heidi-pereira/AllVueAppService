import React from "react";

interface BaseModalButtonsProps {
  secondaryButtonAction: () => void;
  secondaryButtonName: string;
  primaryButtonAction: () => void;
  primaryButtonName: string;
  isShown: boolean;
  primaryDisabledReason?: string;
}

const BaseModalButtons: React.FC<BaseModalButtonsProps> = (props) => {
  return (
    <div className="modal-buttons">
      <button className="modal-button secondary-button" onClick={props.secondaryButtonAction}>{props.secondaryButtonName}</button>
      {props.isShown &&
        <button className="modal-button primary-button" onClick={props.primaryButtonAction} disabled={!!props.primaryDisabledReason} title={props.primaryDisabledReason}>{props.primaryButtonName}</button>
      }
    </div>
  );
}

export default BaseModalButtons;