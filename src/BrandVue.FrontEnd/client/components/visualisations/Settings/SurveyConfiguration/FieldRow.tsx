import React from "react";
import { Factory, KimbleProposal} from "../../../../BrandVueApi";
import { ProductConfiguration } from "../../../../ProductConfiguration";
import Throbber from "../../../throbber/Throbber";
import style from './KimbleProposal.module.less';

const FieldRow: React.FC<{ label: string; value?: string; showWarning?: boolean }> = ({
    label,
    value,
    showWarning = true,
}) => {
    const isEmpty = !value?.trim();
    const valueClass = isEmpty && showWarning ? style.warning : style.valueRow;
    return (
        <div className={style.row}>
            <div className={style.labelRow}>{label}</div>
            <div className={valueClass}>
                {isEmpty ? `No ${label.toLowerCase()} defined` : value}
            </div>
        </div>
    )};
export default FieldRow;