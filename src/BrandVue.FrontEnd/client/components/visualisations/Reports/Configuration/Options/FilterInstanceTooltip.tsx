import { styled, Tooltip, tooltipClasses, TooltipProps } from "@mui/material";

export const InstanceTooltip = styled(({ className, ...props }: TooltipProps) => (
    <Tooltip {...props} classes={{ popper: className }} />
))(({ theme }) => ({
    [`& .${tooltipClasses.tooltip}`]: {
        backgroundColor: 'black',
        fontSize: 12,
        maxWidth: 400,
        padding: 12
    },
}));