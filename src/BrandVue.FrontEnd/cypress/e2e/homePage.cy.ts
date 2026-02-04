/// <reference types="cypress" />

describe('Home page', () => {

    before(() => {
        cy.skipSpecIfNotRunningTag('AllTests');
    });

    beforeEach(() => {
        cy.visitAndEnsureLoggedIn('/');
    });
    
    it('Navigate to charts', () => {
        cy.url().then(homePageUrl => {
            cy.forEach('.chartContent div a',
                link => {
                    link.click();
                    cy.assertPageLoadedAndPathMatches(link.href, {timeout: 300000})
                        // The waits are to try and prevent the flakiness of this test when run on TC.
                        // It sometimes fails when going back to the home page, I don't know why.
                        // I couldn't replicate it when running locally.
                        .wait(100)
                        .go('back')                        
                        .wait(100)
                        .assertPageLoadedAndPathMatches(homePageUrl, {timeout: 180000});
                }, {minElements: 12, maxElements: 24}
            );
        });
    });
})