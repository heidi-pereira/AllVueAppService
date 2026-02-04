const path = require('path');
const webpack = require('webpack');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CheckerPlugin = require('awesome-typescript-loader').CheckerPlugin;
const bundleOutputDir = './wwwroot/dist';
const TerserPlugin = require('terser-webpack-plugin');

module.exports = (env) => {
    return [{
        mode: 'none',
        stats: { modules: false },
        entry: {
            'reporting': './ClientApp/boot.tsx'
        },
        resolve: { extensions: ['.js', '.jsx', '.ts', '.tsx'] },
        output: {
            path: path.join(__dirname, bundleOutputDir),
            filename: '[name].js',
            publicPath: 'dist/',
            libraryTarget: 'var',
            library: 'ui'
        },
        module: {
            rules: [
                { test: /\.tsx?$/, include: /ClientApp/, use: 'awesome-typescript-loader?silent=true' },
                { test: /\.less$/i, use: [MiniCssExtractPlugin.loader, 'css-loader', 'less-loader'] },
                { test: /\.(png)?$/, loader: "url-loader", options: { limit: 100000 } }
            ]
        },
        optimization: {
            minimizer: [
                new TerserPlugin({
                    parallel: true,
                    terserOptions: {
                        compress: false,
                        ecma: 8,
                        mangle: true
                    },
                })
            ]
        },
        plugins: [
            new MiniCssExtractPlugin({ filename: '[name].css' }),
            new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery' }),
            new CheckerPlugin(),
            new webpack.DllReferencePlugin({
                context: __dirname,
                manifest: require('./wwwroot/dist/vendor_reporting-manifest.json')
            }),
            new webpack.SourceMapDevToolPlugin({
                filename: '[file].map', // Remove this line if you prefer inline source maps
                moduleFilenameTemplate:
                    path.relative(bundleOutputDir,
                        '[resourcePath]') // Point sourcemap entries to the original file locations on disk
            })
       ]
    }];
};