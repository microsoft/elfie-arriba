var path = require("path");

module.exports = {
    entry: "./Index.jsx",
    output: {
        path: __dirname,
        filename: "bundle.js"
    },
    devtool: 'source-map',
    resolve: {
        extensions: [".js", ".jsx"]
    },
    module: {
        rules: [
            {
                test: /\.jsx$/,
                exclude: /node_modules/,
                use: {
                    loader: "babel-loader",
                    options: {
                        presets: [
                            ["@babel/preset-env", { targets: { chrome: 64 } }],
                            "@babel/preset-react",
                        ],
                        plugins: [
                            ["@babel/plugin-proposal-decorators", { "legacy": true }],
                            "@babel/plugin-proposal-optional-chaining",
                        ],
                    }
                },
            },
             {
                test: /\.scss$/,
                exclude: /node_modules/,
                use: ["style-loader", "css-loader", "sass-loader"]
            },
        ]
    },
    devServer : {
        host: "0.0.0.0",
        port: 8080,
        historyApiFallback: {
            rewrites: [
                { from: /./, to: 'index.html' },
            ]
        }
    },
    performance: {
        maxAssetSize: 300000,
        maxEntrypointSize: 300000,
    },
}
