import "../Search.scss";
import "!script-loader!../js/utilities.js";

import Mru from "./Mru";
import ErrorPage from "./ErrorPage";
import QueryStats from "./QueryStats";
import SearchHeader from "./SearchHeader";
import SearchBox from "./SearchBox";

import InfiniteScroll from "./InfiniteScroll";
import SplitPane from "./SplitPane";
import Start from "./Start";

import ResultDetails from "./ResultDetails";
import ResultListing from "./ResultListing";

window.configuration = require("../configuration/Configuration.jsx").default;

// SearchMain wraps the overall search UI
export default React.createClass({
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

        return {
            blockingErrorStatus: null,
            loading: false,

            tables: [],
            allCountData: [],
            listingData: [],
            page: 0,
            hasMoreData: false,
            selectedItemData: null,

            query: this.props.params.q || "",

            currentTable: table,
            currentTableSettings: {}, // {} denote no state, do not set to null.

            userSelectedTable: table,
            userTableSettings: {}, // {} denote no state, do not set to null.
            userSelectedId: this.props.params.open
        };
    },
    componentDidMount: function () {
        window.addEventListener("beforeunload", this); // For Mru
        this.mru = new Mru();

        // On Page load, find the list of known table namesz
        jsonQuery(configuration.url + "/allBasics",
            data => {
                if (!data.content) {
                    this.setState({ blockingErrorStatus: 401 });
                } else {
                    Object.values(data.content).forEach(table => table.idColumn = table.columns.find(col => col.isPrimaryKey).name || "");
                    this.setState({ allBasics: data.content }, this.runSearch);
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
            this.setState({
                allCountData: [],
                listingData: [],
                selectedItemData: null,
                userSelectedTable: undefined,
                userTableSettings: {},
                userSelectedId: undefined
            }, this.setHistory);
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
    onSelectionChanged: function (e) {
        this.setState({ userSelectedId: e }, this.getDetails);
    },
    onClose: function () {
        this.setState({ userSelectedId: undefined }, this.setHistory);
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
        }, this.runSearch);
    },
    onAddClause: function (name, value) {
        this.setState({ query: this.state.query + " AND [" + name + "]=\"" + value + "\"" }, this.runSearch);
    },
    onSetColumns: function (columns) {
        localStorage.mergeJson("table-" + this.state.currentTable, {
            columns: columns
        });

        // Clear the userSelectedColumns to and rely on getTableBasics to recalcuate it.
        this.setState({
            userSelectedTable: this.state.currentTable,
            userTableSettings: {}
        }, this.runSearch);
    },
    onSelectedTableChange: function (name) {
        this.setState({ userSelectedTable: name }, this.runSearch);
    },
    queryChanged: function (value) {
        this.setState({ query: value, userSelectedId: undefined }, this.delayedRunSearch);
    },
    delayedRunSearch: function () {
        // Only query every 250 milliseconds while typing
        if (!this.timer) {
            this.timer = window.setTimeout(this.runSearch, 250);
        }
    },
    runSearch: function () {
        this.timer = null;
        this.getAllCounts();
        this.setHistory();
    },
    getAllCounts: function () {
        // On query, ask for the count from every table.

        // If there's no query, clear results and do nothing else
        if (!this.state.query) {
            this.setState({
                loading: false,
                allCountData: [],
                listingData: [],
                selectedItemData: null,
                userTableSettings: {}
            });
            return;
        }

        // Notify any listeners (such as the loading animation).
        this.setState({ loading: true });

        // Get the count of matches from each accessible table
        this.jsonQueryWithError(
            configuration.url + "/allCount",
            data => {
                var currentTable = this.state.userSelectedTable || data.content.resultsPerTable[0].tableName;
                if (this.state.currentTable !== currentTable) {
                    this.setState({
                        userTableSettings: {},
                        userSelectedId: undefined
                    });
                }
                this.setState({
                    allCountData: data, 
                    currentTable: currentTable,
                    loading: false
                }, this.getTableBasics);

                data.content.parsedQuery = data.content.parsedQuery.replace(/\[\*\]:/g, ""); // Other consumers want the [*] removed also.
                this.mru.update(data.content.parsedQuery);
            },
            { q: this.state.query }
        );
    },
    getTableBasics: function () {
        // Once a table is selected, find out the columns and primary key column for the table
        var table = this.state.allBasics[this.state.currentTable];

        // Must write to userTableSettings (and not directly to currentTableSettings) so the URL can refect this.
        // If a table was switched getAllCounts would have wiped userTableSettings and localStorage would show through.
        // Sample schema: { columns: ["Name", "IP"], sortColumn: "IP", sortOrder: "desc" }
        var userTableSettings = localStorage.getJson("table-" + this.state.currentTable);

        // Set the ID column, all columns, and listing columns
        this.setState({
            userTableSettings: userTableSettings,
            currentTableSettings: Object.merge(
                { columns: [table.idColumn], sortColumn: table.idColumn, sortOrder: "asc" },
                configuration.listingDefaults && configuration.listingDefaults[this.state.currentTable],
                userTableSettings)
        }, () => {
            this.setHistory(); // Due to userTableSettings being set.
            if (this.state.query) this.getResultsPage();
            if (this.state.userSelectedId) this.getDetails();
        });
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
        // delayedRunSearch() would have to return before the other two.
        if (!this.state.currentTable) return;

        var detailsQuery = this.state.allBasics[this.state.currentTable].idColumn + '="' + this.state.userSelectedId + '"';
        if (this.state.query) detailsQuery = detailsQuery + " AND " + this.state.query;

        // Select all columns for the selected item, with highlighting
        this.jsonQueryWithError(
            configuration.url + "/table/" + this.state.currentTable,
            function (data) {
                if (data.content.values) {
                    this.setState({ selectedItemData: arribaRowToObject(data.content.values, 0) });
                } else {
                    if (!this.state.query) {
                        this.setState({ selectedItemData: null, error: "Item '" + this.state.userSelectedId + "' not found." })
                    } else {
                        this.setState({ selectedItemData: null, userSelectedId: undefined }, this.setHistory);
                    }
                }
            }.bind(this),
            {
                q: detailsQuery,
                c1: "*",
                action: "select",
                h: "π",
                s: 0,
                t: 1
            }
        );

        this.setHistory();
    },
    jsonQueryWithError: function (url, onSuccess, parameters) {
        jsonQuery(
            url,
            data => {
                this.setState({ error: undefined });
                onSuccess(data);
            },
            function (xhr, status, err) {
                this.setState({ allCountData: [], listingData: [], selectedItemData: null, loading: false, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
            }.bind(this),
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
        var parameters = {
            action: "select",
            q: this.state.query,
            ob: this.state.currentTableSettings.sortColumn,
            so: this.state.currentTableSettings.sortOrder,
            s: 0
        };

        addArrayParameters(parameters, "c", this.state.currentTableSettings.columns);

        var queryString = buildUrlParameters(parameters);
        return configuration.url + "/table/" + this.state.currentTable + queryString;
    },
    buildThisUrl: function (includeOpen) {
        var userTableSettings = this.state.userTableSettings;
        var relevantParams = ({
            t: this.state.userSelectedTable,
            q: this.state.query || undefined,
            ob: userTableSettings.sortColumn,
            so: userTableSettings.sortOrder
        }).cleaned;

        addArrayParameters(relevantParams, "c", userTableSettings.columns);
        if (Object.keys(userTableSettings).length) relevantParams.t = this.state.currentTable;

        if (includeOpen && this.state.userSelectedId) {
            relevantParams.open = this.state.userSelectedId;
        }

        return window.location.protocol + '//' + window.location.host + window.location.pathname + buildUrlParameters(relevantParams);
    },
    render: function () {
        if (this.state.blockingErrorStatus != null) return <ErrorPage status={this.state.blockingErrorStatus} />;

        var table = this.state.allBasics && this.state.currentTable && this.state.allBasics[this.state.currentTable] || undefined;
        var customDetailsView = (configuration.customDetailsProviders && configuration.customDetailsProviders[this.state.currentTable]) || ResultDetails;

        // Consider clearing the currentTable when the query is empty.
        var mainContent = this.state.query && this.state.allBasics && this.state.currentTable
            ? <SplitPane split="horizontal" minSize="300" isFirstVisible={this.state.listingData.content} isSecondVisible={this.state.userSelectedId}>
                <InfiniteScroll page={this.state.page} hasMoreData={this.state.hasMoreData} loadMore={this.getResultsPage }>
                    <ResultListing ref={"list"}
                        data={this.state.listingData}
                        allBasics={this.state.allBasics}
                        sortColumn={this.state.currentTableSettings.sortColumn}
                        sortOrder={this.state.currentTableSettings.sortOrder}
                        selectedId={this.state.userSelectedId}
                        onResort={this.onResort}
                        onSelectionChanged={this.onSelectionChanged}
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
            : <Start allBasics={this.state.allBasics} showHelp={this.props.params.help === "true"} queryChanged={this.queryChanged} />;

        var queryUrl = this.buildQueryUrl();
        var baseUrl = this.buildThisUrl(false);
        var rssUrl = queryUrl + "&fmt=rss&t=100&iURL=" + encodeURIComponent(baseUrl + "&open=");
        var csvUrl = queryUrl + "&fmt=csv&t=50000";

        var gridUrl = "/Grid.html" + buildUrlParameters({ t: this.state.currentTable, q: this.state.query });
        if (!this.state.query) gridUrl = "/Grid.html?p=default";

        return (
            <div className="viewport" onKeyDown={this.handleKeyDown}>
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
                    </nav>

                    <div className="center">
                        <QueryStats error={this.state.error}
                                    allCountData={this.state.allCountData}
                                    selectedData={this.state.listingData}
                                    rssUrl={rssUrl}
                                    csvUrl={csvUrl}
                                    currentTable={this.state.currentTable}
                                    onSelectedTableChange={this.onSelectedTableChange} />
                    
                        {mainContent}
                    </div>
                </div>
            </div>
        );
    }
});
