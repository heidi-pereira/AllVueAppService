import { Id, toast } from "react-toastify";
import {
    IGlobalDetails,
    Question,
    OpenEndQuestionsResponse,
    OpenEndQuestionSummaryResponse,
    OpenEndQuestionStatusResponse,
    ThemeConfigurationResponse,
    ThemeSensitivityConfigurationResponse,
    PreviewMatchResponse,
    PreviewStatsResponse,
    ExportFormat
} from "./Model";

export async function initialiseProject(surveyId: string, questionId: number, instructions: string) {
    return await handledFetch(`surveys/${surveyId}/questions/${questionId}/initialise`, 'POST', { instructions })
}

export async function deleteQuestion(surveyId: string, questionId: number) {
    return await handledFetch<PreviewMatchResponse>(`surveys/${surveyId}/questions/${questionId}`, 'DELETE')
}

export async function recalculateQuestion(surveyId: string, questionId: number) {
    return await handledFetch<PreviewMatchResponse>(`surveys/${surveyId}/questions/${questionId}/recalculate`, 'POST')
}

export async function getNamePreview(surveyId: string, questionId: number, themeName: string) {
    return await handledFetch<PreviewMatchResponse>(`surveys/${surveyId}/questions/${questionId}/configuration/themes/preview`, 'POST', { themeName }, false)
}

export async function previewStats(surveyId: string,
    questionId: number,
    themeId: number,
    threshold: number,
    keywords: string[],
    matchPatterns: string[]) {
    return await handledFetch<PreviewStatsResponse>(`surveys/${surveyId}/questions/${questionId}/configuration/themes/${themeId}/previewstats`,
        'POST',
        {
            threshold: threshold,
            keywords: keywords,
            matchingExamples: matchPatterns
        },
        false)
}

export async function getThemeConfiguration(surveyId: string, questionId: number) {
    return await handledFetch<ThemeConfigurationResponse>(`surveys/${surveyId}/questions/${questionId}/configuration/themes`)
}

export async function deleteThemeConfiguration(surveyId: string, questionId: number, themeId: number) {
    await handledFetch(`surveys/${surveyId}/questions/${questionId}/configuration/themes/${themeId}`, 'DELETE')
}

export async function createThemeConfiguration(surveyId: string, questionId: number, themeName: string, keywords: string[] | null) {
    await handledFetch(`surveys/${surveyId}/questions/${questionId}/configuration/themes`, 'POST', { themeName, keywords })
}

export async function updateThemeParent(surveyId: string, questionId: number, themeId: number, newParentId: number | null) {
    await handledFetch(`surveys/${surveyId}/questions/${questionId}/configuration/themes/${themeId}/parent`, 'PUT', newParentId);
};

export async function updateThemeConfiguration(surveyId: string, questionId: number, themeId: number, sensitivity: number, matchingExamples: string[], keywords: string[], displayName: string) {
    await handledFetch(`surveys/${surveyId}/questions/${questionId}/configuration/themes/${themeId}`, 'PUT', { sensitivity, matchingExamples, keywords, displayName });
};

export async function mergeThemes(surveyId: string, questionId: number, themeId: number, targetThemeId: number) {
    await handledFetch(`surveys/${surveyId}/questions/${questionId}/configuration/themes/${themeId}/merge`, 'PATCH', { targetThemeId });
};

export async function getThemeSensitivity(surveyId: string, questionId: number, themeId: number) {
    return await handledFetch<ThemeSensitivityConfigurationResponse>(`surveys/${surveyId}/questions/${questionId}/configuration/themes/${themeId}/sensitivity`);
};

export async function getQuestionStatus(surveyId: string, questionId: number) {
    return await handledFetch<OpenEndQuestionStatusResponse>(`surveys/${surveyId}/questions/${questionId}/status`, undefined, undefined, false)
}

export async function getGlobalDetails(surveyId?: string) {
    return await handledFetch<IGlobalDetails>(`getglobaldetails/${surveyId ? surveyId : ''}`);
}

export async function getQuestions(surveyId: string) {
    return await handledFetch<OpenEndQuestionsResponse>(`surveys/${surveyId}/questions`);
}

export async function getQuestion(surveyId: string, questionId: number) {
    return await handledFetch<Question>(`surveys/${surveyId}/questions/${questionId}`);
}

export async function getQuestionSummary(surveyId: string, questionId: number) {
    return await handledFetch<OpenEndQuestionSummaryResponse>(`surveys/${surveyId}/questions/${questionId}/summary`);
}

export async function getQuestionSummaryExport(surveyId: string, questionId: number, format: ExportFormat) {
    return await handledFetch(`surveys/${surveyId}/questions/${questionId}/summary/export?format=${encodeURIComponent(format)}`);
}

export async function getCodedTextCount(surveyId: string, questionId: number) {
    return await handledFetch<number>(`surveys/${surveyId}/questions/${questionId}/summary/codedtextcount`);
}

const dateTimeReviver = (_: string, value: string) => {
    if (typeof value === 'string') {
        const dateMatch = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.exec(value);
        if (dateMatch) {
            return new Date(dateMatch[0]);
        }
    }
    return value;
}

let activeRequests = 0;
let loadingId: Id | null = null;

async function handledFetch<T>(url: string, method: "GET"| "POST"| "PUT" | "DELETE" | "PATCH" = "GET", body: any | undefined = undefined, showLoading = true) {
    const headers: Record<string, string> = {
        'Content-Type': 'application/json',
    };

    const getLoadingMessage = () => {
        let message = "Loading... please wait..."
        if (activeRequests>1) message+= ` (${activeRequests} active requests)`;
        return message;
    }

    const viewAs: string = (window as any).location.search.split('viewas=')[1];
    if (viewAs) {
        headers['ViewAs'] = viewAs;
    }

    if (showLoading) {
        activeRequests += 1;
        if (loadingId === null) {
            loadingId = toast.loading(getLoadingMessage());
        } else {
            toast.update(loadingId, { render: getLoadingMessage() });
        }
    }

    const fullApiUrl = document.baseURI + 'api/' + url;

    const response = await fetch(fullApiUrl, {
        method: method,
        headers: headers,
        body: body ? JSON.stringify(body) : undefined,
    });

    if (showLoading) {
        activeRequests -= 1;
        if (activeRequests === 0 && loadingId !== null) {
            toast.dismiss(loadingId);
            loadingId = null;
        } else if (loadingId !== null) {
            toast.update(loadingId, { render: getLoadingMessage() });
        }
    }

    if (!response.ok) {
        if (response.status === 401) {
            const redirectUrl = encodeURIComponent(window.location.href);
            window.location.href = `login?redirectUrl=${redirectUrl}`;
            return undefined as T;
        }

        let errorMessage;
        const errorText = await response.text();

        if (errorText) {
            try {
                const error = JSON.parse(errorText) as ErrorMessage;
                errorMessage = error.detailedMessage;
            } catch {
                errorMessage = errorText;
            }
        } else {
            errorMessage = "";
        }

        let userFriendlyMessage: string | undefined;
        switch (response.status) {
            case 400:
                userFriendlyMessage = "The request was invalid. Please check your input and try again.";
                break;
            case 403:
                userFriendlyMessage = "You do not have permission to perform this action.";
                break;
            case 404:
                userFriendlyMessage = "The requested resource was not found.";
                break;
            case 500:
                userFriendlyMessage = "A server error occurred. Please try again later.";
                break;
            case 504:
                userFriendlyMessage = "The server took too long to respond. Please try again in a moment.";
                break;
            default:
                userFriendlyMessage = undefined;
        }

        if (userFriendlyMessage) {
            toast.error(userFriendlyMessage);
        } else if (errorMessage) {
            toast.error("An unexpected error occurred: " + errorMessage);
        } else {
            toast.error("An unexpected error occurred.");
        }

        return undefined as T;
    }

    if (response.headers.get('Content-Type') === 'application/zip') {
        const blob = await response.blob();

        const header = response.headers.get('Content-Disposition');
        const parts = header!.split(';');
        const filename = parts[1].split('=')[1];

        const elm = document.createElement('a');
        elm.href = URL.createObjectURL(blob);
        elm.setAttribute('download', filename);
        elm.click();

        return undefined as T;
    }

    const result = await response.text();
    if (result) {
        return JSON.parse(result, dateTimeReviver) as T;
    } else {
        return undefined as T;
    }
}


type ErrorMessage = {
    code: number;
    message: string;
    detailedMessage: string;
}
