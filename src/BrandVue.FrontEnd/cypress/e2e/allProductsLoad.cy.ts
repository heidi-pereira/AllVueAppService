/// <reference types="cypress" />

describe('All products load', () => {
    
    before(() => {
       cy.skipSpecIfNotRunningTags(['SmokeTests', 'AllTests']);
    });

    it('Should load Charities', () => {
        cy.visitAndEnsureLoggedIn('', 'charities');
    });

    it('Should load Drinks', () => {
        cy.visitAndEnsureLoggedIn('', 'drinks');
    });

    it('Should load Eating Out', () => {
        cy.visitAndEnsureLoggedIn('', 'eatingout');
    });

    it('Should load Finance', () => {
        cy.visitAndEnsureLoggedIn('', 'finance');
    });
    
    it('Should load Retail', () => {
        cy.visitAndEnsureLoggedIn('', 'retail');
    });

    it('Should load 360', () => {
        cy.visitAndEnsureLoggedIn('', 'brandvue');
    });

    it('Should load Wealth', () => {
        cy.visitAndEnsureLoggedIn('', 'wealth');
    });

})