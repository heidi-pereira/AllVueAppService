/// <reference types="cypress" />

describe('All menu links load', () => {
    const defaultMaxPagesToSample = Cypress.env('maxPagesToSample') ?? 5;
    const queries = {
        'non audience': '.nav-menu-container div.active > ul > li:not(.navItem-Audience) a:not(.nopage)',
        'audience' : '.nav-menu-container div.active > ul > li.navItem-Audience a:not(.nopage)'
    };

    Object.entries(queries).forEach(entry => {
        const [name, query] = entry;
        const maxPagesToSample = Cypress.env(`maxPagesToSample_${name.replace(" ", "_")}`) ?? defaultMaxPagesToSample;
        describe(`Visit ${maxPagesToSample} ${name} pages`, () => {
            let sampledLinks: Array<{element: HTMLElement, text: string, index: number}> = [];
            let numberOfLinks;
            
            before(() => {
               cy.skipSpecIfNotRunningTags(['AllTests']);
               cy.visitAndEnsureLoggedIn('/').openMenuDropdown();
               cy.get(query).then(links => {
                   numberOfLinks = links.length;
                   const linkIncrement = Math.floor(links.length / maxPagesToSample);
                   
                   sampledLinks = [];
                   for(let i = 1; i <= maxPagesToSample; i++) {
                       const linkIndex = i * linkIncrement - 1;
                       if (linkIndex < numberOfLinks) {
                           sampledLinks.push({
                               element: links[linkIndex],
                               text: links[linkIndex].innerText,
                               index: linkIndex
                           });
                       }
                   }
               });
            });

            it(`Verify sample size for ${name} pages`, function () {
                expect(numberOfLinks).to.be.above(maxPagesToSample, `Maximum pages to sample (${maxPagesToSample}) is incorrectly greater than the number of links (${numberOfLinks}).`);
            });

            for(let sampleIndex = 0; sampleIndex < maxPagesToSample; sampleIndex++) {
                it(`Visit ${name} page sample ${sampleIndex + 1}`, function() {
                    const linkData = sampledLinks[sampleIndex];
                    if (!linkData) {
                        cy.log(`Skipping sample ${sampleIndex + 1} - not enough links`);
                        return;
                    }
                    
                    cy.visitAndEnsureLoggedIn('/').openMenuDropdown();
                    cy.get(query).then((links) => {
                        const link = links[linkData.index];
                        Cypress.log({
                            displayName: 'Click menu item',
                            message: `${linkData.text} (link index ${linkData.index}, sample ${sampleIndex + 1}/${sampledLinks.length})`,
                            name: 'Click menu item'
                        });
                        link.click();
                        cy.assertPageIsLoaded({timeout: 300000});
                    });
                });
            }
        });
    });
});