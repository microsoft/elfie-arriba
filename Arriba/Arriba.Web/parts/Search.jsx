import "../Search.scss";
import "!script-loader!../js/utilities.js";
import "../js/utilities.jsx";

import Mru from "./Mru";
import ErrorPage from "./ErrorPage";
import QueryStats from "./QueryStats";
import SearchHeader from "./SearchHeader";
import SearchBox from "./SearchBox";
import Automator from "./Automator";
import DropShield from "./DropShield";

import InfiniteScroll from "./InfiniteScroll";
import SplitPane from "./SplitPane";
import Start from "./Start";

import ResultDetails from "./ResultDetails";
import ResultListing from "./ResultListing";

window.configuration = require("../configuration/Configuration.jsx").default;

const arrayToObject = (array, prefix) => {
    array = array || [];
    return array.reduce((o, col, i) => {
        o[`c${i + 1}`] = col;
        return o;
    }, {});
}

// SearchMain wraps the overall search UI
export default React.createClass({
    getEmptyState: function () {
        return {
            loading: false,
            allCountData: [],
            listingData: [],
            selectedItemData: null,
            userTableSettings: {}, // {} denote no state, do not set to null.
        }
    },
    getInitialState: function () {
        // For schema detection and possible migration.
        localStorage.setItem("version", 1);

        var table = this.props.params.t;
        var columns = getParameterArrayForPrefix(this.props.params, "c");

        if (table) {
            localStorage.mergeJson("table-" + table, ({
                columns: columns.emptyToUndefined(),
                sortColumn: this.props.params.ob || undefined, // Filter out empty strings.
                sortOrder: this.props.params.so || undefined
            }).cleaned);
        }

        return Object.assign(this.getEmptyState(), {
            blockingErrorStatus: null,
            allBasics: [],
            tables: [],
            page: 0,
            hasMoreData: false,
            query: this.props.params.q || "",
            currentTable: table,
            currentTableSettings: {}, // {} denote no state, do not set to null.
            userSelectedTable: table,
            userSelectedId: this.props.params.open
        });
    },
    componentDidMount: function () {
        window.addEventListener("beforeunload", this); // For Mru
        this.mru = new Mru();
        this.refreshAllBasics();
        this.componentDidUpdate({}, {});
    },
    componentDidUpdate: function(prevProps, prevState) {
        const diff = Object.diff(prevState, this.state);

        // Watching currentTable as sometimes inferred from the query.
        // Not watching for query changes (to match old behavior).
        if (diff.hasAny("allBasics", "currentTable", "userSelectedId")) {
            this.getDetails();
        }

        if (diff.hasAny("query", "userSelectedTable", "userTableSettings", "userSelectedId", "currentTable")) {
            this.setHistory();
        }
    },
    refreshAllBasics: function (then) {
        // On Page load, find the list of known table names
        jsonQuery(configuration.url + "/allBasics",
            data => {
                if (!data.content) {
                    this.setState({ blockingErrorStatus: 401 });
                } else {
                    Object.values(data.content).forEach(table => table.idColumn = table.columns.find(col => col.isPrimaryKey).name || "");
                    this.setState({ allBasics: data.content }, () => {
                        this.getAllCounts();
                        if (then) then();
                    });
                }
            },
            (xhr, status, err) => {
                this.setState({ blockingErrorStatus: status });
            }
        );
    },
    componentWillUnmount: function () {
        window.removeEventListener("beforeunload", this);
    },
    handleEvent: function (e) {
        // Assumed to be type="beforeunload" as we only subscribed for that.
        this.mru.push();
    },
    handleKeyDown: function (e) {
        // Backspace: Clear state *if query empty*
        if (e.keyCode === 8 && !this.state.query) {
            const state = Object.assign(
                this.getEmptyState(),
                { userSelectedTable: undefined, userSelectedId: undefined });
            this.setState(state);
        }

        // ESC: Close
        if (e.keyCode === 27) {
            this.onClose();
            e.stopPropagation();
        }

        // Up/Down: Open Previous/Next
        if (e.keyCode === 40 || e.keyCode === 38) {
            var indexChange = (e.keyCode === 40 ? 1 : -1);
            this.refs.list.selectByRelativeIndex(indexChange);
            e.stopPropagation();
        }
    },
    onClose: function () {
        this.setState({ userSelectedId: undefined });
    },
    onResort: function (sortColumn, sortOrder) {
        localStorage.mergeJson("table-" + this.state.currentTable, {
            sortColumn: sortColumn,
            sortOrder: sortOrder
        });

        // If a column heading was clicked, re-query with a new sort order
        this.setState({
            userSelectedTable: this.state.currentTable,
            userTableSettings: {}
        }, this.getAllCounts);
    },
    onAddClause: function (name, value) {
        this.setState({ query: this.state.query + " AND [" + name + "]=\"" + value + "\"" }, this.getAllCounts);
    },
    onSetColumns: function (columns, table) {
        localStorage.mergeJson("table-" + (table || this.state.currentTable), {
            columns: columns
        });

        // Clear the userSelectedColumns to and rely on getTableBasics to recalcuate it.
        this.setState({
            userSelectedTable: table || this.state.currentTable,
            userTableSettings: {}
        }, this.getAllCounts);
    },
    onSelectedTableChange: function (name) {
        this.setState({ userSelectedTable: name }, this.getAllCounts);
    },
    queryChanged: function (value) {
        // Only query every 250 milliseconds while typing
        this.setState(
            { query: value, userSelectedId: undefined },
            () => this.timer = this.timer || window.setTimeout(this.getAllCounts, 250)
        );
    },
    getAllCounts: function (then) {
        // On query, ask for the count from every table.
        this.timer = null;

        // If there's no allBasics or query, clear results and do nothing else
        if (!this.state.allBasics || !Object.keys(this.state.allBasics).length || !this.state.query) {
            this.setState(this.getEmptyState());
            return;
        }

        // Notify any listeners (such as the loading animation).
        this.setState({ loading: true });

        // Get the count of matches from each accessible table
        this.jsonQueryWithError(
            configuration.url + "/allCount",
            data => {
                // Do not wipe userSelectedId if currentTable is going from `undefined` to defined.
                // Scnario: On page load with open=something and table inferred from query.
                var currentTable = this.state.userSelectedTable || data.content.resultsPerTable[0].tableName;
                if (this.state.currentTable && this.state.currentTable !== currentTable) {
                    this.setState({
                        userTableSettings: {},
                        userSelectedId: undefined
                    });
                }
                this.setState({
                    allCountData: data,
                    currentTable: currentTable,
                    loading: false
                }, () => {
                    this.getTableBasics();
                    if (then) then();
                });

                data.content.parsedQuery = data.content.parsedQuery.replace(/\[\*\]:/g, ""); // Other consumers want the [*] removed also.
                this.mru.update(data.content.parsedQuery);
            },
            { q: this.state.query }
        );
    },
    getTableBasics: function () {
        // Once a table is selected, find out the columns and primary key column for the table

        // If allBasics is not ready, abort.
        const table = this.state.allBasics[this.state.currentTable];
        if (!table) return;

        // Must write to userTableSettings (and not directly to currentTableSettings) so the URL can refect this.
        // If a table was switched getAllCounts would have wiped userTableSettings and localStorage would show through.
        // Sample schema: { columns: ["Name", "IP"], sortColumn: "IP", sortOrder: "desc" }
        var userTableSettings = localStorage.getJson("table-" + this.state.currentTable, {});

        // Set the ID column, all columns, and listing columns
        this.setState({
            userTableSettings: userTableSettings,
            currentTableSettings: Object.merge(
                { columns: [table.idColumn], sortColumn: table.idColumn, sortOrder: "asc" },
                configuration.listingDefaults && configuration.listingDefaults[this.state.currentTable],
                userTableSettings)
        }, this.getResultsPage);
    },
    getResultsPage: function (i) {
        // Once the counts query runs and table basics are loaded, get a page of results

        // If there's no query, or current table, don't do anything yet
        if (!this.state.query || !this.state.currentTable) return;

        // Get enough items to fill the requested page number (rather than trying to append one page)
        if (!i) i = 0;
        var pageSize = 50 * (i + 1);

        // Get a page of matches for the given query for the desired columns and sort order, with highlighting.
        this.jsonQueryWithError(
            this.buildQueryUrl() + "&h=%CF%80&t=" + pageSize,
            function (data) {
                this.setState({ listingData: data, hasMoreData: data.content.total > pageSize, page: i });
            }.bind(this)
        );
    },
    getDetails: function () {
        // When an item is selected, get details for it

        // If there's no table don't do anything yet.
        // Unlikely to reach this function before currentTable and allBasics are set.
        // Delayed getAllCounts() would have to return before the other two.
        const table = this.state.allBasics[this.state.currentTable];
        if (!table) return;
        if (!this.state.userSelectedId) return;

        var detailsQuery = table.idColumn + '="' + this.state.userSelectedId + '"';
        if (this.state.query) detailsQuery += " AND (" + this.state.query + ")"; // Query is included for term highlighting.

        // Select all columns for the selected item, with highlighting
        this.jsonQueryWithError(
            configuration.url + "/table/" + this.state.currentTable,
            data => {
                if (data.content.values) {
                    this.setState({ selectedItemData: arribaRowToObject(data.content.values, 0) });
                } else {
                    if (!this.state.query) {
                        this.setState({ selectedItemData: null, error: "Item '" + this.state.userSelectedId + "' not found." })
                    } else {
                        this.setState({ selectedItemData: null, userSelectedId: undefined });
                    }
                }
            },
            {
                q: detailsQuery,
                c1: "*",
                action: "select",
                h: "π",
                s: 0,
                t: 1
            }
        );
    },
    jsonQueryWithError: function (url, onSuccess, parameters) {
        jsonQuery(
            url,
            data => {
                this.setState({ error: undefined });
                onSuccess(data);
            },
            (xhr, status, err) => {
                const state = Object.assign(
                    this.getEmptyState(),
                    { error: `Error: Server didn't respond to [${xhr.url}]. ${err}` });
                this.setState(state);
            },
            parameters
        );
    },
    setHistory: function () {
        var url = this.buildThisUrl(true);
        if (url !== window.location.href) {
            history.pushState("", "", url);
        }
    },
    buildQueryUrl: function () {
        var parameters = Object.merge(
            {
                action: "select",
                q: this.state.query,
                ob: this.state.currentTableSettings.sortColumn,
                so: this.state.currentTableSettings.sortOrder,
                s: 0
            },
            arrayToObject(this.state.currentTableSettings.columns, `c`)
        );
        return `${configuration.url}/table/${this.state.currentTable}${buildUrlParameters(parameters)}`;
    },
    buildThisUrl: function (includeOpen) {
        var userTableSettings = this.state.userTableSettings;
        var parameters = Object.merge(
            {
                t: Object.keys(userTableSettings).length ? this.state.currentTable : this.state.userSelectedTable,
                q: this.state.query || undefined,
                ob: userTableSettings.sortColumn,
                so: userTableSettings.sortOrder,
                open: includeOpen && this.state.userSelectedId || undefined,
            },
            arrayToObject(userTableSettings.columns, `c`)
        );
        return `${location.protocol}//${location.host + location.pathname + buildUrlParameters(parameters)}`;
    },
    render: function () {
        // Consider clearing the currentTable when the query is empty.
        if (this.state.blockingErrorStatus != null) return <ErrorPage status={this.state.blockingErrorStatus} />;

        var table = this.state.allBasics && this.state.currentTable && this.state.allBasics[this.state.currentTable] || undefined;
        var customDetailsView = (configuration.customDetailsProviders && configuration.customDetailsProviders[this.state.currentTable]) || ResultDetails;
        const queryUrl = this.buildQueryUrl();
        const gridUrl = this.state.query
            ? `/Grid.html${buildUrlParameters({ t: this.state.currentTable, q: this.state.query })}`
            : `/Grid.html?p=default`

        return <div ref="viewport" className="viewport" onKeyDown={this.handleKeyDown}
            onDragEnter={e => {
                // Consider disabling pointer events for perf.
                if (!this.state.dropping) this.setState({ dropping: true, file: undefined })
            }} >
            <SearchHeader>
                <SearchBox query={this.state.query}
                    parsedQuery={this.state.allCountData.content && this.state.allCountData.content.parsedQuery}
                    queryChanged={this.queryChanged}
                    loading={this.state.loading} />
            </SearchHeader>
            <div className="middle">
                <nav className="mode">
                    <a className="selected"><i className="icon-details"></i><span>Listing</span></a>
                    <a href={gridUrl}><i className="icon-view-all-albums"></i><span>Grid</span></a>
                    <Automator />
                </nav>
                <div className="center">
                    <QueryStats query={this.state.query}
                                error={this.state.error}
                                allCountData={this.state.allCountData}
                                allBasics={this.state.allBasics}
                                selectedData={this.state.listingData}
                                rssUrl={`${queryUrl}&fmt=rss&t=100&iURL=${encodeURIComponent(this.buildThisUrl(false) + "&open=")}`}
                                csvUrl={`${queryUrl}&fmt=csv&t=50000`}
                                currentTable={this.state.currentTable}
                                onSelectedTableChange={this.onSelectedTableChange}
                                refreshAllBasics={this.refreshAllBasics} />
                    {this.state.query && table
                        ? <SplitPane split="horizontal" minSize="300" isFirstVisible={this.state.listingData.content} isSecondVisible={this.state.userSelectedId}>
                            <InfiniteScroll page={this.state.page} hasMoreData={this.state.hasMoreData} loadMore={this.getResultsPage }>
                                <ResultListing ref={"list"}
                                    data={this.state.listingData}
                                    allBasics={this.state.allBasics}
                                    sortColumn={this.state.currentTableSettings.sortColumn}
                                    sortOrder={this.state.currentTableSettings.sortOrder}
                                    selectedId={this.state.userSelectedId}
                                    onResort={this.onResort}
                                    onSelectionChanged={id => this.setState({ userSelectedId: id })}
                                    onSetColumns={this.onSetColumns} />
                            </InfiniteScroll>
                            <div className="scrollable">
                                {React.createElement(customDetailsView, {
                                    itemId: this.state.userSelectedId,
                                    table: this.state.currentTable,
                                    query: this.state.query,
                                    data: this.state.selectedItemData,
                                    onClose: this.onClose,
                                    onAddClause: this.onAddClause
                                })}
                            </div>
                        </SplitPane>
                        : <Start allBasics={this.state.allBasics} showHelp={this.props.params.help === "true"} queryChanged={this.queryChanged} />}
                </div>
            </div>
            <DropShield
                dropping={this.state.dropping}
                droppingChanged={d => this.setState({ dropping: d })}
                existingTablenames={Object.keys(this.state.allBasics || {})}
                queryChanged={this.queryChanged}
                refreshAllBasics={this.refreshAllBasics}
                getAllCounts={this.getAllCounts}
                columnsChanged={this.onSetColumns} />
        </div>
    }
});
