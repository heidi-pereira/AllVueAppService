import { InfoOutlined, HelpOutline } from "@mui/icons-material";
import { Link, Theme, Typography } from "@mui/material";

interface HelpIconProps {
    helpText?: string;
    helpUrl?: string;
    iconText?: string;
}

const HelpIcon = ({ helpText, helpUrl, iconText }: HelpIconProps) => {

    const style = (theme: Theme) => ({ fontSize: 'medium', ml: 1, color: theme.palette.primary.main, verticalAlign:'middle', '&:focus': { outline: 'none' } })

    const Icon = helpUrl
        ? <HelpOutline data-tooltip-id='shared' data-tooltip-html={(helpText ? helpText + ' - ' : '') + 'Click to learn more'} sx={style} />
        : <InfoOutlined data-tooltip-id='shared' data-tooltip-html={helpText} sx={style} />

    return (
        <Link href={helpUrl} target='_blank' underline='none' display='inline-flex'>
           <Typography component='span'>{Icon}</Typography>
            {iconText && 
                <Typography component='span' fontSize='small' sx={(theme) => ({ paddingTop: '3px', color: theme.palette.primary.main, pl: 1})}>{iconText}</Typography>}
        </Link>
    );

}

export default HelpIcon;

