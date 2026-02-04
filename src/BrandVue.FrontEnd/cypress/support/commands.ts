// Adding cypress extension methods such as cy.findAllByText(). For documentation visit: https://www.npmjs.com/package/@testing-library/cypress
import '@testing-library/cypress/add-commands'
import path from 'path';

// ***********************************************
// Custom Cypress commands organized as a chainable instance
// For more comprehensive examples of custom
// commands please read more here:
// https://on.cypress.io/custom-commands
// ***********************************************

export interface SavantaTypeOptions extends Cypress.TypeOptions {
    minElements?: number;
    maxElements?: number;
}

export interface SavantaChainable {
         // Navigation commands
         getTopMenu(): Cypress.Chainable<Element>;
         openMenuDropdown(): Cypress.Chainable<Element>;
         goToMetric(metricName: string): Cypress.Chainable<Element>;

         // Brand set commands
         getBrandSetDropdown(): Cypress.Chainable<Element>;
         getBrandSetDropdownValue(): Cypress.Chainable<Element>;
         changeBrandSetTo(entitySetName: string): Cypress.Chainable<Element>;

         // UI interaction commands
         changeSplitByTo(splitByOption: string): Cypress.Chainable<Element>;

         // Page state commands
         assertPageMatches(url: string, options: Partial<Cypress.UrlOptions>): Cypress.Chainable<Element>;
         assertPageLoadedAndPathMatches(url: string, options: Partial<Cypress.UrlOptions>): Cypress.Chainable<Element>;
         assertPageIsLoaded(options?: Partial<Cypress.Timeoutable>): Cypress.Chainable<Element>;

         // Utility commands
         forEach(query: string, forEachElement: Function, options?: Partial<SavantaTypeOptions>): Cypress.Chainable<Element>;

         // Authentication commands
         login(name?: string, password?: string, cacheSession?: boolean): Cypress.Chainable<Element>;
         visitAndEnsureLoggedIn(pagePath: string, productOverride?: string): Cypress.Chainable<Element>;

         // Tag-based commands
         skipSpecIfNotRunningTag(tag: string): Cypress.Chainable<Element>;
         skipSpecIfNotRunningTags(tags: string[]): Cypress.Chainable<Element>;
}

/**
 * Collection of custom Cypress commands
 */
class CypressCommands implements SavantaChainable {
    
    private static instance: CypressCommands;
    
    private constructor() {}
    
    /**
     * Gets the singleton instance of CypressCommands
     */
    static getInstance(): CypressCommands {
        if (!this.instance) {
            this.instance = new CypressCommands();
        }
        return this.instance;
    }
    
    /**
     * Registers all custom commands with Cypress dynamically
     */
    static registerAll(): void {
        const instance = this.getInstance();
        
        // Get all instance methods that could be commands
        const instanceMethods = Object.getOwnPropertyNames(Object.getPrototypeOf(instance))
            .filter(name => {
                const prop = instance[name as keyof CypressCommands];
                return typeof prop === 'function' && 
                       name !== 'constructor';
            });
        
        // Register instance methods
        instanceMethods.forEach(methodName => {
            const method = instance[methodName as keyof CypressCommands] as Function;
            if (method) {
                (Cypress.Commands.add as any)(methodName, method.bind(instance));
            }
        });
    }

    // Tag-based commands
    skipSpecIfNotRunningTag(tag: string): Cypress.Chainable<Element> {
        return this.skipSpecIfNotRunningTags([tag]);
    }

    skipSpecIfNotRunningTags(tags: string[]): Cypress.Chainable<Element> {
        const tagsArg = Cypress.env('tags');
        // By default we run all specs
        if (!tagsArg)
            return cy.get('body') as any;
        
        const tagsToRun = tagsArg.split(',');
        if (!tagsToRun.length)
            return cy.get('body') as any;
             
        if (tagsToRun.some(tagToRun => tags.includes(tagToRun)))
            return cy.get('body') as any;

        // We're running with specific tags, none of which have been defined for the spec. We should skip this spec.
        // This is a Mocha statement, to access 'this' we need to be in a non-arrow function: https://stackoverflow.com/questions/51491553
        // Note: In instance context, 'this' refers to CypressCommands, but Mocha needs the test context
        // We'll use a workaround to access the test context
        const currentTest = (cy as any).state('ctx');
        if (currentTest && typeof currentTest.skip === 'function') {
            currentTest.skip();
        }
        return cy.get('body') as any;
    }

    // Authentication commands
    login(name?: string, password?: string, cacheSession?: boolean): Cypress.Chainable<Element> {
        const userName = name ?? Cypress.env('userEmail');
        const userPassword = password ?? (Cypress.env('USERPASSWORD') ?? Cypress.env('userPassword'));
        if (userName === "" || userPassword === "") return cy.get('body') as any;
        
        const userLogin = () => {
            cy
                .visit('auth/account/login?externalLoginRedirect=false')
                .get('#EmailAddress').type(userName)
                .get('#Password').type(userPassword)
                .get('.login-button').click()
                .get('.products-heading', { timeout: 10000 }).contains('Your projects');
        }
        if (cacheSession) {
            cy.session(userName, userLogin)
        } else {
            userLogin()
        }
        return cy.get('body') as any;
    }

    visitAndEnsureLoggedIn(pagePath: string, productOverride?: string): Cypress.Chainable<Element> {    
        const product = productOverride || Cypress.env('product') || 'eatingout';

        cy
            .login()
            .then(() => {
                const pathToUse = path.join(product, pagePath);

                // In case the product is not warmed up, BV loader logic has to run on the first page hit.
                cy
                    .visit(pathToUse, {
                      timeout: 120000
                        //responseTimeout: 80000, // Trying to prevent the socket timeout on the first request
                        //pageLoadTimeout: 120000 // This timeout covers the full page load plus all resources (scripts, css, etc) but no subsequent API calls
                    })
                    // Leave another minute for all subsequent API (metadata + data) calls to finish
                    .assertPageIsLoaded({ timeout: 60000 });
            });
        return cy.get('body') as any;
    }

    // Page state commands
    assertPageMatches(url: string, options: Partial<Cypress.UrlOptions>): Cypress.Chainable<Element> {
        cy.url(options).should('include', url);
        return cy.get('body') as any;
    }

    assertPageLoadedAndPathMatches(url: string, options: Partial<Cypress.UrlOptions>): Cypress.Chainable<Element> {
        const urlObject = new URL(url);
        cy.url(options).should('include', urlObject.pathname).assertPageIsLoaded(options);
        return cy.get('body') as any;
    }

    /**
     * This will wait for the #loaded element, which gets added once any back end calls have finished.
     */
    assertPageIsLoaded(options?: Partial<Cypress.Timeoutable>): Cypress.Chainable<Element> {
        // Allow some time for any potential requests to start.
        // Otherwise the page may look like it's loaded when it's about to get into 'not-loaded' state
        options = options = {...options, timeout: options?.timeout ?? 15_000};

        cy.wait(100).get("#loaded", options);
        return cy.get('body') as any;
    }

    // Utility commands
    // Open issue that cypress can't reconnect elements, so just requery for each https://github.com/cypress-io/cypress/issues/7306
    forEach(query: string, forEachElement: Function, options?: Partial<SavantaTypeOptions>): Cypress.Chainable<Element> {
        cy.get(query, options).then(elements => {
            cy.log(`Found ${elements.length} elements`);
            if (options && options.minElements !== undefined) {
                expect(elements.length).to.be.at.least(options.minElements);
            }
            if (options && options.maxElements !== undefined) {
                expect(elements.length).to.be.at.most(options.maxElements);
            }
            elements.each((index) => {
                cy.log(`Locating element ${index + 1}...`);
                cy.get(query, options).should('have.lengthOf', elements.length).then(links => {
                    cy.log(`Testing element ${index + 1}: ${links[index]}`);
                    forEachElement(links[index], index);
                });
            });
        });
        return cy.get('body') as any;
    }

    // Navigation commands
    goToMetric(metricName: string): Cypress.Chainable<Element> {
        cy.log(`Go to metric: ${metricName}`);
        cy.get('div.desktop-nav [placeholder="Search for metrics"]')
            .type(`${metricName}{downArrow}{enter}`);
        cy.assertPageIsLoaded();
        return cy.get('body') as any;
    }

    getTopMenu(): Cypress.Chainable<Element> {
        return cy.get('div.page-title') as any;
    }

    openMenuDropdown(): Cypress.Chainable<Element> {
        cy.log('Open menu dropdown');
        cy.get('[title="Choose a metric"]').click();
        return cy.get('body') as any;
    }

    // Brand set commands
    getBrandSetDropdown(): Cypress.Chainable<Element> {
        return cy.getTopMenu().contains('Brands').parent() as any;
    }

    getBrandSetDropdownValue(): Cypress.Chainable<Element> {
        return cy.getBrandSetDropdown().get('button.entity-set-toggle-btn') as any;
    }

    changeBrandSetTo(entitySetName: string): Cypress.Chainable<Element> {
        cy.log(`Change Brand Set to: ${entitySetName}`);
        cy.getBrandSetDropdown().within(() => {
            cy.get('button.entity-set-toggle-btn').click();
        });
        cy.get('div.entity-set-selector').within(() => {
            cy.get('button.metric-selector-toggle').click();
            cy.get('#metric-search-input').type(`${entitySetName}`);
            cy.get('div.dropdown-metrics').within(() => {
                cy.contains(entitySetName).click();
            });
        });
        cy.assertPageIsLoaded();
        cy.getBrandSetDropdownValue().should('contain', entitySetName);
        return cy.get('body') as any;
    }

    // UI interaction commands
    changeSplitByTo(splitByOption: string): Cypress.Chainable<Element> {
        cy.log(`Change Split By to: ${splitByOption}`);
        cy.getTopMenu().contains('Split by').parent().within(() => {
            cy.get('div.dropdown').click();
            cy.get('div.dropdown-menu.show .dropdown-item').contains(splitByOption).click();
        });
        return cy.get('body') as any;
    }
}


export const registerCypressCommands = () => CypressCommands.registerAll();
