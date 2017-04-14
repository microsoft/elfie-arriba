require("../Search.scss");

import ErrorPage from "./ErrorPage";
import QueryStats from "./QueryStats";
import SearchHeader from "./SearchHeader";

import InfiniteScroll from "./InfiniteScroll";
import SplitPane from "./SplitPane";
import SyntaxHelp from "./SyntaxHelp";

import ResultDetails from "./ResultDetails";
import ResultListing from "./ResultListing";

// NOTE: Depends on configuration from zConfiguration.jsx.
import defaultConfiguration from "./DefaultConfiguration";
window.configuration = defaultConfiguration;
var optionalContext = require.context("..", true, /\.\/configuration\/Configuration\.jsx/);
if (optionalContext.keys().includes("./configuration/Configuration.jsx")) {
    window.configuration = optionalContext("./configuration/Configuration.jsx").default
}

// SearchMain wraps the overall search UI
var SearchMain = React.createClass({
    getInitialState: function () {
        return {
            blockingErrorStatus: null,

            tables: [],
            allCountData: [],
            listingData: [],
            selectedItemData: null,

            allItemCount: 0,
            loading: false,

            page: 0,
            hasMoreData: false,

            query: this.props.params.q || "",
            pivotQueries: [],

            currentTable: this.props.params.t,
            currentTableIdColumn: "",
            currentTableAllColumns: [],
            currentListingColumns: [],
            currentSortColumn: "",
            currentSortOrder: "",

            userSelectedTable: this.props.params.t,
            userSelectedColumns: getParameterArrayForPrefix(this.props.params, "c"),
            userSelectedSortColumn: this.props.params.ob,
            userSelectedSortOrder: this.props.params.so,
            userSelectedId: this.props.params.open
        };
    },
    getClearedUserSelections: function () {
        return {
            userSelectedTable: null,
            userSelectedColumns: [],
            userSelectedSortColumn: null,
            userSelectedSortOrder: null,
            userSelectedId: null,
            pivotQueries: []
        };
    },
    componentDidMount: function () {
        // Load table details
        this.getTables();

        if (this.state.query) {
            // If there's a query, run it
            this.runSearch();
        } else if (this.state.userSelectedId) {
            // If there's an item to open, open it
            this.getTableBasics();
        }
    },
    handleKeyDown: function (e) {
        // Backspace: Clear state *if query empty*
        if (e.keyCode === 8) {
            if (!this.state.query) {
                var cleared = this.getClearedUserSelections();
                cleared.allCountData = [];
                cleared.listingData = [];
                cleared.selectedItemData = null;

                this.setState(cleared, this.setHistory);
            }
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
        this.setState({ userSelectedId: null }, this.setHistory);
    },
    onResort: function (sortColumn, sortOrder) {
        // If a column heading was clicked, re-query with a new sort order
        this.setState({ userSelectedTable: this.state.currentTable, userSelectedSortColumn: sortColumn, userSelectedSortOrder: sortOrder }, this.runSearch);
    },
    onAddClause: function (name, value) {
        this.setState({ query: this.state.query + " AND [" + name + "]=\"" + value + "\"" }, this.runSearch);
    },
    onSetColumns: function (columns) {
        this.setState({ userSelectedTable: this.state.currentTable, userSelectedColumns: columns }, this.runSearch);
    },
    onSelectedTableChange: function (name) {
        if (this.state.currentTable === name) {
            // If the selected table is clicked, just mark it actively selected and fix the URL
            this.setState({ userSelectedTable: name }, this.setHistory);
        } else {
            // Otherwise, clear the columns/sort/sortOrder and query the new selected table
            var cleared = this.getClearedUserSelections();
            cleared.userSelectedTable = name;
            cleared.currentTable = name;
            this.setState(cleared, this.runSearch);
        }

    },
    onPivot: function (table, baseQuery) {
        var pivots = this.state.pivotQueries;
        pivots.push({ q: this.state.query, t: this.state.currentTable });
        this.setState({ pivotQueries: pivots, query: baseQuery, currentTable: table, userSelectedTable: table }, this.runSearch);
    },
    onSearchChange: function (value) {
        this.setState({ query: value, userSelectedId: null }, this.delayedRunSearch);
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
    getTables: function () {
        // On Page load, find the list of known table names
        jsonQuery(configuration.url,
            function (data) {
                this.setState({ tables: data.content, error: null });
            }.bind(this),
            (xhr, status, err) => {
                this.setState({ blockingErrorStatus: status });
                console.error(xhr.url, status, err.toString());
            }
        );
    },
    getAllCounts: function () {
        // On query, ask for the count from every table.

        // If there's no query, clear results and do nothing else
        if (!this.state.query) {
            var cleared = {};
            cleared.allCountData = [];
            cleared.listingData = [];
            cleared.selectedItemData = null;
            cleared.loading = false;

            this.setState(cleared);
            return;
        }

        // Notify any listeners (such as the loading animation).
        this.setState({ loading: true });

        var params = { q: this.state.query };
        this.addPivotClauses(params);

        // Get the count of matches from each accessible table
        this.jsonQueryWithError(
            configuration.url + "/allCount",
            function (data) {
                var tableToShow = this.state.userSelectedTable;
                if (!tableToShow) tableToShow = data.content[0].tableName;

                this.setState({ allCountData: data, currentTable: tableToShow, loading: false }, this.getTableBasics);
            }.bind(this),
            params
        );
    },
    getTableBasics: function () {
        // Once a table is selected, find out the columns and primary key column for the table
        this.jsonQueryWithError(configuration.url + "/table/" + this.state.currentTable, data => {
            var idColumn = data.content.columns.find(col => col.isPrimaryKey).name || "";

            // Choose columns, sort column, sort order
            var defaultsForTable = (configuration.listingDefaults && configuration.listingDefaults[this.state.currentTable]) || {};

            // Set the ID column, all columns, and listing columns
            this.setState({
                currentTableIdColumn: idColumn,
                currentTableAllColumns: data.content.columns,
                currentListingColumns: firstNonEmptyArray(this.state.userSelectedColumns, defaultsForTable.columns, [idColumn]),
                currentSortColumn: this.state.userSelectedSortColumn || defaultsForTable.sortColumn || idColumn,
                currentSortOrder: this.state.userSelectedSortOrder || defaultsForTable.sortOrder || "asc"
            }, () => {
                if (this.state.query) this.getResultsPage();
                if (this.state.userSelectedId) this.getDetails();
            });
        });
    },
    getResultsPage: function (i) {
        // Once the counts query runs and table basics are loaded, get a page of results

        // If there's no table, id column, or query, don't do anything yet
        if (!this.state.query || !this.state.currentTable || !this.state.currentTableIdColumn) return;

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

        // If there's no table or id column don't do anything yet
        if (!this.state.currentTable || !this.state.currentTableIdColumn) return;

        var detailsQuery = this.state.currentTableIdColumn + '="' + this.state.userSelectedId + '"';
        if (this.state.query) detailsQuery = detailsQuery + " AND " + this.state.query;

        var params = {
            q: detailsQuery,
            c1: "*",
            action: "select",
            h: "π",
            s: 0,
            t: 1
        };
        this.addPivotClauses(params);

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
                        this.setState({ selectedItemData: null, userSelectedId: null }, this.setHistory);
                    }
                }
            }.bind(this),
            params
        );

        this.setHistory();
    },
    jsonQueryWithError: function (url, onSuccess, parameters) {
        jsonQuery(
            url,
            data => {
                this.setState({ error: null });
                onSuccess(data);
            },
            function (xhr, status, err) {
                this.setState({ allCountData: [], listingData: [], selectedItemData: null, loading: false, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
                console.error(xhr.url, status, err.toString());
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
            ob: this.state.currentSortColumn,
            so: this.state.currentSortOrder,
            s: 0
        };

        addArrayParameters(parameters, "c", this.state.currentListingColumns);
        this.addPivotClauses(parameters);

        var queryString = buildUrlParameters(parameters);
        return configuration.url + "/table/" + this.state.currentTable + queryString;
    },
    buildThisUrl: function (includeOpen) {
        var relevantParams = {};
        this.addPivotClauses(relevantParams);

        if (this.state.userSelectedTable)                   relevantParams.t = this.state.userSelectedTable;
        if (this.state.query)                               relevantParams.q = this.state.query;        
        if (this.state.userSelectedSortColumn)              relevantParams.ob = this.state.userSelectedSortColumn;
        if (this.state.userSelectedSortOrder === "desc")    relevantParams.so = this.state.userSelectedSortOrder;

        for (var i = 0; i < this.state.userSelectedColumns.length; ++i) {
            relevantParams["c" + (i + 1).toString()] = this.state.userSelectedColumns[i];
        }

        if (includeOpen && this.state.userSelectedId) {
            relevantParams.open = this.state.userSelectedId;
        }

        return window.location.protocol + '//' + window.location.host + window.location.pathname + buildUrlParameters(relevantParams);
    },
    addPivotClauses: function (set) {
        for (var i = 0; i < this.state.pivotQueries.length; ++i) {
            set["q" + (i + 1)] = this.state.pivotQueries[i].q;
            set["t" + (i + 1)] = this.state.pivotQueries[i].t;
        }
    },
    render: function () {
        if (this.state.blockingErrorStatus != null) return <ErrorPage status={this.state.blockingErrorStatus} />;

        var customDetailsView = (configuration.customDetailsProviders && configuration.customDetailsProviders[this.state.currentTable]) || ResultDetails;

        var mainContent = this.state.query
            ? <SplitPane split="horizontal" minSize="300" isFirstVisible={this.state.listingData.content} isSecondVisible={this.state.userSelectedId}>
                <InfiniteScroll page={this.state.page} hasMoreData={this.state.hasMoreData} loadMore={this.getResultsPage }>
                    <ResultListing ref={"list"}
                        data={this.state.listingData}
                        idColumn={this.state.currentTableIdColumn}
                        allColumns={this.state.currentTableAllColumns}
                        sortColumn={this.state.currentSortColumn}
                        sortOrder={this.state.currentSortOrder}
                        selectedId={this.state.userSelectedId}
                        onResort={this.onResort}
                        onSelectionChanged={this.onSelectionChanged}
                        onSetColumns={this.onSetColumns}
                        onPivot={this.onPivot} />
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
            : <SyntaxHelp showHelp={this.props.params.help} />

        var queryUrl = this.buildQueryUrl();
        var baseUrl = this.buildThisUrl(false);
        var rssUrl = queryUrl + "&fmt=rss&t=100&iURL=" + encodeURIComponent(baseUrl + "&open=");
        var csvUrl = queryUrl + "&fmt=csv&t=50000";

        var gridUrl = "/Grid.html" + buildUrlParameters({ t: this.state.currentTable, q: this.state.query });
        if (!this.state.query) gridUrl = "/Grid.html?p=Scorecard";

        return (
            <div className={"viewport " + configuration.theme} onKeyDown={this.handleKeyDown}>
                <SearchHeader query={this.state.query}
                              tables={this.state.tables}
                              allColumns={this.state.currentTableAllColumns}
                              onSearchChange={this.onSearchChange}
                              loading={this.state.loading} />

                <div className="middle">
                    <nav className="mode theme-background-dark">
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

if (document.getElementById("searchContainer")) {
    ReactDOM.render(
        <SearchMain params={getQueryStringParameters()} />,
        document.getElementById("searchContainer")
    );

    document.title = configuration.toolName;
}
