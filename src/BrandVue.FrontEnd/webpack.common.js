import webpack from "webpack";
import path from "path";
import { fileURLToPath } from 'url';
import { CleanWebpackPlugin } from "clean-webpack-plugin";
import ForkTsCheckerWebpackPlugin from "fork-ts-checker-webpack-plugin";
import HtmlWebpackPlugin from "html-webpack-plugin";
import {BundleAnalyzerPlugin} from "webpack-bundle-analyzer";
import extractCssPlugin from "mini-css-extract-plugin";
import areYouEs5 from "are-you-es5";
import loaders from "./webpack.loaders.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const escape = (dep) => dep.replace('/', '\\/');
const getBabelLoaderIgnoreRegex = () => {
    const moduleCompatCheck = areYouEs5.checkModules({
        path: '',
        checkAllNodeModules: true,
        ignoreBabelAndWebpackPackages: true
    });
    return new RegExp(`[\\\\/]node_modules[\\\\/](?!(${moduleCompatCheck.es6Modules.map(escape).join('|')})[\\\\/])`);
};

export default env => {
    return {
        devtool: "source-map",
        entry: "./client/main.tsx",
        output: {
            path: path.resolve(__dirname, "wwwroot/dist"),
            hashFunction: "xxhash64"
        },
        resolve: {
            extensions: [".ts", ".tsx", ".js"],
            alias: {
                client: path.resolve(__dirname, 'client'),
                FeatureGuardShared: path.resolve(__dirname, '../Vue.Common.FrontEnd/Components/FeatureGuard')
            }
        },
        stats: {
            warningsFilter: [
                /Critical dependency/,
            ]
        },
        plugins: [
            new BundleAnalyzerPlugin({
                analyzerMode: env && env.BUNDLE_ANALYZE ? "server" : "disabled"
            }),
            new webpack.IgnorePlugin({
                resourceRegExp: /^\.\/locale$/, 
                contextRegExp: / moment$ /
            }),
            new webpack.ProvidePlugin({
                $: "jquery",
                jQuery: "jquery",
                Popper: ["popper.js", "default"]
            }),
            new CleanWebpackPlugin({
                path: 'wwwroot/dist',
            }),
            new ForkTsCheckerWebpackPlugin(),
            new HtmlWebpackPlugin({
                inject: false,
                template: 'client/webpackEntryPointTemplate.ejs',
                filename: '../../wwwroot/webpackEntryPoint.html.partial'
            }),
        ],
        module: {
            rules: [
                {
                    test: /\.js$/,
                    use: [loaders.babelLoader],
                    exclude: [
                        getBabelLoaderIgnoreRegex() //Ignore everything not ES6 for babel loader. This polyfills node_modules as well.
                    ]
                },
                {
                    test: /\.tsx?$/,
                    //Strictly we don't need tsLoader as babelLoader can handle TS with a preset. 
                    //However in many places in code we use inconsistent import styles and this is not supported.
                    //So stick to what works for now and then pipe to babel
                    use: [loaders.babelLoader, loaders.tsLoader] 

                },
                {
                    test: /\.module\.less$/,
                    use: [extractCssPlugin.loader, loaders.moduleCssLoader,
                        loaders.postCssLoader, loaders.lessLoader]
                },
                {
                    test: /\.(less)$/,
                    exclude: /\.module\./,
                    use: [extractCssPlugin.loader, loaders.cssLoader,
                        loaders.postCssLoader, loaders.lessLoader]
                },
                {
                    test: /\.module\.css$/,
                    use: [extractCssPlugin.loader, loaders.moduleCssLoader, loaders.postCssLoader]
                },
                {
                    test: /\.css$/,
                    exclude: /\.module\./,
                    use: [extractCssPlugin.loader, loaders.cssLoader, loaders.postCssLoader]
                },
                {
                    test: /\.woff(2)?(\?v=[0-9]\.[0-9]\.[0-9])?$/,
                    type: 'asset',
                    parser: {
                        dataUrlCondition: {
                            maxSize: 10000
                        }
                    }
                },
                {
                    test: /\.(ttf|eot|svg)(\?v=[0-9]\.[0-9]\.[0-9])?$/,
                    type: 'asset/resource'
                },
                {
                    test: /\.(png)?$/,
                    type: 'asset',
                    parser: {
                        dataUrlCondition: {
                            maxSize: 10000
                        }
                    }
                }
            ]
        }
    };
}
