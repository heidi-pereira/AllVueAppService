import React from 'react';
import {useLocation} from "react-router";

export const ScrollableContainer = ((props: React.HTMLAttributes<HTMLDivElement> & React.DOMAttributes<HTMLDivElement>) => {
    const divRef = React.useRef<HTMLDivElement>();
    const location = useLocation();
    
    const { children, ...divAttributes } = props;

    React.useEffect(() => {
        if (divRef.current) {
            divRef.current.scrollTop = 0;
        }
    }, [location]);

    return (
        <div ref={divRef} {...divAttributes}>
            {children}
        </div>
    );
});
