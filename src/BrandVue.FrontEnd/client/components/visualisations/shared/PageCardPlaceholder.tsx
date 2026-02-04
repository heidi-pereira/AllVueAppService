import React from "react"

export const PageCardPlaceholder = () => {
    return (
        <div className="page-placeholder">

            {[1, 2].map(n => {
                return (
                    <div key={n} className="result-placeholder">
                        <div className="result">
                            <div className="q-text"/>
                            <div className="value"/>
                        </div>
                        <div className="result-page-chart">
                            <div className="result-page-bar"/>
                        </div>
                    </div>
                )
            })}
        </div>
    )
};