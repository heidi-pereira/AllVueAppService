import './commands'
import 'cypress-mochawesome-reporter/register';
import { SavantaChainable, registerCypressCommands as registerSavantaCypressCommands } from './commands'

registerSavantaCypressCommands();

// Extend Cypress global namespace to include our custom chainable commands
declare global {
  namespace Cypress {
     interface Chainable<Subject = any> extends SavantaChainable {
    }
  }
}

// Make it so that fetch requests don't wrap so the model doesn't fill the whole window
Cypress.on('window:before:load', () => {
  const styleId = 'custom-command-message-style';
  const doc = window.top?.document;
  if (doc && !doc.getElementById(styleId)) {
    const style = doc.createElement('style');
    style.id = styleId;
    style.innerHTML = `
      .command-message-text {
        max-width: 28vw !important;
        overflow: hidden !important;
        text-overflow: ellipsis !important;
        white-space: nowrap !important;
        cursor: pointer !important;
      }
    `;
    doc.head.appendChild(style);
  }
});