import React from "react";
import style from "./MetricInsights.module.less";
import { updateSegmentCorrectness } from "../state/llmInsightSlice";
import { useAppDispatch } from "../state/store";
import { LlmInsight } from "../BrandVueApi";

export interface CorrectnessFeedbackSectionProps {
    id: string,
    insight: LlmInsight
}

export const CorrectnessFeedbackSection = (props: CorrectnessFeedbackSectionProps) => {
    const dispatch = useAppDispatch();
    const [userFeedbackSegmentCorrectness, setUserFeedbackSegmentCorrectness] = React.useState(props.insight.userFeedbackSegmentCorrectness);
    const [backButtonPressed, setBackButtonPressed] = React.useState(true);

    const CorrectnessFeedbackPrompt = ({ id, segmentId, correctness }) => {
        const handleCorrectnessFeedback = (id: string, segmentId: number, isCorrect: boolean) => {
            dispatch(updateSegmentCorrectness({ id, segmentId, isCorrect }));
            setUserFeedbackSegmentCorrectness(isCorrect);
            setBackButtonPressed(false);
        }
        return (
            <div className={style.userFeedbackCorrectness}>
                Does this reflect the data correctly?
                <button className={correctness == false ? style.buttonClicked : ""} onClick={() => handleCorrectnessFeedback(id, segmentId, false)}>
                    <i className="material-symbols-outlined">close</i>
                </button>
                <button className={correctness == true ? style.buttonClicked : ""} onClick={() => handleCorrectnessFeedback(id, segmentId, true)}>
                    <i className="material-symbols-outlined">check</i>
                </button>
            </div>
        );
    };
    
    const CorrectnessFeedbackReceived = ({ correctness }) => (
        <div className={style.userFeedbackCorrectness}>
            <div>
                {correctness ? <p>Thanks for your feedback!</p>
                    : <p>We are still testing this new feature, if it doesn't look quite right <b>please contact your account manager</b> for further details.</p>
                }
            </div>
            <div>
                <button className={style.goBackButton} onClick={() => setBackButtonPressed(true)}>Go back</button>
            </div>
        </div>
    )

    if (userFeedbackSegmentCorrectness == null || backButtonPressed) {
        return <CorrectnessFeedbackPrompt id={props.id} segmentId={props.insight.segmentId} correctness={userFeedbackSegmentCorrectness} />
    }
    return <CorrectnessFeedbackReceived correctness={userFeedbackSegmentCorrectness} />
}