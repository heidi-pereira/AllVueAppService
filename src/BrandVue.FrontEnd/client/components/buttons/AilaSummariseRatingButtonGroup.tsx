import style from './AilaSummariseRatingButtonGroup.module.less';
import React from 'react';
import { MixPanel } from '../mixpanel/MixPanel';
import MultiPageModal from '../MultiPageModal';
import ModalPage from '../ModalPage';

interface IAilaSummariseRatingButtonGroup {
    disabled?: boolean;
    onFeedback?: () => void;
}

const AilaSummariseRatingButtonGroup: React.FunctionComponent<IAilaSummariseRatingButtonGroup> = ({ disabled, onFeedback }) => {
    const [modal, setModal] = React.useState(false);
    const [feedback, setFeedback] = React.useState('');

    const handleRating = (isPositive: boolean) => {
        if (isPositive) {
            MixPanel.track("aiSummariseRatingThumbsUp");
        }
        else {
            MixPanel.track("aiSummariseRatingThumbsDown");
        }
        if (onFeedback) onFeedback();
        setModal(true);
    };

    const handleFeedbackChange = (event: React.ChangeEvent<HTMLTextAreaElement>) => {
        setFeedback(event.target.value);
    };

    const handleSubmitFeedback = () => {
        MixPanel.track("aiSummariseFeedbackSubmit", { Feedback: feedback,  });
        setModal(false);
    };

    return (
        <>
            <div className={style.ratingButtonsContainer}>
                <div className={style.ratingButtons}>
                    <div className={style.ratingButtonsMessage}>Was this summary helpful?</div>
                    <button onClick={() => handleRating(true)} className="hollow-button" disabled={disabled}>
                        <i className="material-symbols-outlined">thumb_up</i>
                    </button>
                    <button onClick={() => handleRating(false)} className="hollow-button" disabled={disabled}>
                        <i className="material-symbols-outlined">thumb_down</i>
                    </button>
                </div>
                <MultiPageModal
                    isOpen={modal}
                    setIsOpen={setModal}
                    header="Share Your Thoughts"
                >
                    <ModalPage
                        className={style.feedbackModal}
                        actionButtonCss="hollow-button"
                        actionButtonText="Send"
                        actionButtonHandler={handleSubmitFeedback}
                        cancelButtonText="Close"
                        cancelButtonCss="secondary-button"
                    >
                        <p>Thank you for your feedback! We appreciate your input. Feel free to share more details about your experience or any additional features you'd like to see. Your insights help us improve.</p>
                        <textarea
                            className={style.feedbackModalTextarea}
                            value={feedback}
                            onChange={handleFeedbackChange}
                            placeholder="Please provide your feedback..."
                        />
                    </ModalPage>
                </MultiPageModal>
            </div>
        </>
    );
};

export default AilaSummariseRatingButtonGroup;