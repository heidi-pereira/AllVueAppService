import { MixPanel } from './mixpanel/MixPanel';
import React, { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

const WithPerformanceMeasurement = (WrappedComponent: React.ComponentType<any>, page: string) => {
    return (props: any) => {
        const location = useLocation();

        useEffect(() => {
            const perfObserver = new PerformanceObserver((observedEntries) => {
                const entry: PerformanceEntry =
                    observedEntries.getEntriesByType('navigation')[0]
                MixPanel.trackPageLoadTime(entry.duration, page);
            })

            perfObserver.observe({
                type: 'navigation',
                buffered: true
            })
        }, [location])

        return <WrappedComponent {...props} />;
    };
};

export default WithPerformanceMeasurement;
