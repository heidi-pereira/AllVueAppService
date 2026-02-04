import { MenuItem, TextField, Typography } from "@mui/material";
import { IEntityType } from "../../../BrandVueApi";
import { TableItemInstances } from "../TableBuilderTypes";

interface Props {
    selectedEntityType: IEntityType;
    allEntities: TableItemInstances[];
    handleEntityTypeChange: (identifier: string) => void;
}

const EntityTypeSelector = (props: Props) => {

    return (
        <TextField
            select
            fullWidth
            label="Choice set"
            value={props.selectedEntityType.identifier}
            onChange={e => props.handleEntityTypeChange(e.target.value)}
        >
            {props.allEntities.map(et => (
                <MenuItem key={et.entityType.identifier} value={et.entityType.identifier}>
                    <Typography
                        variant="body2"
                        sx={{
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap",
                            maxWidth: '552px'
                        }}
                    >
                        {et.entityType.displayNameSingular} ({et.instances.map(i => i.label).join(", ")})
                    </Typography>
                </MenuItem>
            ))}
        </TextField>
    );
};
export default EntityTypeSelector;