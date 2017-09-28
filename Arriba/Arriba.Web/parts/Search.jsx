import "../Search.scss";
import "!script-loader!../js/utilities.js";
import "../js/utilities.jsx";

import EventedComponent from "./EventedComponent";
import QueryStats from "./QueryStats";
import Automator from "./Automator";
import DropShield from "./DropShield";

import InfiniteScroll from "./InfiniteScroll";
import SplitPane from "./SplitPane";
import Start from "./Start";

import ResultDetails from "./ResultDetails";
import AddColumnList from "./AddColumnList";
import ResultListing from "./ResultListing";

import createDOMPurify  from "DOMPurify";
const DOMPurify = createDOMPurify(window); // Consider lazy instantiation.

window.configuration = require("../configuration/Configuration.jsx").default;

const arrayToObject = (array, prefix) => {
    array = array || [];
    return array.reduce((o, col, i) => {
        o[`c${i + 1}`] = col;
        return o;
    }, {});
}

// SearchMain wraps the overall search UI
export default class Search extends EventedComponent {
    constructor(props) {
        super(props);

        const query = this.props.params.q || "";
        this.state = {
            tables: [],
            page: 0,
            currentTableSettings: {}, // {} denote no state, do not set to null.
            userSelectedId: this.props.params.open
        };

        this.events = {
            "storage": e => { if (e.key.startsWith("table-")) this.getTableSettings() },
            "keydown": e => this.onKeyDown(e),
        };
    }
    componentDidMount() {
        super.componentDidMount();
        this.componentDidUpdate({}, {});
    }
    componentDidUpdate(prevProps, prevState) {
        // Note: The order of the if-statements do not strictly imply sequence/dependency.
        // Note: The inputs to each if-statement do not strictly imply values will mutate in that order.
        // Note: Objects are compared by reference. It's possible an object with the exact same values counts as a change.

        const diffProps = Object.diff(prevProps, this.props);
        const diffState = Object.diff(prevState, this.state);

        // Do not wipe userSelectedId if currentTable is going from `undefined` to defined.
        // Scnario: On page load with open=something and table inferred from query.
        if (diffProps.hasAny("currentTable") && prevProps.currentTable) {
            this.setState({ userSelectedId: undefined });
        }

        // Cross-references currentTable with allBasics, configuration, and localStorage to determine
        // currentTableSettings (columns, sortColumn, sortOrder).
        if (diffProps.hasAny("allBasics", "currentTable")) {
            this.getTableSettings();
        }

        // Technically depends on "currentTable" however listening to "currentTableSettings" is
        // sufficient as the former is currently guaranteed to trigger the latter.
        if (diffProps.hasAny("debouncedQuery") || diffState.hasAny("currentTableSettings", "page")) {
            this.getListings();
        }

        // Watching currentTable as sometimes inferred from the query.
        // Not watching for query changes (to match old behavior), therefore highlighting is not currently being updated.
        // allBasics + currentTable = idColumn, which is needed for the query.
        if (diffProps.hasAny("allBasics", "currentTable") || diffState.hasAny("userSelectedId")) {
            this.getDetails();
        }

        if (diffProps.hasAny("debouncedQuery", "userSelectedTable", "currentTable") || diffState.hasAny("userTableSettings", "userSelectedId")) {
            var url = this.buildThisUrl(true);
            if (url !== window.location.href) {
                this.props.thisUrlChanged(url);
                history.pushState("", "", url);
            }
        }
    }

    onKeyDown(e) {
        // ESC: Close
        if (e.keyCode === 27) {
            this.setState({ userSelectedId: undefined });
            e.stopPropagation();
        }

        if (e.key === "ArrowDown" || e.key === "ArrowUp") {
            var indexChange = (e.key === "ArrowDown" ? 1 : -1);
            this.refs.list.selectByRelativeIndex(indexChange);
            e.stopPropagation();
            e.preventDefault(); // Prevent viewport scroll.
        }
    }
    onResort(sortColumn, sortOrder) {
        localStorage.mergeJson("table-" + this.props.currentTable, {
            sortColumn: sortColumn,
            sortOrder: sortOrder
        });
        this.props.userSelectedTableChanged(this.props.currentTable);
    }
    onSetColumns(columns, table) {
        table = table || this.props.currentTable;
        localStorage.mergeJson("table-" + table, { columns: columns });
        this.props.userSelectedTableChanged(table);
    }

    getTableSettings() {
        const table = this.props.allBasics[this.props.currentTable];
        if (!table) {
            this.setState({ userTableSettings: undefined });
            return;
        }

        // Must write to userTableSettings (and not directly to currentTableSettings) so the URL can refect this.
        // Sample schema: { columns: ["Name", "IP"], sortColumn: "IP", sortOrder: "desc" }
        var userTableSettings = localStorage.getJson("table-" + this.props.currentTable, {});
        this.setState({
            userTableSettings: userTableSettings,
            currentTableSettings: Object.merge(
                { columns: [table.idColumn], sortColumn: table.idColumn, sortOrder: "asc" },
                configuration.listingDefaults && configuration.listingDefaults[this.props.currentTable],
                userTableSettings)
        });
    }
    getListings() {
        if (!this.props.query ||
            !this.props.currentTable ||
            !Object.keys(this.state.currentTableSettings).length) {
            this.setState({ listingData: undefined, hasMoreData: undefined })
            return;
        };
        var rowCount = 50 * (this.state.page + 1);

        var parameters = Object.merge(
            {
                action: "select",
                q: this.props.query,
                ob: this.state.currentTableSettings.sortColumn,
                so: this.state.currentTableSettings.sortOrder,
                s: 0
            },
            arrayToObject(this.state.currentTableSettings.columns, `c`)
        );
        const queryUrl = `${configuration.url}/table/${this.props.currentTable}${buildUrlParameters(parameters)}`;

        this.props.queryUrlChanged(queryUrl);
        this.jsonQueryWithError(
            queryUrl + "&h=%CF%80&t=" + rowCount,
            data => this.setState({ listingData: data.content, hasMoreData: data.content.total > rowCount })
        );
    }
    getDetails() {
        // When an item is selected, get details for it

        // If there's no table don't do anything yet.
        // Unlikely to reach this function before currentTable and allBasics are set.
        // Delayed getCounts() would have to return before the other two.
        const table = this.props.allBasics[this.props.currentTable];
        if (!table || !this.state.userSelectedId) {
            this.setState({ selectedItemData: null });
            return;
        };

        var detailsQuery = table.idColumn + '="' + this.state.userSelectedId + '"';
        if (this.props.query) detailsQuery += " AND (" + this.props.query + ")"; // Query is included for term highlighting.

        // Select all columns for the selected item, with highlighting
        this.jsonQueryWithError(
            configuration.url + "/table/" + this.props.currentTable,
            data => {
                if (data.content.values) {
                    const dictionary = arribaRowToObject(data.content.values, 0);
                    table.columnsLookup = table.columnsLookup || table.columns.toObject(c => c.name);
                    for (const key in dictionary) {
                        if (dictionary[key] && table.columnsLookup[key].type === "Html") { // Make case insensitive in future.
                            dictionary[key] = DOMPurify.sanitize(dictionary[key]);
                        }
                    }
                    this.setState({ selectedItemData: dictionary });
                } else {
                    if (!this.props.query) {
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
    }

    jsonQueryWithError(url, onSuccess, parameters) {
        jsonQuery(
            url,
            data => {
                this.setState({ error: undefined });
                onSuccess(data);
            },
            (xhr, status, err) => {
                this.setState({ error: `Error: Server didn't respond to [${xhr.url}]. ${err}`, loading: false });
            },
            parameters
        );
    }
    buildThisUrl(includeOpen) {
        var userTableSettings = this.state.userTableSettings || {};
        var parameters = Object.merge(
            {
                t: Object.keys(userTableSettings).length ? this.props.currentTable : this.props.userSelectedTable,
                q: this.props.query || undefined,
                ob: userTableSettings.sortColumn,
                so: userTableSettings.sortOrder,
                open: includeOpen && this.state.userSelectedId || undefined,
            },
            arrayToObject(userTableSettings.columns, `c`)
        );
        return `${location.protocol}//${location.host + "/" + buildUrlParameters(parameters)}`;
    }
    render() {
        const table = this.props.allBasics && this.props.currentTable && this.props.allBasics[this.props.currentTable] || undefined;
        const CustomDetailsView = (configuration.customDetailsProviders && configuration.customDetailsProviders[this.props.currentTable]) || ResultDetails;

        return <div className="center" onDragEnter={e => {
                // Consider disabling pointer events for perf.
                if (!this.state.dropping) this.setState({ dropping: true, file: undefined })
            }}>
            <QueryStats error={this.state.error} selectedData={this.state.listingData} />
            {this.props.query && table
                ? <SplitPane split="horizontal" minSize="300" isFirstVisible={this.state.listingData} isSecondVisible={this.state.userSelectedId}>
                    <InfiniteScroll hasMoreData={this.state.hasMoreData} loadMore={() => this.setState({ page: this.state.page + 1 })}>
                        <ResultListing ref={"list"}
                            data={this.state.listingData}
                            allBasics={this.props.allBasics}
                            sortColumn={this.state.currentTableSettings.sortColumn}
                            sortOrder={this.state.currentTableSettings.sortOrder}
                            selectedId={this.state.userSelectedId}
                            onResort={this.onResort.bind(this)}
                            onSelectionChanged={id => this.setState({ userSelectedId: id })}
                            onSetColumns={this.onSetColumns.bind(this)} />
                    </InfiniteScroll>
                    <div className="scrollable">
                        <CustomDetailsView
                            itemId={this.state.userSelectedId}
                            table={this.props.currentTable}
                            query={this.props.query}
                            data={this.state.selectedItemData}
                            onClose={() => this.setState({ userSelectedId: undefined })}
                            onAddClause={(name, value) => this.props.queryChanged(`${this.props.query} AND [${name}]="${value}"`)} />
                    </div>
                </SplitPane>
                : <Start allBasics={this.props.allBasics} showHelp={this.props.params.help === "true"} queryChanged={this.props.queryChanged} />}
            <DropShield
                dropping={this.state.dropping}
                droppingChanged={d => this.setState({ dropping: d })}
                existingTablenames={Object.keys(this.props.allBasics || {})}
                queryChanged={this.props.queryChanged}
                refreshAllBasics={this.props.refreshAllBasics}
                getCounts={this.props.getCounts}
                columnsChanged={this.onSetColumns.bind(this)} />
        </div>
    }
}
