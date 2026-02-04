import { create } from 'zustand';
import { OpenEndQuestionSummaryResponse } from '@model/Model';
import * as OpenEndApi from './OpenEndApi';

interface ThemeSummaryState {
    themeSummary: OpenEndQuestionSummaryResponse | undefined;
    setThemeSummary: (summary: OpenEndQuestionSummaryResponse) => void;
    reloadThemeSummary: (surveyId: string, questionId: number) => Promise<void>;
}

export const useThemeSummaryStore = create<ThemeSummaryState>((set) => ({
    themeSummary: undefined,
    setThemeSummary: (summary) => set({ themeSummary: summary }),
    reloadThemeSummary: async (surveyId, questionId) => {
        try {
            const response = await OpenEndApi.getQuestionSummary(surveyId, questionId);
            set({ themeSummary: response });
        } catch (error) {
            console.error('Error reloading theme summary:', error);
        }
    },
}));
