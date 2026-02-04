const path = require('path');
const webpack = require('webpack');
var package = require('./package.json');
const TerserPlugin = require('terser-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CssMinimizerPlugin = require('css-minimizer-webpack-plugin');

module.exports = (env) => {
    const isDevBuild = !(env && env.prod);
    return [{
        mode: isDevBuild ? 'development' :'production',
        stats: { modules: false },
        resolve: {
            extensions: ['.js']
        },
        module: {
            rules: [
                { test: /\.(png|woff|woff2|eot|ttf|svg)(\?|$)/, type: 'asset/inline' },
                { test: /\.css(\?|$)/, use: [MiniCssExtractPlugin.loader, 'css-loader'] }
            ]
        },
        entry: {
            // All dependencies are assumed to be needed for reporting...
            "vendor_reporting": Object.keys(package.dependencies).filter(i=>['font-awesome'].indexOf(i) === -1).concat([
                'bootstrap/dist/css/bootstrap.css',
                'material-icons/iconfont/material-icons.css',
                'font-awesome/css/font-awesome.css'
            ])
        },
        output: {
            path: path.join(__dirname, 'wwwroot', 'dist'),
            publicPath: 'dist/',
            filename: '[name].js',
            library: '[name]_[hash]'
        },
        optimization: {
            minimizer: [
                new TerserPlugin({
                    parallel: true,
                    terserOptions: {
                        compress: false,
                        ecma: 8,
                        mangle: true
                    }
                }),
                new CssMinimizerPlugin()
            ]
        },
        plugins: [
            new MiniCssExtractPlugin({ filename: '[name].css' }),
            new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery' }), // Maps these identifiers to the jQuery package (because Bootstrap expects it to be a global variable)
            new webpack.DllPlugin({
                path: path.join(__dirname, 'wwwroot', 'dist', '[name]-manifest.json'),
                name: '[name]_[hash]'
            }),
            new webpack.DefinePlugin({
                'process.env.NODE_ENV': isDevBuild ? '"development"' : '"production"'
            })
        ]
    }];
};
