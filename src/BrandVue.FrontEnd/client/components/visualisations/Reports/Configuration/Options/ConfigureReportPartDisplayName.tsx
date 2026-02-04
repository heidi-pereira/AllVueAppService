import React from 'react';
import { PartDescriptor } from "../../../../../BrandVueApi";

interface IConfigureReportPartDisplayNameProps {
    part: PartDescriptor;
    savePartChanges(newPart: PartDescriptor);
}

const ConfigureReportPartDisplayName = (props: IConfigureReportPartDisplayNameProps) => {
    const [displayName, setDisplayName] = React.useState<string>(props.part.helpText ?? "");
    const [isEditingDisplayName, setEditingDisplayName] = React.useState<boolean>(false);

    React.useEffect(() => {
        setDisplayName(props.part.helpText ?? "");
    }, [props.part]);

    const updateDisplayName = () => {
        setEditingDisplayName(false);
        const modifiedPart = new PartDescriptor(props.part);
        modifiedPart.helpText = displayName;
        props.savePartChanges(modifiedPart);
    }

    const handleDisplayNameKeyDown = (event) => {
        if (event.key === 'Enter') {
            updateDisplayName();
        } else if (event.key === 'Escape') {
            setDisplayName(props.part.helpText ?? "");
            setEditingDisplayName(false);
        }
    };

    if (isEditingDisplayName) {
        return (
            <>
                <label className="category-label">Display name</label>
                <input type="text"
                    className="input text-input display-name-input"
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    onBlur={() => updateDisplayName()}
                    onKeyDown={handleDisplayNameKeyDown}
                    ref={input => input && input.focus()} />
            </>
        );
    } else {
        return (
            <>
                <label className="category-label">Display name</label>
                <div className="display-name-text">
                    <span className="display-text">{displayName}</span>
                    <i className="material-symbols-outlined edit-icon" onClick={() => setEditingDisplayName(true)}>edit</i>
                </div>
            </>
        );
    }
}

export default ConfigureReportPartDisplayName