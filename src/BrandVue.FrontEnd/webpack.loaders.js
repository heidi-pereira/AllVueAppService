const loaderNames = {
    tsLoader: "ts-loader",
    babelLoader: "babel-loader",
    cssLoader: "css-loader",
    lessLoader: "less-loader",
    postCssLoader: "postcss-loader"
};

export default {
    tsLoader: {
        loader: loaderNames.tsLoader,
        options: {
            transpileOnly: true
        }
    },
    babelLoader: {
        loader: loaderNames.babelLoader,
        options: {
            presets: [
                "@babel/preset-typescript",
                "@babel/preset-react",
                [
                    "@babel/preset-env",
                    {
                        targets: "defaults and not IE 11",
                        modules: false,           // Keep ESModules for tree shaking
                        useBuiltIns: false        // Skip polyfills entirely
                    }
                ]
            ]
        }
    },
    cssLoader: {
        loader: loaderNames.cssLoader,
        options: {
            sourceMap: true,
        }
    },
    moduleCssLoader: {
        loader: loaderNames.cssLoader,
        options: {
            sourceMap: true,
            importLoaders:2,
            modules: {
                localIdentName: "[local]--[hash:base64:6]",
            },
            esModule: false // This is important for compatibility with Vue and other libraries that expect CommonJS modules
        }
    },
    lessLoader: {
        loader: loaderNames.lessLoader,
        options: {
            lessOptions: {
                math: 'parens-division'
            }
        }
    },
    postCssLoader: {
        loader: loaderNames.postCssLoader,
        options: {
            postcssOptions: {
                plugins: [
                    [
                        "postcss-preset-env",
                        {
                            "autoprefixer": {
                                "flexbox": "no-2009"
                            },
                            "stage": 3,
                            "features": {
                                "custom-properties": false
                            }
                        }
                    ]
                ]
            }
        }
    }
};