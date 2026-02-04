import * as BrandVueApi from "../../BrandVueApi";

export class ViewType {
    id: ViewTypeEnum;
    name: string;
    url: string;
    icon: string;

    constructor(id: ViewTypeEnum, name: string, icon: string) {
        this.id = id;
        this.name = name;
        this.icon = icon;
        this.url = `/${name.toLowerCase().replace(/ /g, '-')}`;
    }
}

export function getViewTypeEnum(viewType: number): BrandVueApi.ViewTypeEnum {
    switch (viewType) {
        case 1:
            return BrandVueApi.ViewTypeEnum.OverTime;
        case 2:
            return BrandVueApi.ViewTypeEnum.Competition;
        case 3:
            return BrandVueApi.ViewTypeEnum.Profile;
        case 4:
            return BrandVueApi.ViewTypeEnum.ProfileOverTime;
        case 5:
            return BrandVueApi.ViewTypeEnum.RankingTable;
        case 6:
            return BrandVueApi.ViewTypeEnum.ScorecardPerformance;
        case 7:
            return BrandVueApi.ViewTypeEnum.ScorecardVsPeers;
        case -5:
            return BrandVueApi.ViewTypeEnum.FullPage;
        case -10:
            return BrandVueApi.ViewTypeEnum.SingleSurveyNav;
        default:
            throw new Error("Invalid view type");
    }
}

export enum ViewTypeEnum {
    SingleSurveyNav = -10,
    OverTime = 1,
    Competition = 2,
    Profile = 3,
    ProfileOverTime = 4,
    Ranking = 5,
    Performance = 6,
    PerformanceVsPeers = 7,
}

export const allViewTypes = [
    new ViewType(ViewTypeEnum.OverTime, "Over time", "timeline"),
    new ViewType(ViewTypeEnum.Competition, "Competition", "bar_chart"),
    new ViewType(ViewTypeEnum.Ranking, "Ranking", "format_list_numbered"),
    new ViewType(ViewTypeEnum.Profile, "Profile", "people"),
    new ViewType(ViewTypeEnum.ProfileOverTime, "Profile over time", "multiline_chart"),
    new ViewType(ViewTypeEnum.Performance, "Performance", ""),
    new ViewType(ViewTypeEnum.PerformanceVsPeers, "Performance vs key competitors", ""),
];

export const getViewTypeByNameOrUrl = (viewTypeName: string) : ViewType | undefined => {
    let foundViewTypeByName = allViewTypes.find(vt => vt.name.toLowerCase() === viewTypeName.toLowerCase())
    return foundViewTypeByName || allViewTypes.find(vt => vt.url.toLowerCase() === viewTypeName.toLowerCase())
}