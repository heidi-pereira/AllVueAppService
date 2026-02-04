#!/bin/env node

'use strict';

var fs = require('fs');
var sass = require('sass');

function sassCompile(infile, outfile) {
    try {
        const compileOptions = {
            functions: {
                'font-url($filename, $only-path: false)': function (args) {
                    const [fileName, onlyPath] = args;
                    if (onlyPath.getValue()) return fileName;
                    return new sass.SassString(`url("../../pub/fonts/${fileName.text}")`, { quotes: false });
                }
            }
        };

        const compileResult = sass.compile(infile, compileOptions);
        fs.writeFile(outfile, compileResult.css, 'utf8', function (err) {
            if (err) console.warn(err.message);
        });
    } catch (error) {
        console.error(error);
    }
}

sassCompile('./docs/source/stylesheets/screen.css.scss', '../developers/docs/pub/css/screen.css');
sassCompile('./docs/source/stylesheets/print.css.scss', '../developers/docs/pub/css/print.css');
