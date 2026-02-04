import style from "./brandanalysis/BrandAnalysis.module.less";
import React from "react";

const CardRoundelComponent = (props: { content: JSX.Element, value: string, title: string, isBarometer: boolean, isSubPage: boolean }) => {

    const titleDiv = <div className={style.title}>
                         <div>{props.title}</div>
                     </div>;

    return <div className={style.scorecardComponent}>
               {props.isSubPage && titleDiv}
               <div className={style.cardPadded}>
                   <div className={style.cardRoundel}>
                       <div className={`${style.roundel} ${props.isBarometer ? style.barometer : ""}`} role="roundel">
                           <div className={style.label}>{props.value}</div>
                       </div>
                   </div>
                   {!props.isSubPage && titleDiv}
               </div>
               <div className={`${style.cardSubtext} ${props.isSubPage ? style.fullWidth : ''}`}>
                   {props.content}
               </div>
           </div>;
}

export default CardRoundelComponent;