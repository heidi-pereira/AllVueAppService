import React from "react"

export const CategoryComparisonPlaceholder: React.FunctionComponent = () => {
    return (
        <div className="category-comparison-placeholder">

            {[1, 2, 3].map(n => {
                return (
                    <div key={n} className="metric-placeholder">
                        <div className="metric">
                            <div className="score"/>
                            <div className="metric-label"/>
                        </div>
                        <div className={`chart-${n}`}>
                            <div className="top-bar"/>
                            <div className="bottom-bar"/>
                        </div>
                    </div>
                )
            })}
            <div className="inline-legend">
                <div className="legend-container">
                    <div>
                        <div className="legend-icon"/>
                        <div className="entity"/>
                    </div> 
                    <div>
                        <div className="legend-icon"/>
                        <div className="average"/>
                    </div>
                </div>
                <div className="legend-footer"/>
            </div>
        </div>
    )
};