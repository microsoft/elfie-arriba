import ErrorPage from "./ErrorPage";
import Search from "./Search";
import Grid from "./Grid";
window.configuration = require("../configuration/Configuration.jsx").default;

class Index extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            allBasics: [],
            blockingErrorStatus: null
        };
    }
    componentDidMount() {
        this.refreshAllBasics();
    }
    refreshAllBasics(then) {
        // On Page load, find the list of known table names
        jsonQuery(configuration.url + "/allBasics",
            data => {
                if (!data.content) {
                    this.setState({ blockingErrorStatus: 401 });
                } else {
                    Object.values(data.content).forEach(table => table.idColumn = table.columns.find(col => col.isPrimaryKey).name || "");
                    this.setState({ allBasics: data.content }, () => {
                        if (then) then();
                    });
                }
            },
            (xhr, status, err) => {
                this.setState({ blockingErrorStatus: status });
            }
        );
    }
    render() {
        if (this.state.blockingErrorStatus != null) return <ErrorPage status={this.state.blockingErrorStatus} />;

        const Page = window.location.pathname.startsWith("/Grid.html") ? Grid : Search;
        return <Page params={getQueryStringParameters()}
            allBasics={this.state.allBasics}
            refreshAllBasics={then => this.refreshAllBasics(then)} />
    }
}

ReactDOM.render(<Index />, document.getElementById("app"));
document.title = configuration.toolName;
