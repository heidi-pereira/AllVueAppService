/// <reference types="cypress" />

describe('Brand performance page', () => {
    const pageUrl = 'ui/home/brand-performance/competition';

    before(() => {
        cy.skipSpecIfNotRunningTag('AllTests');
/*        
		cy.intercept('GET', '/retail/api/data/wordle*', (req) => {
		  req.reply({
			statusCode: 200, // default
			fixture: 'wordle.json'
		  })
		});
*/
    });

    beforeEach(() => {
        cy.visitAndEnsureLoggedIn(pageUrl);
    });
	
    it('Has 4 cards with links to charts', () => {
        cy.forEach('div.card-multi.has-link', link => {
			
            link.click();
            cy.assertPageIsLoaded({ timeout: 80000 });
			cy.wait(5000);
            cy.go('back');
            cy.assertPageIsLoaded({ timeout: 80000 });
			cy.wait(5000);
        }, { minElements: 4, maxElements: 4, timeout: 80000});
    });

    it('Has 3-5 metric change links', () => {
        cy.forEach('a.metric-change', link => {
            link.click();
            cy.assertPageIsLoaded({ timeout: 80000 });
			cy.wait(5000);
            cy.go('back');
            cy.assertPageIsLoaded({ timeout: 80000 });
			cy.wait(5000);
        }, { minElements: 3, maxElements: 5, timeout: 80000});
    });
})
