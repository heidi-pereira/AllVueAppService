import React from 'react';
import CustomDialog from '../shared/CustomDialog';
import { toast } from 'mui-sonner';
import { usePostApiUserForgotpasswordemailMutation } from '../../rtk/apiSlice';
import { User } from '../../orval/api/models';

interface ResendEmailDialogProps {
    verified: boolean;
    open: boolean;
    user: User | null;
    onClose: () => void;
}

const ResendEmailDialog: React.FC<ResendEmailDialogProps> = ({ verified, open, user, onClose}) => {
    const [forgotPassword] = usePostApiUserForgotpasswordemailMutation();

    const handleConfirm = async () => {
        if (user) {
            const { error } = await forgotPassword({ userId: user.id, userEmail: user.email });
            if (error && error.status !== 200) {
                toast.error(`${error.data?.error || error.status}`);
                onClose();
                return;
            } else {
                toast.success(verified ? "Password reset email sent." : "Invite resent.");
            }
            onClose();
        }
    };

    if (!user) return null;

    const fullName = user.firstName?.length ? `${user.firstName} ${user.lastName}` : user.email;
    return (
        <CustomDialog
            open={open}
            title={verified ? "Reset Password" : "Resend invite"}
            question={(verified ? `Would you like to send a password reset email to ${fullName}?` : `Would you like to resend an invite email to ${fullName}?`)}
            description={`Make sure that the user (${user.email}) checks their junk/spam email folder.`}
            confirmButtonText={verified ? "Send reset" : "Send invite"}
            onCancel={onClose}
            onConfirm={handleConfirm}
        />
    );
};

export default ResendEmailDialog;