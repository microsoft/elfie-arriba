import "./Index.scss";
import SvgPage from "../icons/page.svg";
import SvgGrid from "../icons/grid.svg";

import EventedComponent from "./EventedComponent";
import Mru from "./Mru";
import ErrorPage from "./ErrorPage";
import Help from "./Help";
import Tabs from "./Tabs";
import SearchBox from "./SearchBox";
import Search from "./Search";
import Grid from "./Grid";
window.configuration = require("../configuration/Configuration.jsx").default;

// For schema detection and possible migration.
localStorage.setItem("version", 1);

class Index extends EventedComponent {
    constructor(props) {
        super(props);
        this.mru = new Mru();

        this.params = getQueryStringParameters();
        const table = this.params.t;
        const columns = getParameterArrayForPrefix(this.params, "c");

        if (table) {
            localStorage.mergeJson("table-" + table, ({
                columns: columns.emptyToUndefined(),
                sortColumn: this.params.ob || undefined, // Filter out empty strings.
                sortOrder: this.params.so || undefined
            }).cleaned);
        }
        const query = this.params.q || "";

        this.state = {
            blockingErrorStatus: null,
            allBasics: {},
            query: query,
            debouncedQuery: query, // Required to trigger getCounts.
            currentTable: table,
            userSelectedTable: table,
            mode:
                window.location.pathname.startsWith("/help") ? "help" :
                window.location.pathname.startsWith("/Grid.html") ? "grid" :
                undefined // Implies "search".
        };

        this.events = {
            "beforeunload": e => this.mru.push(),
        }
    }
    componentDidMount() {
        super.componentDidMount();
        window.errorBar = msg => this.setState({ error: msg});
        this.refreshAllBasics();
        this.componentDidUpdate({}, {});
    }
    componentDidUpdate(prevProps, prevState) {
        // const diffProps = Object.diff(prevProps, this.props);
        const diffState = Object.diff(prevState, this.state);

        if (diffState.hasAny("query")) {
            // Only query every 250 milliseconds while typing
            this.timer = this.timer || window.setTimeout(() => this.setState({ debouncedQuery: this.state.query }), 250);
        }

        // Depends on allBasics because if a table is deleted, we don't want stale query results.
        if (diffState.hasAny("allBasics", "debouncedQuery")) {
            this.getCounts();
        }

        if (diffState.hasAny("userSelectedTable", "counts")) {
            const currentTable = this.state.userSelectedTable || this.state.counts && this.state.counts.resultsPerTable[0].tableName;
            this.setState({ currentTable: currentTable });
        }
    }

    onKeyDown(e) {
        // Backspace: Clear state *if query empty*
        if (e.keyCode === 8 && !this.state.query && this.state.userSelectedTable) {
            this.setState({ userSelectedTable: undefined });
        }
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
    getCounts(then) {
        // On query, ask for the count from every table.
        this.timer = null;

        if (!this.state.query || !Object.keys(this.state.allBasics).length) {
            this.setState({ counts: undefined, loading: false });
            return;
        }

        // Notify any listeners (such as the loading animation).
        this.setState({ loading: true });

        // Get the count of matches from each accessible table
        xhr("allCount", { q: this.state.query })
            .then(data => {
                this.setState({ counts: data, loading: false }, then);

                data.parsedQuery = data.parsedQuery.replace(/\[\*\]:/g, ""); // Other consumers want the [*] removed also.
                this.mru.update(data.parsedQuery);
            });
    }

    render() {
        if (this.state.blockingErrorStatus != null) return <ErrorPage status={this.state.blockingErrorStatus} />;
        const Page = { grid: Grid, help: Help }[this.state.mode] || Search;
        return <div className="viewport" onKeyDown={this.onKeyDown.bind(this)}>
            {this.state.error && <div className="errorBar">{this.state.error}</div>}
            <div className="header">
                <a className="title font-light" onClick={() => {
                    this.setState({ mode: undefined, query: "", userSelectedTable: undefined });
                    this.refs.searchBox.focus();
                }}>{configuration.toolName}</a>
                <Tabs
                    allBasics={this.state.allBasics}
                    refreshAllBasics={this.refreshAllBasics.bind(this)}

                    query={this.state.query}
                    queryUrl={this.state.queryUrl}
                    thisUrl={this.state.thisUrl}

                    currentTable={this.state.currentTable}
                    userSelectedTable={this.state.userSelectedTable}
                    userSelectedTableChanged={name => this.setState({ userSelectedTable: name })}

                    counts={this.state.counts}>

                    <SearchBox ref="searchBox"
                        query={this.state.query}
                        parsedQuery={this.state.counts && this.state.counts.parsedQuery}
                        queryChanged={query => this.setState({ query: query })}
                        userSelectedTable={this.state.userSelectedTable}
                        loading={this.state.loading} />

                </Tabs>
            </div>
            <div className="middle">
                <div className="mode">
                    <a title="Listing" className={this.state.mode === undefined ? "selected" : undefined}
                        onClick={e => this.setState({ mode: undefined }) }><SvgPage /></a>
                    <a title="Grid" className={this.state.mode === "grid" ? "selected" : undefined}
                        onClick={e => this.setState({ mode: "grid" }) }><SvgGrid /></a>
                    <span className="mode-fill"></span>
                    <a title="Feedback" href={"mailto:" + encodeURIComponent(configuration.feedbackEmailAddresses) + "?subject=" + encodeURIComponent(configuration.toolName) + " Feedback"}>
                        <img src="/icons/feedback.svg" alt="feedback"/>
                    </a>
                    <a>
                        <img src="/icons/help.svg" alt="help" title="Help" onClick={() => this.setState({ mode: this.state.mode === "help" ? undefined : "help" })}/>
                    </a>
                </div>
                <Page params={this.params}
                    allBasics={this.state.allBasics}
                    refreshAllBasics={then => this.refreshAllBasics(then)}

                    query={this.state.query}
                    debouncedQuery={this.state.debouncedQuery}
                    queryChanged={query => this.setState({ query: query })}

                    userSelectedTable={this.state.userSelectedTable}
                    userSelectedTableChanged={table => this.setState({ userSelectedTable: table })}
                    currentTable={this.state.currentTable}

                    queryUrlChanged={url => this.setState({ queryUrl: url })}
                    thisUrlChanged={url => this.setState({ thisUrl: url })}

                    getCounts={this.getCounts.bind(this)}
                    />
            </div>
        </div>
    }
}

ReactDOM.render(<Index />, document.getElementById("app"));
document.title = configuration.toolName;
