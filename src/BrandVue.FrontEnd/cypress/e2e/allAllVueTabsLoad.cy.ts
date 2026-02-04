/// <reference types="cypress" />

describe('All AllVue tabs load', () => {

    const surveyId = Cypress.env('allVueSurveyId');
    
    before(() => {
       cy.skipSpecIfNotRunningTags(['SmokeTests', 'AllTests']);
    });

    it('Should load AllVue Reports', () => {
        cy.visitAndEnsureLoggedIn('', `survey/${surveyId}/ui/reports`);
    });
    
    it('Should load AllVue Data', () => {        
        cy.visitAndEnsureLoggedIn('', `survey/${surveyId}/ui/crosstabbing`);
    });
})