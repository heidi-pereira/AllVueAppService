import { merge } from "webpack-merge";
import common from "./webpack.common.js";
import ExtractCssPlugin from "mini-css-extract-plugin";
import TerserPlugin from "terser-webpack-plugin";
import CssMinimizerPlugin from "css-minimizer-webpack-plugin";

export default env => {
    return merge(common(env),
        {
            output: {
                filename: "[name].[contenthash].js",
            },
            mode: 'production',
            optimization: {
                runtimeChunk: 'single',
                splitChunks: {
                    chunks: 'all'
                },
                minimizer: [
                    new TerserPlugin({
                        parallel: true,
                        terserOptions: {
                            mangle: true,
                            compress: true,
                            ecma: 5,
                            ie8: true
                        }
                    }),
                    new CssMinimizerPlugin({
                        minimizerOptions: {
                            preset: [
                                'default', {
                                    discardComments: {
                                        removeAll: true
                                    }
                                }
                            ]
                        }
                    })
                ]
            },
            plugins: [
                new ExtractCssPlugin({
                    filename: "[name].[contenthash].css",
                    chunkFilename: "[name].[contenthash].css"
                }),
            ]
        });
}