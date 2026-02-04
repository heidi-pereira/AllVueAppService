
export const DataGridViewRenderDate = (date?: string) => {
    if (!date) {
        return (<span>--</span>);
    }
    const dateAsADate = new Date(date);
    const result = dateAsADate.toLocaleString(undefined,
        {
            year: 'numeric',
            month: 'short',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
        });
    return (<span>{result}</span>);
}

export const DataGridViewRenderYesNo = (yesNo: boolean) => {
    return (<span>{yesNo?"Yes":"No"}</span>);
}

