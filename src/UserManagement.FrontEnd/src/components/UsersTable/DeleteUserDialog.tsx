import React from 'react';
import CustomDialog from '../shared/CustomDialog';
import { toast } from 'mui-sonner';
import { useDeleteApiUserDeleteByUserIdMutation } from '../../rtk/apiSlice';
import { User } from '../../orval/api/models';

interface DeleteUserDialogProps {
    open: boolean;
    user: User | null;
    onClose: () => void;
    onDeleted: () => void;
}

const DeleteUserDialog: React.FC<DeleteUserDialogProps> = ({ open, user, onClose, onDeleted }) => {
    const [deleteUser] = useDeleteApiUserDeleteByUserIdMutation();

    const handleConfirm = async () => {
        if (user) {
            const { error } = await deleteUser({ userId: user.id, userEmail: user.email });
            if (error && error.status !== 200) {
                toast.error(`${error.data?.error || error.status}`);
                onClose();
                return;
            }
            onDeleted();
            onClose();
        }
    };

    if (!user) return null;

    const name = user.firstName?.length ? user.firstName : user.email;
    const fullName = user.firstName?.length ? `${user.firstName} ${user.lastName}` : user.email;

    return (
        <CustomDialog
            open={open}
            title="Remove user"
            question={`Are you sure you want to remove ${fullName} from your team?`}
            description={`This can't be undone, and ${name} will lose access.`}
            confirmButtonText="Remove"
            confirmButtonColour="error"
            onCancel={onClose}
            onConfirm={handleConfirm}
        />
    );
};

export default DeleteUserDialog;