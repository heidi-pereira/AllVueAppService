import { create } from 'zustand';
import { IGlobalDetails } from '@model/Model';
import * as OpenEndApi from '@model/OpenEndApi';
import mixpanel from 'mixpanel-browser';

interface GlobalDetailsState {
    details: IGlobalDetails;
    fetchGlobalDetails: (surveyId?: string) => Promise<void>;
}

const useGlobalDetailsStore = create<GlobalDetailsState>((set) => ({
    details: { 
        overrideLocalOrg: '', 
        user: {
            isAdministrator: false,
            isAuthorizedSavantaUser: false,
            isInSavantaRequestScope: false,
            isReportViewer: false,
            isSystemAdministrator: false,
            isThirdPartyLoginAuth: false,
            isTrialUser: false,
            featurePermissions: null,
            accountName: '',
            currentCompanyShortCode: '',
            firstName: '',
            lastName: ''
        },
        defaultQueueId: 0, 
        faviconUrl: '', 
        stylesheetUrl: '', 
        basePath: '', 
        mixPanelToken: '',
        maxTexts: 0,
        surveyName: "",
        navigationTabs: [],
        customUiIntegrations: []
    },

    fetchGlobalDetails: async (surveyId) => {
        const details = await OpenEndApi.getGlobalDetails(surveyId);

        if (details.mixPanelToken) {
            mixpanel.init(details.mixPanelToken, {
                debug: true,
                track_pageview: false,
                persistence: "localStorage",
                api_host: "/mixpanel",
            });

            mixpanel.identify(details.user.accountName);
            mixpanel.track("Start session");
        } else {
            mixpanel.init('dev', { debug: true, persistence: "localStorage" });
            mixpanel.disable();
        }

        set({ details });
    }
}));

export default useGlobalDetailsStore;
