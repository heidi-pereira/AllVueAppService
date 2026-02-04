const path = require('path');
const merge = require('webpack-merge');
const common = require('./webpack.config.js');
const appsettings = require('./appsettings.json');
const ExtractCssPlugin = require('mini-css-extract-plugin');

class PrintCompilationTime {
    apply(compiler) {
        compiler.hooks.watchRun.tapAsync(
            'Print-Compilation-Times',
            (compilation, callback) => {
                console.log(" ");
                let now = new Date();
                console.log(`Started compilation at ${now.getHours().toString().padStart(2, "0")}:${now.getMinutes().toString().padStart(2, "0")}:${now.getSeconds().toString().padStart(2, "0")}`);
                callback();
            }
        );
        compiler.hooks.done.tapAsync(
            'Print-Compilation-Times',
            (compilation, callback) => {
                console.log(" ");
                let now = new Date();
                console.log(`Finished compilation at ${now.getHours().toString().padStart(2, "0")}:${now.getMinutes().toString().padStart(2, "0")}:${now.getSeconds().toString().padStart(2, "0")}`);
                callback();
            }
        );
    }
}

module.exports = (env, argv) => merge(common(env, argv), {
    mode: 'development',
    output: {
        filename: "[name].js",
        publicPath: '/dist/',
    },
    devtool: 'inline-source-map',
    devServer: {
        compress: true,
        port: appsettings.Development.HMRPort,
        static: path.resolve(__dirname,"wwwroot"),
        server: {
            type: 'http'
        }
    },

    plugins: [
        new PrintCompilationTime(),
        new ExtractCssPlugin({
            filename: "[name].css",
            chunkFilename: "[id].css"
        })
    ]
});
