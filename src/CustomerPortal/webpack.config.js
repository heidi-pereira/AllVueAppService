const path = require('path');
const webpack = require('webpack');
const merge = require('webpack-merge');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin');
const CaseSensitivePathsPlugin = require('case-sensitive-paths-webpack-plugin');
const TerserWebpackPlugin = require("terser-webpack-plugin");
const CssMinimizerPlugin = require("css-minimizer-webpack-plugin");

module.exports = (env, argv) => {
    const isDevBuild = argv.mode === "development";

    const sharedConfig = () => {

        var mode = isDevBuild ? "development" : "production";

        console.log('\x1b[36m%s\x1b[0m', "=== Webpack compilation mode: " + mode + " ===");

        var config = {
            mode,
            optimization: {
                minimize: !isDevBuild,
                usedExports: isDevBuild,
                moduleIds: isDevBuild ? 'named' : 'deterministic',
                minimizer: !isDevBuild ? [
                    // Production.
                    new TerserWebpackPlugin({
                        terserOptions: {
                            output: {
                                comments: false,
                            },
                        },
                    }),
                    new CssMinimizerPlugin()
                ] : [
                        // Development.
                    ]
            },
            stats: { modules: false },
            resolve: {
                extensions: ['.js', '.jsx', '.ts', '.tsx', '.jpg'],
                alias: {
                    "@Docs": path.resolve(__dirname, "ClientApp/docs/"),
                    "@Layouts": path.resolve(__dirname, "ClientApp/layouts/"),
                    "@Components": path.resolve(__dirname, "ClientApp/components/"),
                    "@Images": path.resolve(__dirname, "ClientApp/images/"),
                    "@Store": path.resolve(__dirname, "ClientApp/store/"),
                    "@Utils": path.resolve(__dirname, "ClientApp/utils"),
                    "@Styles": path.resolve(__dirname, 'ClientApp/styles/'),
                    "@Pages": path.resolve(__dirname, 'ClientApp/pages/'),
                    "@Cards": path.resolve(__dirname, 'ClientApp/cards/'),
                    "@Services": path.resolve(__dirname, 'ClientApp/services/'),
                    "@Models": path.resolve(__dirname, 'ClientApp/models/'),
                    "@Globals": path.resolve(__dirname, 'ClientApp/Globals'),
                    "FeatureGuardShared": path.resolve(__dirname, '../Vue.Common.FrontEnd/Components/FeatureGuard')
                }
            },
            output: {
                filename: '[name].js',
                publicPath: 'dist/', // Webpack dev middleware, if enabled, handles requests for this URL prefix.
            },
            module: {
                rules: [
                    {
                        test: /\.tsx?$/,
                        use: [
                            {
                                loader: 'babel-loader',
                                options: {
                                    compact: true
                                }
                            },
                            {
                                loader: 'ts-loader',
                                options: {
                                    // Disable type checker - we will use it in fork plugin.
                                    transpileOnly: true
                                }
                            },
                            'ts-nameof-loader']
                    },
                    {
                        test: /\.(gif|png|jpe?g|svg|woff|woff2|eot|ttf)$/i,
                        type: 'asset/inline'
                    },
                    {
                        test: /\.md$/,
                        use: [
                            {
                                loader: "raw-loader"
                            }
                        ]
                    }
                ]
            },
            plugins: [
                new ForkTsCheckerWebpackPlugin(),
                // Moment.js is an extremely popular library that bundles large locale files
                // by default due to how Webpack interprets its code. This is a practical
                // solution that requires the user to opt into importing specific locales.
                // https://github.com/jmblog/how-to-optimize-momentjs-with-webpack
                // You can remove this if you don't use Moment.js:
                new webpack.IgnorePlugin({ resourceRegExp: /^\.\/locale$/, contextRegExp: /moment$/})
            ].concat(isDevBuild ? [
                // Watcher doesn't work well if you mistype casing in a path so we use
                // a plugin that prints an error when you attempt to do this.
                // See https://github.com/facebookincubator/create-react-app/issues/240
                new CaseSensitivePathsPlugin()
            ] : [
                // Production.
            ])
        };

        if (isDevBuild) {

            // Change config for development build.

            config = {
                ...config,
                // Turn off performance hints during development because we don't do any
                // splitting or minification in interest of speed. These warnings become
                // cumbersome.
                performance: {
                    hints: false,
                },
                devtool: 'eval-source-map'
            };

            config.resolve.alias = {
                ...config.resolve.alias
            };
        }

        return config;
    };

    // Configuration for client-side bundle suitable for running in browsers.
    const clientBundleOutputDir = './wwwroot/dist';
    const clientBundleConfig = merge(sharedConfig(), {
        entry: './ClientApp/main.tsx',
        module: {
            rules: [
                { test: /\.css$/, use: isDevBuild ? ['style-loader', 'css-loader'] : [MiniCssExtractPlugin.loader, 'css-loader'] },
                { test: /\.less/, use: isDevBuild ? ['style-loader', 'css-loader', 'less-loader'] : [MiniCssExtractPlugin.loader, 'css-loader', 'less-loader'] },
                { test: /\.(scss|sass)$/, use: isDevBuild ? ['style-loader', 'css-loader', 'sass-loader'] : [MiniCssExtractPlugin.loader, 'css-loader', 'sass-loader'] }
            ]
        },
        output: { path: path.join(__dirname, clientBundleOutputDir) },
        plugins: [
            new webpack.DllReferencePlugin({
                context: __dirname,
                manifest: require('./wwwroot/dist/vendor-manifest.json')
            })
        ].concat(isDevBuild ? [

            // Do not need this as we used devtool https://webpack.js.org/configuration/devtool/ but if we stop using devtool then this could
            //new webpack.SourceMapDevToolPlugin({
            //    filename: '[file].map', // Remove this line if you prefer inline source maps.
            //    moduleFilenameTemplate: path.relative(clientBundleOutputDir, '[resourcePath]') // Point sourcemap entries to the original file locations on disk
            //})

        ] : [
            // Production.
            new MiniCssExtractPlugin({
                filename: "site.css"
            })
        ])
    });

    return clientBundleConfig;
};