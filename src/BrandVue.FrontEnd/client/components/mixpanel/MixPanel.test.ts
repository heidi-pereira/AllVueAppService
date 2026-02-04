import { MixPanel } from './MixPanel';
import { IMixPanelClient } from './IMixPanelClient';
import { ApplicationUser } from '../../BrandVueApi';
import { MixPanelModel } from './MixPanelHelper';
import "@testing-library/jest-dom";

describe('MixPanel', () => {
    let mockClient: IMixPanelClient;
    let model: MixPanelModel;

    beforeEach(() => {
        mockClient = {
            init: jest.fn(),
            identify: jest.fn(),
            track: jest.fn(),
            setPeople: jest.fn(),
            reset: jest.fn()
        };
        model = {
            client: mockClient,
            projectId: 'testProjectId',
            userId: 'testUserId',
            isAllVue: true,
            productName: 'testProduct',
            project: 'testProject',
            kimbleProposalId: "KimbleId",
        };
        MixPanel.init(model);
    });

    test('init should initialize client and set properties', () => {
        expect(mockClient.init).toHaveBeenCalledWith('testProjectId');
        expect(mockClient.identify).toHaveBeenCalledWith('testUserId');
        expect(MixPanel['isAllVue']).toBe(true);
        expect(MixPanel['productName']).toBe('testProduct');
    });

    test('logout should track userLoggedOut event', () => {
        MixPanel.logout();
        expect(mockClient.track).toHaveBeenCalledWith('User Logged Out', expect.any(Object));
    });

    test('track should track events correctly', () => {
        MixPanel.track('metricVsMetric');
        expect(mockClient.track).toHaveBeenCalledWith('Metric Vs Metric', expect.any(Object));
    });

    test('track with context should track events correctly', () => {
        MixPanel.trackWithContext('metricVsMetric', "myContext");

        const expectedProps = {
            "Category": "Audience",
            "Page": undefined,
            "SubCategory": "Audience Page",
            "Subset": undefined,
            "Tag": undefined,
            "Product": "testProduct",
            "Context": "myContext",
            "Project": "testProject",
            "KimbleProposalId": "KimbleId",
        }

        expect(mockClient.track).toHaveBeenCalledWith('Metric Vs Metric', expectedProps);
    });

    test('trackPageLoadTime should track page load time correctly', () => {
        MixPanel.trackPageLoadTime({PageLoadTime: 123,PageName: 'testPage', EntitySet: 1, Instances: [1], 
            Average: "Monthly", Metric: "Metric", DateStart: "2021-11-11", DateEnd: "2022-12-21" }, []);
        expect(mockClient.track).toHaveBeenCalledWith('Test Page Page Loaded', expect.any(Object));
        expect(mockClient.track).toHaveBeenCalledWith('Page Loaded', expect.any(Object));
    });

    test('trackPage should track page selection correctly', () => {
        MixPanel.trackPage('testPage');
        expect(mockClient.track).toHaveBeenCalledWith('Page Selected', expect.any(Object));
    });

    test('setPeople should set user profile correctly', () => {
        const user = new ApplicationUser(); 
        MixPanel.setPeople(user);
        expect(mockClient.setPeople).toHaveBeenCalledWith(expect.any(Object));
    });

    test('trackSurvey should track projectLoaded event', () => {
        MixPanel.trackSurvey([1], ['surveyNames'], 'sport-england-grouped-survey-name', '');
        expect(mockClient.track).toHaveBeenCalledWith('Project Loaded', expect.any(Object));
    });
});