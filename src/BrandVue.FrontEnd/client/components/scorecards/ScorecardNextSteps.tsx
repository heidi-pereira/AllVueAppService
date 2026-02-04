import React from "react";
import { useNavigate } from 'react-router-dom';
import { useEffect } from "react";

const ScorecardNextSteps = (props: { nextSteps: string }) => {
    let element: HTMLDivElement | null = null;
    const navigate = useNavigate();

    useEffect(() => {
        if (element) {
            $(element).find('a').click((e) => {
                e.preventDefault();
                let url = (e.currentTarget as any).pathname;
                if (url.startsWith("/ui")) {
                    url = url.substring(3);
                }
                navigate(url);
            });
        }
    }, [element, navigate]);

    return (
        <div className="col subsection scorecardNextSteps">
            <header>Next steps</header>
            <div
                ref={el => element = el}
                dangerouslySetInnerHTML={{ __html: props.nextSteps }}
            />
        </div>
    );
}

export default ScorecardNextSteps;

export const willRenderNextStep = (text: string): boolean => {
    return text != null && text.length > 0;
}