var path = require("path");

module.exports = {
    entry: ["./jsx/Grid.jsx", "./jsx/Search.jsx"],
    output: {
        path: path.join(__dirname, "lib"),
        filename: "Search.js"
    },
    devtool: 'source-map',
    resolve: {
        extensions: [".jsx"]
    },
    module: {
        loaders: [
            {
                test: /\.jsx$/,
                exclude: /node_modules/,
                loader: "babel-loader"
            }
        ]
    }
}
