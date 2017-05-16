var path = require("path");

module.exports = {
    entry: ["./parts/Grid.jsx", "./parts/Search.jsx"],
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
                use: "babel-loader"
            },
             {
                test: /\.scss$/,
                exclude: /node_modules/,
                use: ["style-loader", "css-loader", "sass-loader"]
            }
        ]
    },
    devServer : {
        historyApiFallback: {
            index: 'search.html'
        }
    }
}
