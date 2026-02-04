import React from 'react';
import Button from '@mui/material/Button';
import ButtonGroup from '@mui/material/ButtonGroup';
import style from './HeatMapConfiguration.module.less';
import { HeatMapKeyPosition, HeatMapOptions } from "../../../../../BrandVueApi";
import { Label, Input } from 'reactstrap';
 
interface IHeatMapConfiguration {
    options: HeatMapOptions;
    saveOptionChanges(HeatMapOptions): void;
}

const HeatMapConfiguration = (props: IHeatMapConfiguration) => {
    const heatMapOptions = props.options;

    const setRadius = (value: number) => {
        heatMapOptions.radius = value;
        props.saveOptionChanges(heatMapOptions);
    }

    const setTransparency = (value: number) => {
        heatMapOptions.overlayTransparency = value / 100;
        props.saveOptionChanges(heatMapOptions);
    }

    const setIntensity = (value: number) => {
        heatMapOptions.intensity = value;
        props.saveOptionChanges(heatMapOptions);
    }

    const setKeyPosition = (value: HeatMapKeyPosition) => {
        heatMapOptions.keyPosition = value;
        props.saveOptionChanges(heatMapOptions);
    }

    const toggleDisplayKey = () => {
        heatMapOptions.displayKey = !heatMapOptions.displayKey;
        props.saveOptionChanges(heatMapOptions);
    }

    const toggleDisplayClickCount = () => {
        heatMapOptions.displayClickCounts = !heatMapOptions.displayClickCounts;
        props.saveOptionChanges(heatMapOptions);
    }

    const getNumericInput = (id: string, label: string, min: number, max: number, value: number, onChange) => {
        return (
            <div className={style.categoryLabel}>
                    <input id={id}
                        type="number"
                        autoComplete="off"
                        min={min}
                        max={max}
                        step="1"
                        value={value}
                        onChange={(e) => { onChange(+e.target.value) }}
                        onBlur={(e) => { onChange(+e.target.value) } }
                    />
                <label htmlFor={id}>
                    {label}
                </label>
            </div>);
    }

    const getKeyPositionButton = (icon: string, keyPosition: HeatMapKeyPosition) => {
        const className = heatMapOptions.keyPosition === keyPosition ? "selected" : "";
        return (<Button className={className} onClick={() => setKeyPosition(keyPosition)}><i className="material-symbols-outlined">{icon}</i></Button>)
    }

    return (
        <>
            <div className={style.section}>
                <div className="option-section-label category-label">Data points (user clicks)</div>
                {getNumericInput("radius", "Radius in pixels", 1, 999, heatMapOptions.radius!, setRadius)}
                {getNumericInput("transparency", "% Transparency", 1, 100, heatMapOptions.overlayTransparency! * 100, setTransparency)}
                {getNumericInput("intensity", "Intensity", 1, 100, heatMapOptions.intensity!, setIntensity)}
            </div>
            <div className={style.section}>
                <div className="option-section-label category-label">Heatmap key</div>
                <div className={style.position}>
                    <label>Position
                        <ButtonGroup className={style.positionButtons}>
                            {getKeyPositionButton("north_west", HeatMapKeyPosition.TopLeft)}
                            {getKeyPositionButton("north_east", HeatMapKeyPosition.TopRight)}
                            {getKeyPositionButton("south_west", HeatMapKeyPosition.BottomLeft)}
                            {getKeyPositionButton("south_east", HeatMapKeyPosition.BottomRight)}
                        </ButtonGroup>
                    </label>
                </div>
                <div className={style.checkbox}>
                    <Input id="display-key-checkbox" type="checkbox" className="checkbox" checked={heatMapOptions.displayKey} onChange={() => toggleDisplayKey()}></Input>
                    <Label for="display-key-checkbox">Display key</Label>
                </div>
                <div className={style.checkbox}>
                    <Input id="display-click-count-checkbox" type="checkbox" className="checkbox" checked={heatMapOptions.displayClickCounts} onChange={() => toggleDisplayClickCount()}></Input>
                    <Label for="display-click-count-checkbox">Display click count</Label>
                </div >
            </div>
        </>
    );
}

export default HeatMapConfiguration