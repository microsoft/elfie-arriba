import "../Search.scss";
import "!script-loader!../js/utilities.js";
import "../js/utilities.jsx";

import Mru from "./Mru";
import QueryStats from "./QueryStats";
import SearchHeader from "./SearchHeader";
import Tabs from "./Tabs";
import SearchBox from "./SearchBox";
import Mode from "./Mode";
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
        this.componentDidUpdate({}, {});
    },
    componentDidUpdate: function(prevProps, prevState) {
        const diffProps = Object.diff(prevProps, this.props);
        const diff = Object.diff(prevState, this.state);

        if (diffProps.hasAny("allBasics") || diff.hasAny("query")) {
            this.getAllCounts();
        }

        if (diff.hasAny("userSelectedTable", "allCountData") && this.state.allCountData.content) {
            const currentTable = this.state.userSelectedTable || this.state.allCountData.content.resultsPerTable[0].tableName;
            this.setState({ currentTable: currentTable });
        }

        // Do not wipe userSelectedId if currentTable is going from `undefined` to defined.
        // Scnario: On page load with open=something and table inferred from query.
        if (diff.hasAny("currentTable") && prevState.currentTable) {
            this.setState({ userSelectedId: undefined });
        }

        if (diffProps.hasAny("allBasics") || diff.hasAny("currentTable")) {
            const table = this.props.allBasics[this.state.currentTable];
            if (!table) return;

            // Must write to userTableSettings (and not directly to currentTableSettings) so the URL can refect this.
            // Sample schema: { columns: ["Name", "IP"], sortColumn: "IP", sortOrder: "desc" }
            var userTableSettings = localStorage.getJson("table-" + this.state.currentTable, {});
            this.setState({
                userTableSettings: userTableSettings,
                currentTableSettings: Object.merge(
                    { columns: [table.idColumn], sortColumn: table.idColumn, sortOrder: "asc" },
                    configuration.listingDefaults && configuration.listingDefaults[this.state.currentTable],
                    userTableSettings)
            });
        }

        if (diff.hasAny("query", "currentTableSettings", "page")) {
            this.getListings();
        }

        // Watching currentTable as sometimes inferred from the query.
        // Not watching for query changes (to match old behavior).
        if (diffProps.hasAny("allBasics") || diff.hasAny("currentTable", "userSelectedId")) {
            this.getDetails();
        }

        if (diff.hasAny("query", "userSelectedTable", "userTableSettings", "userSelectedId", "currentTable")) {
            var url = this.buildThisUrl(true);
            if (url !== window.location.href) {
                history.pushState("", "", url);
            }
        }
    },
    componentWillUnmount: function () {
        window.removeEventListener("beforeunload", this);
    },
    handleEvent: function (e) {
        // Assumed to be type="beforeunload" as we only subscribed for that.
        this.mru.push();
    },

    onKeyDown: function (e) {
        // Backspace: Clear state *if query empty*
        if (e.keyCode === 8 && !this.state.query) {
            const state = Object.assign(
                this.getEmptyState(),
                { userSelectedTable: undefined, userSelectedId: undefined });
            this.setState(state);
        }

        // ESC: Close
        if (e.keyCode === 27) {
            this.setState({ userSelectedId: undefined });
            e.stopPropagation();
        }

        // Up/Down: Open Previous/Next
        if (e.keyCode === 40 || e.keyCode === 38) {
            var indexChange = (e.keyCode === 40 ? 1 : -1);
            this.refs.list.selectByRelativeIndex(indexChange);
            e.stopPropagation();
        }
    },
    onResort: function (sortColumn, sortOrder) {
        localStorage.mergeJson("table-" + this.state.currentTable, {
            sortColumn: sortColumn,
            sortOrder: sortOrder
        });

        // If a column heading was clicked, re-query with a new sort order
        this.setState({
            userSelectedTable: this.state.currentTable
        }, this.getAllCounts);
    },
    onSetColumns: function (columns, table) {
        localStorage.mergeJson("table-" + (table || this.state.currentTable), {
            columns: columns
        });
        this.setState({
            userSelectedTable: table || this.state.currentTable
        }, this.getAllCounts);
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
        if (!this.props.allBasics || !Object.keys(this.props.allBasics).length || !this.state.query) {
            this.setState(this.getEmptyState());
            return;
        }

        // Notify any listeners (such as the loading animation).
        this.setState({ loading: true });

        // Get the count of matches from each accessible table
        this.jsonQueryWithError(
            configuration.url + "/allCount",
            data => {
                this.setState({
                    allCountData: data,
                    loading: false
                }, then);

                data.content.parsedQuery = data.content.parsedQuery.replace(/\[\*\]:/g, ""); // Other consumers want the [*] removed also.
                this.mru.update(data.content.parsedQuery);
            },
            { q: this.state.query }
        );
    },
    getListings: function () {
        if (!this.state.query ||
            !this.state.currentTable ||
            !Object.keys(this.state.currentTableSettings).length) return;
        var rowCount = 50 * (this.state.page + 1);
        this.jsonQueryWithError(
            this.buildQueryUrl() + "&h=%CF%80&t=" + rowCount,
            data => this.setState({ listingData: data, hasMoreData: data.content.total > rowCount })
        );
    },
    getDetails: function () {
        // When an item is selected, get details for it

        // If there's no table don't do anything yet.
        // Unlikely to reach this function before currentTable and allBasics are set.
        // Delayed getAllCounts() would have to return before the other two.
        const table = this.props.allBasics[this.state.currentTable];
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

        var table = this.props.allBasics && this.state.currentTable && this.props.allBasics[this.state.currentTable] || undefined;
        var customDetailsView = (configuration.customDetailsProviders && configuration.customDetailsProviders[this.state.currentTable]) || ResultDetails;
        const queryUrl = this.buildQueryUrl();

        return <div ref="viewport" className="viewport" onKeyDown={this.onKeyDown}
            onDragEnter={e => {
                // Consider disabling pointer events for perf.
                if (!this.state.dropping) this.setState({ dropping: true, file: undefined })
            }} >
            <SearchHeader>
                <Tabs
                    allBasics={this.props.allBasics}
                    allCountData={this.state.allCountData}
                    currentTable={this.state.currentTable}
                    listingDataContent={this.state.listingData && this.state.listingData.content}
                    query={this.state.query}
                    queryUrl={queryUrl}
                    thisUrl={this.buildThisUrl(false)}
                    onSelectedTableChange={name => this.setState({ userSelectedTable: name })}
                    refreshAllBasics={this.props.refreshAllBasics}>

                    <SearchBox query={this.state.query}
                        parsedQuery={this.state.allCountData.content && this.state.allCountData.content.parsedQuery}
                        queryChanged={this.queryChanged.bind(this)}
                        loading={this.state.loading} />

                </Tabs>
            </SearchHeader>
            <div className="middle">
                <Mode query={this.state.query} currentTable={this.state.currentTable} />
                <div className="center">
                    <QueryStats error={this.state.error} selectedData={this.state.listingData} />
                    {this.state.query && table
                        ? <SplitPane split="horizontal" minSize="300" isFirstVisible={this.state.listingData.content} isSecondVisible={this.state.userSelectedId}>
                            <InfiniteScroll hasMoreData={this.state.hasMoreData} loadMore={() => this.setState({ page: this.state.page + 1 })}>
                                <ResultListing ref={"list"}
                                    data={this.state.listingData}
                                    allBasics={this.props.allBasics}
                                    sortColumn={this.state.currentTableSettings.sortColumn}
                                    sortOrder={this.state.currentTableSettings.sortOrder}
                                    selectedId={this.state.userSelectedId}
                                    onResort={this.onResort.bind(this)}
                                    onSelectionChanged={id => this.setState({ userSelectedId: id })}
                                    onSetColumns={this.onSetColumns} />
                            </InfiniteScroll>
                            <div className="scrollable">
                                {React.createElement(customDetailsView, {
                                    itemId: this.state.userSelectedId,
                                    table: this.state.currentTable,
                                    query: this.state.query,
                                    data: this.state.selectedItemData,
                                    onClose: () => this.setState({ userSelectedId: undefined }),
                                    onAddClause: (name, value) => this.setState({ query: `${this.state.query} AND [${name}]="${value}"` })
                                })}
                            </div>
                        </SplitPane>
                        : <Start allBasics={this.props.allBasics} showHelp={this.props.params.help === "true"} queryChanged={this.queryChanged.bind(this)} />}
                </div>
            </div>
            <DropShield
                dropping={this.state.dropping}
                droppingChanged={d => this.setState({ dropping: d })}
                existingTablenames={Object.keys(this.props.allBasics || {})}
                queryChanged={this.queryChanged.bind(this)}
                refreshAllBasics={this.props.refreshAllBasics}
                getAllCounts={this.getAllCounts.bind(this)}
                columnsChanged={this.onSetColumns.bind(this)} />
        </div>
    }
});
