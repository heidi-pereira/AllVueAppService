const fs = require('fs');
const path = require('path');

console.log('Search Files for Bootstrap 4 Classes');

const bootstrap4Classes = [
    'form-row', 'form-inline', 'custom-control', 'custom-checkbox', 'custom-radio',
    'custom-switch', 'custom-select', 'custom-file', 'custom-range', 'input-group-prepend',
    'input-group-append', 'input-group-text', 'badge-pill', 'float-left', 'float-right',
    'text-left', 'text-right', 'ml-1', 'ml-2', 'ml-3', 'ml-4', 'ml-5',
    'mr-1', 'mr-2', 'mr-3', 'mr-4', 'mr-5', 'pl-1', 'pl-2', 'pl-3', 'pl-4', 'pl-5',
    'pr-1', 'pr-2', 'pr-3', 'pr-4', 'pr-5', 'border-left', 'border-right',
    'rounded-left', 'rounded-right', 'dropleft', 'dropright', 'navbar-toggler-left',
    'navbar-toggler-right', 'card-deck', 'card-group', 'card-columns'
];

const directoryPath = 'C:\\Users\\RichardPhillips\\source\\repos\\Vue\\src\\BrandVue.FrontEnd\\client';

function searchFiles(dir) {
    //console.log('Searching:', dir);
    const files = fs.readdirSync(dir);
    const impactedFiles = new Set();

    files.forEach(file => {
        const filePath = path.join(dir, file);
        const stat = fs.statSync(filePath);
        if (stat.isDirectory()) {
            searchFiles(filePath);
        } else if (filePath.endsWith('.ts') || filePath.endsWith('.tsx')) {
          bootstrap4Classes.forEach(cssClass => {
                const content = fs.readFileSync(filePath, 'utf-8');
                // const regex = new RegExp(`className="[^"]*\\b${cssClass}\\b[^"]*"`);
                const regex1 = new RegExp(`className=["'][^"']*?\\s+${cssClass}\\s+[^"']*?["']`);
                const regex2 = new RegExp(`className=["']${cssClass}["']`);
                const regex3 = new RegExp(`className=["']${cssClass}\\s+[^"']*?["']`);
                const regex4 = new RegExp(`className=["'][^"']*?\\s+${cssClass}["']`);
                // const regex5 = new RegExp(`className=["'][^"']*?\\b${cssClass}\\b[^"']*?["']`);
            
                if (regex1.test(content) || regex2.test(content) || regex3.test(content) || regex4.test(content)) {
                impactedFiles.add(filePath);
                console.log(cssClass,filePath);
                }
            });
        }
    });

    return Array.from(impactedFiles);
}

const impactedFiles = searchFiles(directoryPath);
// console.log('Impacted files:', impactedFiles);