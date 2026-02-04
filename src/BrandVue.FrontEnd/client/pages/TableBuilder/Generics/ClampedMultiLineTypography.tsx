import Typography, { TypographyProps } from '@mui/material/Typography';

type MultiLineEllipsisTypographyProps = TypographyProps & {
  lineClamp: number;
};

const MultiLineEllipsisTypography = (props: MultiLineEllipsisTypographyProps) => (
    <Typography
        {...props}
        sx={{
            display: '-webkit-box',
            WebkitLineClamp: props.lineClamp,
            WebkitBoxOrient: 'vertical',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            wordBreak: 'break-word',
            ...props.sx,
        }}
    >
        {props.children}
    </Typography>
);

export default MultiLineEllipsisTypography;