import { toast, ToastOptions, cssTransition } from 'react-toastify';
import './toast.scss';
import { ReactNode } from 'react';

export const Slide = cssTransition({
    enter: 'moveUp',
    exit: 'moveDown',
    duration: 250
});

export class ToastHandler {
    showToast(message: string, toastId?: number) {
        const toastStyle: ToastOptions =
        {
            position: toast.POSITION.BOTTOM_CENTER,
            closeButton: false,
            className: "toast toast-success"
        };

        if (toastId == null) {
            toast.success(message, toastStyle);
        } else if (!toast.isActive(toastId)) {
            toast.success(message, { toastId, ...toastStyle });
        } else {
            toast.update(toastId, { ...toastStyle, render: message, type: toast.TYPE.SUCCESS });
        }
    }

    showError(message: string, toastId?: number) {
        const toastStyle: ToastOptions =
        {
            position: toast.POSITION.BOTTOM_CENTER,
            autoClose: false,
            closeButton: true,
            className: "toast toast-error"
        };

        if (toastId == null) {
            toast.error(message, toastStyle);
        } else if (!toast.isActive(toastId)) {
            toast.error(message, { toastId, ...toastStyle });
        } else {
            toast.update(toastId, { ...toastStyle, render: message, type: toast.TYPE.ERROR });
        }
    }

    showProgress(message: string | ReactNode, toastId?: number) {
        const toastStyle: ToastOptions =
        {
            toastId: toastId,
            position: toast.POSITION.BOTTOM_CENTER,
            autoClose: false,
            closeButton: false
        };

        if (toastId == null) {
            toast(message, toastStyle);
        } else if (!toast.isActive(toastId)) {
            toast(message, { toastId, ...toastStyle });
        } else {
            toast.update(toastId, { ...toastStyle, render: message, type: toast.TYPE.DEFAULT });
        }
    }

    dismiss(toastId?: number) {
        if (toastId != null) {
            toast.dismiss(toastId);
        } else {
            toast.dismiss();
        }
    }
}

export default ToastHandler;