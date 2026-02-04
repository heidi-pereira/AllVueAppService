import React from "react";
import { updateUsefulness, noMoreFeedback, updateUserComment } from "../state/llmInsightSlice";
import { useAppDispatch, useAppSelector } from "../state/store";
import { LlmInsightResults } from "../BrandVueApi";
import LlmUserFeedbackModal from "./LlmUserFeedbackModal";


export interface UsefulnessFeedbackSectionProps {
    results: LlmInsightResults | null
}

export const UsefulnessFeedbackSection = () => {
    const dispatch = useAppDispatch();
    const [usefulnessLastSelected, setUsefulnessLastSelected] = React.useState<boolean | undefined>(undefined);

    const results = useAppSelector(state => state.llmInsights.results);
    const userFeedback = results?.userFeedback;

    React.useEffect(() => {
        setUsefulnessLastSelected(userFeedback?.isUseful);
    },[]);

    const UsefulnessFeedbackPrompt = ({ id }) => {
        const handleUsefulnessFeedback = (id: string, isUseful: boolean) => {
            dispatch(updateUsefulness({ id, isUseful }));
            dispatch(updateUserComment({ id: id, userComment: "" }));
            setUsefulnessLastSelected(isUseful);
        }
        return (
            <div className='user-feedback-helpfulness'>
                <div>
                    <span className='title'>Enhance your experience with feedback!</span><br />
                    Was this summary helpful?
                </div>
                <div className='button-group'>
                    <button className={usefulnessLastSelected == false ? "button-clicked" : ""} onClick={() => handleUsefulnessFeedback(id, false)}>
                        <i className="material-symbols-outlined">thumb_down</i>
                    </button>
                    <button className={usefulnessLastSelected == true ? "button-clicked" : ""} onClick={() => handleUsefulnessFeedback(id, true)}>
                        <i className="material-symbols-outlined">thumb_up</i>
                    </button>
                </div>
            </div>
        );
    };

    const UsefulnessFeedbackReceived = ({ id }) => {
        const [userFeedbackModalVisible, setUserFeedbackModalVisible] = React.useState(false);
        const handleSubmitUserComment = (feedbackText: string) => {
            dispatch(updateUserComment({ id: id, userComment: feedbackText }));
            setUserFeedbackModalVisible(false);
        }     
        return (
            <div className='user-feedback-helpfulness'>
                <LlmUserFeedbackModal
                        isOpen={userFeedbackModalVisible}
                        submit={(feedbackText) => handleSubmitUserComment(feedbackText)}
                        closeModal={() => setUserFeedbackModalVisible(false)}
                    />
                <div>
                    <button className='back-button' onClick={() => dispatch(noMoreFeedback())}>
                        <i className="material-symbols-outlined">chevron_left</i>
                    </button>
                </div>
                <div>
                    Tell us what you think, we're listening!
                </div>
                <div>
                    <button className='feedback-button' onClick={() => setUserFeedbackModalVisible(true)}>
                        Leave feedback
                    </button>
                </div>
            </div>
        );
    };

    const UserCommentReceived = () => {
        return (
            (<div className='user-feedback-helpfulness'>
                <div>
                    <span className='title'>Thanks! Your thoughts inspire our improvements.</span>
                </div>
                <div>
                    <button className='close-button' onClick={() => dispatch(noMoreFeedback())}>
                        <i className="material-symbols-outlined">close</i>
                    </button>
                </div>
            </div>)
        );
    };

    if (!results) {
        return <div />;
    }
    if (userFeedback?.isUseful == null) {
        return <UsefulnessFeedbackPrompt id={results.id}/>;
    }
    if (!userFeedback?.userComment) {
        return <UsefulnessFeedbackReceived id={results.id}/>;
    }
    return <UserCommentReceived />;
}
