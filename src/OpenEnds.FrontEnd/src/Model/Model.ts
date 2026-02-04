import { IUserContext } from "@/Template/Header/IUserContext";
import { CustomUIIntegration } from "./CustomUIIntegration";

export interface IGlobalDetails {
    overrideLocalOrg: string,
    mixPanelToken: string,
    user: IUserContext,
    defaultQueueId: number,
    faviconUrl: string,
    stylesheetUrl: string,
    basePath: string,
    maxTexts: number,
    surveyName: "",
    navigationTabs: NavigationTab[],
    customUiIntegrations: CustomUIIntegration[]
}

export enum NavigationTab {
    Reports = "Reports",
    Data = "Data",
    Documents = "Documents",
    Quota = "Quota"
}

export enum Role {
    ClientUser = "ClientUser",
    ClientAdmin = "ClientAdmin",
    SavantaUser = "SavantaUser",
    SavantaAdmin = "SavantaAdmin",
}

export type ThemeSensitivityConfigurationResponse = {
    totalTexts: number;
    themes: ThemeSensitivityConfigurationItem[];
}

export type ThemeSensitivityConfigurationItem = {
    userSuppliedId: string,
    text: string,
    distanceScore: number,
    isFuzzyMatch: boolean,
    isKeywordMatch: boolean,
    isManuallyIncluded: boolean,
    isManuallyExcluded: boolean
}

export type ThemeConfigurationResponse = {
    themes: ThemeConfiguration[];
}

export type ThemeConfigurationSplitResponse = {
    themes: BasicTheme[];
    message: string,
}

export type BasicTheme = {
    name: string;
    group: string;
    matches: number;
    matchPatterns: string[];
}

export type RootTheme = OpenEndTheme & {
    subThemes: OpenEndTheme[];
}

export type ThemeConfiguration = {
    id: number;
    name: string;
    parentId?: number;
    matchingBehaviour: MatchingBehaviour;
}

export type MatchingBehaviour = {
    matchingExamples: string[];
    keywords: string[];
    delegatedMatching: boolean;
    exclusiveSubtheme: boolean;
    includeOtherSubtheme: boolean;
    matchingSensitivity: number;
}

export type Question = {
    id: number;
    text: string;
    varCode: string;
}

export type OpenEndQuestion = {
    question: Question;
    questionCount: number;
    themeCount: number;
    status: OpenEndQuestionStatusResponse;
    additionalInstructions: string;
}

export type OpenEndQuestionsResponse = {
    respondentCount: number;
    openTextQuestions: OpenEndQuestion[];
}

export type OpenEndQuestionSummaryResponse = {
    totalCount: number;
    question: Question;
    openTextAnswerCount: number;
    summary: string;
    themes: OpenEndTheme[];
    textThemes: OpenEndTextTheme[]; // Matches TextThemes in C#
    additionalInstructions: string;
}

export type OpenEndTextTheme = {
    text: string;
    themes: number[];
}

export type OpenEndTheme = {
    themeText: string;
    count: number;
    themeIndex: number;
    score: number;
    percentage: number;
    themeId: number;
    themeSensitivity: number;
    parentId?: number
}

export type OpenEndQuestionStatusResponse = {
    progress: number;
    message: string;
    statusEvent?: StatusEvent;
}

export enum StatusEvent {
    newProject = "NewProject",
    uploadingData = "UploadingData",
    analysing = "Analysing",
    finished = "Finished",
}

export type PreviewStatsResponse = {
    keywordMatches: number;
    fuzzyMatches: number;
    combinedMatches: number;
    total: number;
}

export type PreviewMatchResponse = {
    matchPatterns: string[];
    matches: number;
    total: number;
}

export enum ExportFormat {
    TabularXLSX,
    TabularCSV,
    CodebookXLSX
}