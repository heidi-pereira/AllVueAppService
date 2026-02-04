import React from 'react';
import {debounce} from 'lodash';

interface IUseOnOffScreenParams {
    ref,
    initialState?: boolean,
    useDebounce?: boolean,
    disconnectAfterOnScreen?: boolean,
}

// https://developer.mozilla.org/en-US/docs/Web/API/Intersection_Observer_API

export default function useOnOffScreen({ref, initialState, useDebounce, disconnectAfterOnScreen}: IUseOnOffScreenParams) {
    //initialState is useful incase you want the element using this to have some style dependent on it being on/off screen
    //      e.g. animating on/off screen, you may want it to start as on/off screen so it doesn't flash on then off when you load the page
    //disconnectAfterOnScreen is for lazy loading where you don't care if it goes off screen again
    const [isOnScreen, setIsOnScreen] = React.useState<boolean>(initialState ?? false);

    const checkOnScreen = (entry: IntersectionObserverEntry) => {
        if (entry.isIntersecting) {
            setIsOnScreen(true);
            if (disconnectAfterOnScreen) {
                observer.disconnect();
            }
        } else {
            setIsOnScreen(false);
        }
    }
    const debouncedCheckOnScreen = debounce((entry: IntersectionObserverEntry) => checkOnScreen(entry), 200);

    const observer = React.useMemo(() => {
        return new IntersectionObserver(([entry]) => {
            if (useDebounce) {
                debouncedCheckOnScreen(entry);
            } else {
                checkOnScreen(entry);
            }
        });
    }, []);

    React.useEffect(() => {
        if (ref.current) {
            observer.observe(ref.current);
        }
        return () => { observer.disconnect() };
    }, [ref.current]);

    return isOnScreen;
}