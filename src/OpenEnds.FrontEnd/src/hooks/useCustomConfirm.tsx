import { Close } from '@mui/icons-material';
import { IconButton } from '@mui/material';
import { ButtonProps } from '@mui/material/Button';
import { useConfirm } from 'material-ui-confirm';

export const useCustomConfirm = () => {
    const confirm = useConfirm();
    const handleCloseClick = (event: React.MouseEvent) => {
        const parentElement = (event.target as HTMLElement).closest('[role="dialog"]');
        if (parentElement) {
            const cancelButton = Array.from(parentElement.querySelectorAll('button')).find(button => button.textContent === 'Cancel');
            cancelButton?.click();
        }
    };

    const customConfirm = async (options: { title: React.ReactNode; description: React.ReactNode; confirmationText: string; confirmationButtonProps?: ButtonProps }) => {
        return confirm({
            title: <>{options.title} <IconButton onClick={handleCloseClick} sx={{ float: 'right' }} > <Close /></IconButton></>,
            description: options.description,
            confirmationText: options.confirmationText,
            confirmationButtonProps: options.confirmationButtonProps
        });
    };

    return customConfirm;
};
