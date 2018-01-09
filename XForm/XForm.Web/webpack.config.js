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
                        presets: ["babel-preset-es2015", "babel-preset-react"],
                    	plugins: [
                    		["babel-plugin-transform-es2015-arrow-functions", { "spec": true }],
                    		"babel-plugin-transform-object-rest-spread",
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
        port: 8081,
        historyApiFallback: {
            rewrites: [
                { from: /./, to: 'index.html' },
            ]
        }
    }
}
