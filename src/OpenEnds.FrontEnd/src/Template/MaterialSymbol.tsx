import { useTheme } from '@mui/material/styles';

interface MaterialSymbolProps {
    symbolName: string;
    size: "small" | "medium" | "large";
    colour?: string;
}

function getMaterialSymbolFontSize(size: "small" | "medium" | "large"): number {
    switch (size) {
        case "small":
            return 12;
        case "medium":
            return 18;
        case "large":
            return 24;
        default:
            return 18;
    }
}

const MaterialSymbol: React.FC<MaterialSymbolProps> = ({ symbolName, size, colour }) => {
    const theme = useTheme();
    const symbolColor = colour ?? theme.palette.primary.main;

    return (<i className="material-symbols-outlined" style={{ fontSize: getMaterialSymbolFontSize(size), color: symbolColor }}>{symbolName}</i>
    );
};

export default MaterialSymbol;