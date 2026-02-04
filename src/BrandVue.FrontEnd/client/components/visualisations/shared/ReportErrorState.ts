export interface ReportErrorState {
    isError: boolean;
    errorMessage: string;
}

export const initialReportErrorState: ReportErrorState = {
    isError: false,
    errorMessage: ""
}