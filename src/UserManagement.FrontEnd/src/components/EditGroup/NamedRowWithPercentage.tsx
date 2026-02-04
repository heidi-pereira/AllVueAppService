import React from 'react';
import { Box, Typography } from '@mui/material';

interface NamedRowWithPercentageProps {
    name: string;
    percent: number | React.ReactNode;
    onClick?: () => void;
}
function describeArc(cx: number, cy: number, r: number, percent: number) {
    const angle = (percent / 100) * 360;
    const radians = (angle - 90) * Math.PI / 180.0;
    const x = cx + r * Math.cos(radians);
    const y = cy + r * Math.sin(radians);
    const largeArcFlag = percent > 50 ? 1 : 0;

    if (percent === 0) {
        return '';
    }
    if (percent === 100) {
        return `
            M ${cx} ${cy}
            m -${r}, 0
            a ${r},${r} 0 1,0 ${r * 2},0
            a ${r},${r} 0 1,0 -${r * 2},0
        `;
    }

    return `
        M ${cx} ${cy}
        L ${cx} ${cy - r}
        A ${r} ${r} 0 ${largeArcFlag} 1 ${x} ${y}
        Z
    `;
}
const PieChart: React.FC<{ percent: number; radius?: number }> = ({ percent, radius = 6 }) => {
    const cx = radius;
    const cy = radius;
    const r = radius - 1;
    const size = radius * 2;
    const backgroundCircleColor = percent === 0 ? "#FF0000" : "#eee";
    const sliceOfPieColor = "#4caf50";
    return (
        <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
            <circle cx={cx} cy={cy} r={r} fill={backgroundCircleColor} />
            {percent > 0 && (
                <path
                    d={describeArc(cx, cy, r, percent)}
                    fill={sliceOfPieColor}
                />
            )}
        </svg>
    );
};

const drawPercentageWithIcon = (percent: number) => {
    return (
        <>
            <PieChart percent={percent} />
            <Typography variant="questionListPercent">
                {percent.toFixed(0)}%
            </Typography>
        </>
    );
}

const NamedRowWithPercentage: React.FC<NamedRowWithPercentageProps> = ({name, percent, onClick }) => (
    <Box display="flex" alignItems="baseline"
        gap={0.5} onClick={onClick}
        sx={{ cursor: onClick ? 'pointer' : 'default' }}>
        <Typography variant="filterListOption">{name}</Typography>
        <Box sx={{ flexGrow: 1 }} />
        {typeof percent === 'number' ?
            (drawPercentageWithIcon(percent))
            :
            (percent)
        }
    </Box>
);

export default NamedRowWithPercentage;