import { merge } from "webpack-merge";
import common from "./webpack.common.js";
import ExtractCssPlugin from "mini-css-extract-plugin";

const webpackDevServerPort = 8082;
const proxyTarget = "http://localhost:8491";

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

export default env => {
    return merge(common(env),
        {
            output: {
                filename: "[name].js",
                publicPath: '/dist/',
            },
            mode: 'development',
            devtool: 'inline-source-map',
            devServer: {
                compress: true,
                proxy: [{
                    context: ()=>true,
                    target: proxyTarget,
                }],
                port: webpackDevServerPort
            },
            plugins: [
                new PrintCompilationTime(),
                new ExtractCssPlugin({
                    filename: "[name].css",
                    chunkFilename: "[name].css"
                }),
            ]
        });
}