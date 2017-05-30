import Search from "./Search";
import Grid from "./Grid";
window.configuration = require("../configuration/Configuration.jsx").default;

class Index extends React.Component {
    render() {
        return window.location.pathname.startsWith("/Grid.html")
            ? <Grid params={getQueryStringParameters()} />
            : <Search params={getQueryStringParameters()} />
    }
}

ReactDOM.render(<Index />, document.getElementById("app"));
document.title = configuration.toolName;