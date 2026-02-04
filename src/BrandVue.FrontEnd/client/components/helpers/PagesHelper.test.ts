import { PageDescriptor, PaneDescriptor, PartDescriptor } from '../../BrandVueApi';
import { findPageTreeByMetricName, findPageTreeByPageDisplayName } from './PagesHelper';
import { ViewTypeEnum } from "./ViewTypeHelper";

const createPageForMetric = (pageName: string, metricName: string, viewType: ViewTypeEnum): PageDescriptor => {
    return PageDescriptor.fromJS({
        name: pageName,
        displayName: pageName,
        panes: [
            {
                view: viewType,
                parts: [
                    {
                        spec1: metricName
                    }
                ]
            }
        ]
    });
}

describe("findPageTreeByMetricName", () => {
    it("should return an array with one item when the metric page is a parent page", () => {
        const expectedPageName = "expectedPageName";
        const metricName = "metricName";

        const pages = new Array<PageDescriptor>();
        pages.push(createPageForMetric(expectedPageName, metricName, ViewTypeEnum.Competition));
        pages.push(createPageForMetric("someOtherPage1", "someOtherMetric1", ViewTypeEnum.Competition));
        pages.push(createPageForMetric("someOtherPage2", "someOtherMetric2", ViewTypeEnum.OverTime));
        
        const pageTree = findPageTreeByMetricName(pages, metricName);

        expect(pageTree.length).toBe(1);
        expect(pageTree[0].name).toBe(expectedPageName);
    });

    it("should find the metric page even if there are other panes", () => {
        const expectedPageName = "expectedPageName";
        const metricName = "metricName";

        const pages = new Array<PageDescriptor>();
        const page = createPageForMetric(expectedPageName, metricName, ViewTypeEnum.Competition);
        
        // Add the same metric pane, but on a different view
        page.panes.push(PaneDescriptor.fromJS({
            view: ViewTypeEnum.OverTime,
                parts: [
                    {
                        spec1: metricName
                    }
                ]
        }));

        // Add another pane
        page.panes.push(PaneDescriptor.fromJS({
            view: ViewTypeEnum.OverTime
        }));

        pages.push(page);
        pages.push(createPageForMetric("someOtherPage1", "someOtherMetric1", ViewTypeEnum.Competition));
        pages.push(createPageForMetric("someOtherPage2", "someOtherMetric2", ViewTypeEnum.OverTime));
        
        const pageTree = findPageTreeByMetricName(pages, metricName);

        expect(pageTree.length).toBe(1);
        expect(pageTree[0].name).toBe(expectedPageName);
    });

    it("should return an array with two items when the metric page is a second level page", () => {
        const expectedParentPageName = "parentName";
        const expectedChildName = "childName"
        const metricName = "metricName";

        const pageContainingTheChildPageWeAreLookingFor = createPageForMetric(expectedParentPageName, "someOtherMetric", ViewTypeEnum.Ranking);
        pageContainingTheChildPageWeAreLookingFor.childPages.push(createPageForMetric(expectedChildName, metricName, ViewTypeEnum.Competition));

        const pages = new Array<PageDescriptor>();
        pages.push(createPageForMetric("someOtherPage1", "someOtherMetric1", ViewTypeEnum.Competition));
        pages.push(createPageForMetric("someOtherPage2", "someOtherMetric2", ViewTypeEnum.OverTime));
        pages.push(pageContainingTheChildPageWeAreLookingFor);

        const pageTree = findPageTreeByMetricName(pages, metricName);

        expect(pageTree.length).toBe(2);
        expect(pageTree[0].name).toBe(expectedParentPageName);
        expect(pageTree[1].name).toBe(expectedChildName);
    });

    it("should return an array with three items when the metric page is a third level page", () => {
        const rootName = "rootName";
        const branchName = "branchName"
        const leafName = "leafName"
        const metricName = "metricName";
        const pages = new Array<PageDescriptor>();
        
        // Create the pages we expect to find
        const rootPage = createPageForMetric(rootName, "123", ViewTypeEnum.Ranking);
        const branchPage = createPageForMetric(branchName, "321", ViewTypeEnum.PerformanceVsPeers);
        const leafPage = createPageForMetric(leafName, metricName, ViewTypeEnum.Competition);
                
        // Add some other pages to all of them
        const someOtherPage = createPageForMetric("otherPage", "123", ViewTypeEnum.OverTime);
        rootPage.childPages.push(someOtherPage);
        branchPage.childPages.push(someOtherPage);
        leafPage.childPages.push(someOtherPage);
        
        // Create the tree structure        
        rootPage.childPages.push(branchPage);
        branchPage.childPages.push(leafPage);

        // Add more noise        
        pages.push(createPageForMetric("someOtherPage1", "someOtherMetric1", ViewTypeEnum.Competition));
        pages.push(createPageForMetric("someOtherPage2", "someOtherMetric2", ViewTypeEnum.OverTime));
        pages.push(rootPage);
        pages.push(createPageForMetric("blabla", "543543fsdf", ViewTypeEnum.Competition));

        const pageTree = findPageTreeByMetricName(pages, metricName);

        expect(pageTree.length).toBe(3);
        expect(pageTree[0].name).toBe(rootName);
        expect(pageTree[1].name).toBe(branchName);
        expect(pageTree[2].name).toBe(leafName);
    });

    it("should ignore metric pages which have a view type different than Competition", () => {
        const metricName = "metricName";

        const pages = new Array<PageDescriptor>();
        pages.push(createPageForMetric("pageName", metricName, ViewTypeEnum.OverTime));
        pages.push(createPageForMetric("someOtherPage1", metricName, ViewTypeEnum.Profile));
        pages.push(createPageForMetric("someOtherPage2", metricName, ViewTypeEnum.ProfileOverTime));
        pages.push(createPageForMetric("fdsafds", metricName, ViewTypeEnum.Ranking));
        pages.push(createPageForMetric("hgfdgvfd444", metricName, ViewTypeEnum.Performance));
        pages.push(createPageForMetric("dsfdsfd4345332", metricName, ViewTypeEnum.PerformanceVsPeers));
        
        const pageTree = findPageTreeByMetricName(pages, metricName);

        expect(pageTree.length).toBe(0);
    });

    it("should ignore pages which include the metric, but have more than one parts on that pane", () => {
        const metricName = "metricName";

        const pages = new Array<PageDescriptor>();
        const page = createPageForMetric("pageName", metricName, ViewTypeEnum.Competition);
        page.panes[0].parts.push(PartDescriptor.fromJS({
            spec1: "anotherPartSpec"
        }));
        pages.push(page);
        
        const pageTree = findPageTreeByMetricName(pages, metricName);

        expect(pageTree.length).toBe(0);
    });

    it("should ignore pages which include the metric more than once on Competition view", () => {
        const metricName = "metricName";

        const pages = new Array<PageDescriptor>();
        const page = createPageForMetric("pageName", metricName, ViewTypeEnum.Competition);
        page.panes.push(PaneDescriptor.fromJS({
                view: ViewTypeEnum.Competition,
                parts: [
                    {
                        spec1: metricName
                    }
                ]
            })
        );
        
        const pageTree = findPageTreeByMetricName(pages, metricName);

        expect(pageTree.length).toBe(0);
    });
});

describe("findPageTreeByPageDisplayName", () => {
    it("should find pages matching the page display name", () => {
        const pageName = "pageName";

        const pages = new Array<PageDescriptor>();
        pages.push(createPageForMetric("otherPage", "metric", ViewTypeEnum.Competition));
        pages.push(createPageForMetric(pageName, "metric", ViewTypeEnum.Competition));

        const pageTree = findPageTreeByPageDisplayName(pages, pageName, false);

        expect(pageTree.length).toBe(1);
        expect(pageTree[0].name).toBe(pageName);
    });

    it("should find pages beginning with the page display name when configured", () => {
        const pageName = "pageName";
        const pageNamePlusExtra = pageName + "extra";

        const pages = new Array<PageDescriptor>();
        pages.push(createPageForMetric("otherPage", "metric", ViewTypeEnum.Competition));
        pages.push(createPageForMetric(pageNamePlusExtra, "metric", ViewTypeEnum.Competition));

        const pageTree = findPageTreeByPageDisplayName(pages, pageName, true);

        expect(pageTree.length).toBe(1);
        expect(pageTree[0].name).toBe(pageNamePlusExtra);
    });

    it("should not find a page beginning with the page display name when not configured", () => {
        const pageName = "pageName";
        const pageNamePlusExtra = pageName + "extra";

        const pages = new Array<PageDescriptor>();
        pages.push(createPageForMetric("otherPage", "metric", ViewTypeEnum.Competition));
        pages.push(createPageForMetric(pageNamePlusExtra, "metric", ViewTypeEnum.Competition));

        const pageTree = findPageTreeByPageDisplayName(pages, pageName, false);

        expect(pageTree.length).toBe(0);
    });

    it("should not find a page beginning with the page display name when there exists a page matching exactly", () => {
        const pageName = "pageName";
        const pageNamePlusExtra = pageName + "extra";

        const pages = new Array<PageDescriptor>();
        pages.push(createPageForMetric("otherPage", "metric", ViewTypeEnum.Competition));
        pages.push(createPageForMetric(pageName, "metric", ViewTypeEnum.Competition));
        pages.push(createPageForMetric(pageNamePlusExtra, "metric", ViewTypeEnum.Competition));

        const pageTree = findPageTreeByPageDisplayName(pages, pageName, true);

        expect(pageTree.length).toBe(1);
        expect(pageTree[0].name).toBe(pageName);
    });
});