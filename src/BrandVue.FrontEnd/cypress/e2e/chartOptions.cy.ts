/// <reference types="cypress" />

/** Chart dropdowns belong to the top menu which is a div with a class 'page-title' */
describe('Chart options dropdowns', () => {
    
    const singleEntityMetric = 'Brand Affinity';
    const otherSingleEntityMetric = 'Advertising Awareness';
    const multiEntityMetric = 'Image';
    const otherMultiEntityMetric = 'Occasions Associated';
    const primaryBrandSet = 'Itsu';

    enum SplitBy {
      Brand = "Brand",
      Product = "Occasion"
    }

    before(() => {
        cy.skipSpecIfNotRunningTag('AllTests');
    });

    beforeEach(() => {
        cy.visitAndEnsureLoggedIn('');
    });

    it('Remembers selected brand set when changing from one single-entity metric chart to another', () => {
        cy.goToMetric(singleEntityMetric);
        cy.changeBrandSetTo(primaryBrandSet);
        cy.goToMetric(otherSingleEntityMetric);
        cy.getBrandSetDropdownValue().should('contain', primaryBrandSet);
    });

    it('Remembers selected brand set when changing from a single-entity metric to a multi-entity metric', () => {
        cy.goToMetric(singleEntityMetric);
        cy.changeBrandSetTo(primaryBrandSet);
        cy.goToMetric(multiEntityMetric);
        cy.changeSplitByTo(SplitBy.Brand);
        cy.getBrandSetDropdownValue().should('contain', primaryBrandSet);
    });

    it('Remembers selected brand set when changing from a multi-entity metric to a single-entity metric', () => {
        cy.goToMetric(multiEntityMetric);
        cy.changeSplitByTo(SplitBy.Brand);
        cy.changeBrandSetTo(primaryBrandSet);
        cy.goToMetric(singleEntityMetric);
        cy.getBrandSetDropdownValue().should('contain', primaryBrandSet);
    });
    
    it('Remembers selected brand set when changing Split By option back and forth', () => {
        cy.goToMetric(multiEntityMetric);
        cy.changeSplitByTo(SplitBy.Brand);
        cy.changeBrandSetTo(primaryBrandSet);
        cy.changeSplitByTo(multiEntityMetric);
        cy.changeSplitByTo(SplitBy.Brand);
        cy.getBrandSetDropdownValue().should('contain', primaryBrandSet);
    });

    it('Remembers selected brand set when changing Split By option back and forth and going to another multi-entity metric', () => {
        cy.goToMetric(otherMultiEntityMetric);
        cy.changeSplitByTo(SplitBy.Brand);
        cy.changeBrandSetTo(primaryBrandSet);
        cy.changeSplitByTo(SplitBy.Product);
        cy.goToMetric(multiEntityMetric);
        cy.changeSplitByTo(SplitBy.Brand);
        cy.getBrandSetDropdownValue().should('contain', primaryBrandSet);
    });
});