import React from "react";
import { Modal, ModalHeader, ModalBody, Form, FormGroup, Label, Input } from "reactstrap";
import Throbber from "./throbber/Throbber";

interface IProps {
    isOpen: boolean;
    submit(feedback: string): void;
    closeModal: (deleted: boolean) => void;
}

const LlmUserFeedbackModal = (props: IProps) => {
    const [hasBeenClicked, setHasBeenClicked] = React.useState(false);
    const [feedbackText, setFeedbackText] = React.useState("");

    const getContent = () => {
        return (
            <>
                <p className="text">Your feedback is important to us to provide you with the optimum experience. Feel free to share any of your thoughts.</p >
                <FormGroup>
                    <Label for="userComment" className="feedback-label">Additional Feedback</Label>
                    <Input id="userComment" name="userComment" type="textarea" placeholder="Placeholder" value={feedbackText} onChange={(e) => setFeedbackText(e.target.value)} />
                </FormGroup>
            </>
        )
    }

    const getThrobber = () => {
        return (
            <div className="throbber">
                <Throbber />
            </div>
        )
    }

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        setHasBeenClicked(true);
        props.submit(feedbackText);
    };

    const handleCancel = (e: React.MouseEvent) => {
        e.preventDefault();
        setHasBeenClicked(false);
        props.closeModal(false);
    };

    const closeButton = (
        <button className="btn btn-close" type="button"></button>
    );

    return (
        <Modal isOpen={props.isOpen} centered={true} className="user-feedback-modal" autoFocus={false} toggle={handleCancel}>
            <ModalHeader className="feedback-header mb-1" toggle={handleCancel} close={closeButton}>
                <h3 className="p-0 m-0">Leave feedback</h3>
            </ModalHeader>
            <ModalBody>
                <Form onSubmit={handleSubmit}>
                    {hasBeenClicked ? getThrobber() : getContent()}
                    <div className="button-container">
                        <button type="button" className="secondary-button" onClick={handleCancel} autoFocus={true}>Cancel</button>
                        <button type="submit" className="primary-button" disabled={hasBeenClicked || !feedbackText.trim()}>Submit feedback</button>
                    </div>
                </Form>
            </ModalBody>
        </Modal>
    );
}
export default LlmUserFeedbackModal;