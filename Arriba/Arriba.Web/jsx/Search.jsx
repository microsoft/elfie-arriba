﻿import ErrorPage from "./ErrorPage";
import QueryStats from "./QueryStats";
import SearchHeader from "./SearchHeader";

import InfiniteScroll from "./InfiniteScroll";
import SplitPane from "./SplitPane";
import SyntaxHelp from "./SyntaxHelp";

import ResultDetails from "./ResultDetails";
import ResultListing from "./ResultListing";

// NOTE: Depends on configuration from zConfiguration.jsx.
import defaultConfiguration from "./DefaultConfiguration";
var configuration = defaultConfiguration;
var optionalContext = require.context("..", true, /\.\/configuration\/zConfiguration\.jsx/);
if (optionalContext.keys().includes("./configuration/zConfiguration.jsx")) {
    configuration = optionalContext("./configuration/zConfiguration.jsx").default
}

// SearchMain wraps the overall search UI
var SearchMain = React.createClass({
    getInitialState: function () {
        return {
            blockingErrorTitle: null,
            blockingErrorContent: null,

            tables: [],
            allCountData: [],
            listingData: [],
            selectedItemData: null,

            allItemCount: 0,

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
        this.getTables(function () {
            if (this.state.query) {
                // If there's a query, run it
                this.runSearch();
            } else if (this.state.userSelectedId) {
                // If there's an item to open, open it
                this.getTableBasics();
            }
        });
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
    onSearchChange: function (e) {
        this.setState({ query: e.target.value, userSelectedId: null }, this.delayedRunSearch);
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
    getTables: function (callback) {
        // On Page load, find the list of known table names
        jsonQuery(this.props.url,
            function (data) {
                this.setState({ tables: data.content, error: null }, callback);
            }.bind(this),
            function (xhr, status, err) {
                if (status === 401) {
                    this.setState({ blockingErrorTitle: "Access Denied", blockingErrorContent: this.props.accessDeniedContent });
                } else {
                    this.setState({ blockingErrorTitle: "Service Unavailable", blockingErrorContent: this.props.serviceUnavailableContent });
                }
                console.error(xhr.url, status, err.toString());
            }.bind(this)
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

            this.setState(cleared);
            return;
        }

        var params = { q: this.state.query };
        this.addPivotClauses(params);

        // Get the count of matches from each accessible table
        jsonQuery(
            this.props.url + "/allCount",
            function (data) {
                var tableToShow = this.state.userSelectedTable;
                if (!tableToShow) tableToShow = data.content[0].tableName;

                this.setState({ allCountData: data, currentTable: tableToShow, error: null }, this.getTableBasics);
            }.bind(this),
            function (xhr, status, err) {
                this.setState({ allCountData: [], listingData: [], selectedItemData: null,  error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
                console.error(xhr.url, status, err.toString());
            }.bind(this),
            params
        );
    },
    getTableBasics: function () {
        // Once a table is selected, find out the columns and primary key column for the table
        jsonQuery(this.props.url + "/table/" + this.state.currentTable,
            function (data) {
                var idColumn = "";

                // Find the ID column
                for (var j = 0; j < data.content.columns.length; ++j) {
                    if (data.content.columns[j].isPrimaryKey) {
                        idColumn = data.content.columns[j].name;
                        break;
                    }
                }

                // Choose columns, sort column, sort order
                var defaultsForTable = (this.props.listingDefaults ? this.props.listingDefaults[this.state.currentTable] : null);
                if (!defaultsForTable) defaultsForTable = {};

                var columns = firstNonEmptyArray(this.state.userSelectedColumns, defaultsForTable.columns, [idColumn]);
                var sortColumn = this.state.userSelectedSortColumn || defaultsForTable.sortColumn || idColumn;
                var sortOrder = this.state.userSelectedSortOrder || defaultsForTable.sortOrder || "asc";

                var next = function () {
                    if (this.state.query) this.getResultsPage();
                    if (this.state.userSelectedId) this.getDetails();
                };

                // Set the ID column, all columns, and listing columns
                this.setState(
                    {
                        currentTableIdColumn: idColumn,
                        currentTableAllColumns: data.content.columns,
                        currentListingColumns: columns,
                        currentSortColumn: sortColumn,
                        currentSortOrder: sortOrder,
                        error: null
                    }, next);
            }.bind(this),
            function (xhr, status, err) {
                this.setState({ allCountData: [], listingData: [], selectedItemData: null, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
                console.error(xhr.url, status, err.toString());
            }.bind(this)
        );
    },
    getResultsPage: function (i) {
        // Once the counts query runs and table basics are loaded, get a page of results

        // If there's no table, id column, or query, don't do anything yet
        if (!this.state.query || !this.state.currentTable || !this.state.currentTableIdColumn) return;

        // Get enough items to fill the requested page number (rather than trying to append one page)
        if (!i) i = 0;
        var pageSize = 50 * (i + 1);

        // Get a page of matches for the given query for the desired columns and sort order, with highlighting.
        jsonQuery(
            this.buildQueryUrl() + "&h=%CF%80&t=" + pageSize,
            function (data) {
                this.setState({ listingData: data, hasMoreData: data.content.total > pageSize, page: i, error: null });
            }.bind(this),
            function (xhr, status, err) {
                this.setState({ allCountData: [], listingData: [], selectedItemData: null, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
                console.error(xhr.url, status, err.toString());
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
        jsonQuery(
            this.props.url + "/table/" + this.state.currentTable,
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
            function (xhr, status, err) {
                this.setState({ allCountData: [], listingData: [], selectedItemData: null, error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
                console.error(xhr.url, status, err.toString());
            }.bind(this),
            params
        );

        this.setHistory();
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
        return this.props.url + "/table/" + this.state.currentTable + queryString;
    },
    buildThisUrl: function (includeOpen) {
        var relevantParams = {};
        this.addPivotClauses(relevantParams);

        if (this.state.userSelectedTable) relevantParams.t = this.state.userSelectedTable;
        if (this.state.query) relevantParams.q = this.state.query;        
        if (this.state.userSelectedSortColumn) relevantParams.ob = this.state.userSelectedSortColumn;
        if (this.state.userSelectedSortOrder === "desc") relevantParams.so = this.state.userSelectedSortOrder;

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
        if(this.state.blockingErrorTitle) return <ErrorPage title={this.state.blockingErrorTitle} message={this.state.blockingErrorContent} />;

        var detailsView = null;
        var customDetailsView = ResultDetails;
        if (this.props.customDetailsProviders) customDetailsView = this.props.customDetailsProviders[this.state.currentTable] || customDetailsView;

        detailsView = React.createElement(customDetailsView, { itemId: this.state.userSelectedId, table: this.state.currentTable, query: this.state.query, data: this.state.selectedItemData, onClose: this.onClose, onAddClause: this.onAddClause });

        var mainContent = <SyntaxHelp showHelp={this.props.params.help} splashContent={configuration.splashContent} />;
        if (this.state.query) {
            mainContent = (
                <SplitPane split="horizontal" minSize="300" isFirstVisible={this.state.listingData.content} isSecondVisible={this.state.userSelectedId}>
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
                        {detailsView}
                    </div>
                </SplitPane>
            );
        }

        var queryUrl = this.buildQueryUrl();
        var baseUrl = this.buildThisUrl(false);
        var rssUrl = queryUrl + "&fmt=rss&t=100&iURL=" + encodeURIComponent(baseUrl + "&open=");
        var csvUrl = queryUrl + "&fmt=csv&t=50000";

        var gridUrl = "/Grid.html" + buildUrlParameters({ t: this.state.currentTable, q: this.state.query });
        if (!this.state.query) gridUrl = "/Grid.html?p=Scorecard";

        return (
            <div className={"viewport " + configuration.theme} onKeyDown={this.handleKeyDown}>
                <SearchHeader name={configuration.toolName}
                              feedbackEmailAddresses={configuration.feedbackEmailAddresses}
                              query={this.state.query}
                              tables={this.state.tables}
                              allColumns={this.state.currentTableAllColumns}
                              onSearchChange={this.onSearchChange} />

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
    var params = getQueryStringParameters();
    ReactDOM.render(
        <SearchMain 
            url={configuration.url} 
            listingDefaults={configuration.listingDefaults} 
            customDetailsProviders={configuration.customDetailsProviders} 
            accessDeniedContent={configuration.accessDeniedContent}
            serviceUnavailableContent={configuration.serviceUnavailableContent}
            params={params} />,
        document.getElementById("searchContainer")
    );

    document.title = configuration.toolName;
}
