require("../Search.css");

import ErrorPage from "./ErrorPage"
import QueryStats from "./QueryStats"
import SearchHeader from "./SearchHeader"

// NOTE: Depends on configuration from zConfiguration.jsx.
import defaultConfiguration from "./DefaultConfiguration";
window.configuration = defaultConfiguration;
var optionalContext = require.context("..", true, /\.\/configuration\/Configuration\.jsx/);
if (optionalContext.keys().includes("./configuration/Configuration.jsx")) {
    window.configuration = optionalContext("./configuration/Configuration.jsx").default
}

// The GridFunctionCell is the corner cell which shows the aggregation function and column [COUNT(*)].
var GridFunctionCell = React.createClass({
    getInitialState: function () {
        return {
            isEditing: false,
            aggregationFunction: this.props.aggregationFunction,
            aggregateColumn: this.props.aggregateColumn
        }
    },
    handleEdit: function() {
        this.setState({ isEditing: true });
    },
    handleChange: function() {
        this.setState({ aggregationFunction: this.refs.aggregationFunction.value, aggregateColumn: this.refs.aggregateColumn.value });
    },
    handleCancel: function() {
        this.setState(this.getInitialState());
    },
    handleAccept: function () {
        this.setState({ isEditing: false });
        this.props.onChange(this.refs.aggregationFunction.value, this.refs.aggregateColumn.value);
    },
    isNumericColumn: function (t) {
        t = t.toLowerCase();
        return t.indexOf("int") === 0 || t.indexOf("uint") === 0 || t === "short" || t === "byte" || t === "float" || t === "double";
    },
    render: function () {
        if (this.state.isEditing) { 
            var columns = [];

            if (this.state.aggregationFunction === "COUNT") {
                columns.push(<option key="C*" value=""></option>);
            } else {
                for (var i = 0; i < this.props.allColumns.length; ++i) {
                    if (!this.isNumericColumn(this.props.allColumns[i].type)) continue;
                    var columnName = this.props.allColumns[i].name;
                    columns.push(<option key={columnName} value={columnName }>{columnName}</option>);
                }
            }

            return (
                <td className="grid-heading">
                    <span className="icon-cancel icon-column-heading clickable" title="Cancel" onClick={this.handleCancel} />
                    <span className="icon-accept icon-column-heading clickable" title="Done" onClick={this.handleAccept} />
                    <div className="cell">
                        <select ref="aggregationFunction" value={this.state.aggregationFunction} onChange={this.handleChange}>
                            <option>COUNT</option>
                            <option>SUM</option>
                            <option>MIN</option>
                            <option>MAX</option>
                        </select>
                        &nbsp;of&nbsp;
                        <select ref="aggregateColumn" value={this.state.aggregateColumn} onChange={this.handleChange}>
                            {columns}
                        </select>
                    </div>
                </td>
            );
        }

        return <td className="grid-heading" onClick={this.handleEdit}><div>{this.props.aggregationFunction + "(" + this.props.aggregateColumn + ")"}</div></td>;
    }
});

// GridHeadingCell renders the cells used in the first row and column
var GridHeadingCell = React.createClass({
    getInitialState: function () {
        return {
            isEditing: false,
            label: this.props.label,
            query: this.props.value
        }
    },
    componentDidMount: function() {
        if(this.props.value === "All") document.body.addEventListener("keydown", this.handleKeyDown);
    },
    handleKeyDown: function (e) {
        if (e.altKey) {
            // Alt+Right Arrow -> new column
            if (e.keyCode == 39 && this.props.type === "column") {
                this.handleEdit();
            }

            // Alt+Down Arrow -> new row
            if (e.keyCode == 40 && this.props.type === "row") {
                this.handleEdit();
            }
        }
    },
    handleRemove: function(e) {
        this.props.onChange(this.props.type, this.props.index, "", "");
        e.stopPropagation();
    },
    handleEdit: function (e) {
        var editQuery = (this.props.value === "All" ? "" : this.props.value);
        this.setState({ isEditing: true, query: editQuery });
        e.stopPropagation();
    },
    handleQueryChange: function(e) {
        this.setState({ query: e.target.value });
    },
    handleLabelChange: function (e) {
        this.setState({ label: e.target.value });
    },
    handleCancel: function(e) {
        this.setState({ isEditing: false, query: this.props.value, label: this.props.label });
    },
    handleAccept: function(e) {
        this.props.onChange(this.props.type, this.props.index, this.refs.query.value, this.refs.label.value);
        this.setState({ isEditing: false });
    },
    handleKey: function (e) {
        // ESC: Cancel change
        if (e.keyCode === 27) {
            this.setState({ isEditing: false, query: this.props.value, label: this.props.label });
        }

        // Enter: Change query
        if (e.keyCode === 13) {
            this.props.onChange(this.props.type, this.props.index, this.refs.query.value, this.refs.label.value);
            this.setState({ isEditing: false });
            return false;
        }
    },
    render: function () {
        if (this.state.isEditing) {
            return (
                <td className="grid-heading editing">
                    <span className="icon-cancel icon-column-heading clickable" title="Cancel" onClick={this.handleCancel} />
                    <span className="icon-accept icon-column-heading clickable" title="Done" onClick={this.handleAccept} />
                    <div>
                        <span className="icon-find" />
                        <input 
                                ref="query" 
                                autoFocus 
                                tabIndex="2"
                                type="text" 
                                placeholder="Query"
                                value={this.state.query} 
                                onChange={this.handleQueryChange} 
                                onKeyDown={this.handleKey} />
                    </div>
                    <div>
                        <span className="icon-rename" />
                        <input 
                                ref="label" 
                                tabIndex="3"
                                type="text" 
                                placeholder="Label"
                                value={this.state.label} 
                                onChange={this.handleLabelChange} 
                                onKeyDown={this.handleKey} />                        
                    </div>

                </td>
            );
        }

        var label = (this.state.label || this.state.query);

        var rowOrColumn = (this.props.type === "row" ? "Row" : "Column");
        var control;
        if (this.props.value !== "All") {
            control = <span key={"removeButton"} className="icon-cancel icon-column-heading" title={"Remove " + rowOrColumn} onClick={this.handleRemove} />;
        } else {
            control = <span ref={"addButton"} className="add-column-button icon-add icon-column-heading" title={"Add " + rowOrColumn} onClick={this.handleEdit} />;
        }

        return <td onClick={this.handleEdit} title={this.state.query} className="grid-heading">{control}<div>{label}</div></td>;
    }
});

// GridValueCell renders the cells in the grid, showing their value formatted according to settings.
var GridValueCell = React.createClass({
    renderPie: function (percentage, emptyColor, filledColor) {
        if(!emptyColor) emptyColor= "rgba(181, 231, 247, 0.5)";
        if(!filledColor) filledColor = "rgb(61, 195, 246)";

        // For technique, see: https://www.smashingmagazine.com/2015/07/designing-simple-pie-charts-with-css/
        return (
            <svg className="pie" viewBox="0 0 32 32" style={{ transform: "rotate(-90deg)", background: emptyColor, borderRadius: "50%" }}>
                <circle r="16" cx="16" cy="16" style={{ fill: emptyColor, stroke: filledColor, strokeWidth: "32", strokeDasharray: (100 * percentage).toFixed(0) + " 100" }}></circle>
            </svg>
        );
    },
    render: function () {
        var nullCell = <td key={"R" + this.props.rowIndex + "C" + this.props.colIndex }><div className="cell">-</div></td>;
        if (!this.props.content.values) return nullCell;

        var colors = ["rgba(88, 207, 119, 0.5)", "rgba(96, 117, 194, 0.5)", "rgba(81, 164, 183, 0.5)", "rgba(255, 181, 108, 0.5)", "rgba(255, 125, 108, 0.5)", "rgba(170, 84, 191, 0.5)"];
        var darkColors = ["rgb(13, 186, 58)", "rgb(17, 42, 134)", "rgb(15, 127, 154)", "rgb(248, 132, 17)", "rgb(245, 44, 17)", "rgb(135, 16, 164)"]

        var dataBlockRowIndex = this.props.rowIndex * this.props.columnCount + this.props.colIndex;
        var dataBlockValueColumnIndex = this.props.content.values.columnCount - 1;
        
        // Bail out if we don't find the expected cell values
        if (this.props.content.values.rows.length <= dataBlockRowIndex || this.props.content.values.rows[dataBlockRowIndex].length <= dataBlockValueColumnIndex) {
            return nullCell;
        }

        // Get the numeric value
        var value = this.props.content.values.rows[dataBlockRowIndex][dataBlockValueColumnIndex] || 0;

        // Determine the denominator to compare against
        var denominatorRowIndex;
        if (this.props.showPortionOf === "row") {
            denominatorRowIndex = ((this.props.rowIndex + 1) * this.props.columnCount) - 1;
        } else if (this.props.showPortionOf === "col") {
            denominatorRowIndex = ((this.props.rowCount - 1) * this.props.columnCount) + this.props.colIndex;
        } else {
            denominatorRowIndex = this.props.content.values.rows.length - 1;
        }

        var denominator = this.props.content.values.rows[denominatorRowIndex][dataBlockValueColumnIndex] || 0;
        var percentage = value / denominator;
        if (denominator === 0) percentage = 0;

        // Write the value in the requested form - number with commas or percentage with three significant digits.
        var valueFormatted = (percentage * 100).toFixed(1) + "%";
        if (valueFormatted === "100.0%") valueFormatted = "100%";

        if (this.props.show === "number") {
            valueFormatted = value.toLocaleString();
        } else if (this.props.show === "both") {
            valueFormatted = <span><span>{valueFormatted}</span><span style={{ float: "right" }}>{value.toLocaleString()}</span></span>;
        }

        // Compute the Query URL for this cell
        var cellQuery = this.props.content.query.where;

        for (var columnIndex = 0; columnIndex < dataBlockValueColumnIndex; ++columnIndex) {
            var filter = this.props.content.values.rows[dataBlockRowIndex][columnIndex];
            if (filter) {
                if (cellQuery) cellQuery += " AND ";
                cellQuery += filter;
            }
        }

        if (this.props.content.query.aggregator === "MIN" || this.props.content.query.aggregator === "MAX") {
            if (cellQuery) cellQuery += " AND ";
            cellQuery += this.props.content.query.aggregationColumns[0] + " = " + value.toString();
        }

        var cellUrl = buildUrlParameters({ t: this.props.content.query.tableName, q: cellQuery })

        // Choose a color for the relative value rendering
        var colorIndex = 0;
        if (this.props.showPortionOf === "row") {
            colorIndex = this.props.rowIndex % colors.length;
        } else if (this.props.showPortionOf === "col") {
            colorIndex = this.props.colIndex % colors.length;
        }

        // Write the element or style needed to show the relative value
        var portionElement = null;
        if (this.props.showPortionAs === "pie") {
            portionElement = this.renderPie(percentage, colors[colorIndex], darkColors[colorIndex]);
        }

        var portionStyle = null;
        if (this.props.showPortionAs === "bar") {
            // Target Width: 150px + (2 x 8x padding) = 166px.
            portionStyle = { boxShadow: colors[colorIndex] + " " + 166 * percentage + "px 0px 0px inset" };
            
            // Fills 100% of the cell width, but doesn't animate [in Chrome, 12/2016]. The animation is a higher priority until we can find a way to get both.
            //portionStyle = { background: "linear-gradient(to right, " + colors[colorIndex] + " 0, " + colors[colorIndex] + " " + (100 * percentage).toFixed(0) + "%, rgba(0, 0, 0, 0) " + (100 * percentage).toFixed(0) + "%)" };
        }

        return (
            <td key={"R" + this.props.rowIndex + "C" + this.props.colIndex} className="c">
                <div style={portionStyle}>
                    {portionElement}
                    <a href={"/" + cellUrl}>{valueFormatted}</a>
                </div>
            </td>
        );
    }
});

// GridMain wraps the overall grid UI
var GridMain = React.createClass({
    getInitialState: function () {
        return {
            blockingErrorTitle: null,
            blockingErrorStatus: null,
            blockingErrorContent: null,

            query: this.props.params.q || "",
            pivotQueries: [],
            currentTable: this.props.params.t,
            currentTableAllColumns: [],

            aggregationFunction: this.props.params.af || "COUNT",
            aggregateColumn: this.props.params.ac || "",
            rows: getParameterArrayForPrefix(this.props.params, "r"),
            rowLabels: getParameterArrayForPrefix(this.props.params, "rl"),
            cols: getParameterArrayForPrefix(this.props.params, "c"),
            colLabels: getParameterArrayForPrefix(this.props.params, "cl"),

            show: this.props.params.show || "both",
            showPortionOf: this.props.params.of || "total",
            showPortionAs: this.props.params.as || "bar",

            gridData: null
        };
    },
    getClearedUserSelections: function () {
        return {
            /* Query is kept to allow "re-exploring" the query. All other state is cleared. */

            aggregationFunction: "COUNT",
            aggregateColumn: "",
            rows: [],
            rowLabels: [],
            cols: [],
            colLabels: [],

            show: "both",
            showPortionOf: "total",
            showPortionAs: "bar",

            userSelectedTable: "",

            gridData: null
        };
    },
    componentDidMount: function () {
        if (this.props.params.p) {
            this.selectDefaultQuery(this.props.params.p);
        } else {
            this.runSearch();
        }
    },
    handleSelectDefaultQuery: function (e) {
        var name = e.target.value;
        this.selectDefaultQuery(name);
    },
    selectDefaultQuery: function(name) {
        var query = this.props.gridDefaultQueries[name];
        if (query) {
            this.setState(this.getClearedUserSelections());
            this.setState(query, this.runSearch);
        }
    },
    handleChangeAggregation: function(aggregationFunction, aggregateColumn) {
        this.setState({ aggregationFunction: aggregationFunction, aggregateColumn: aggregateColumn, userSelectedTable: this.state.currentTable }, this.runSearch);
    },
    handleChangeShow: function(e) {
        this.setState({ show: e.target.value }, this.setHistory);
    },
    handleChangeShowPortionOf: function (e) {
        this.setState({ showPortionOf: e.target.value }, this.setHistory);
    },
    handleChangeShowPortionAs: function (e) {
        this.setState({ showPortionAs: e.target.value }, this.setHistory);
    },
    handleQueryChange: function (type, index, value, label) {
        var newState = { userSelectedTable: this.state.currentTable, gridData: null, addColumn: false, addRow: false };

        // NOTE: When a column or row is changed, we lock the current table and clear the grid data.
        //  We lock the table because the rows/cols are cleared when the active table is changed and we don't want "top query" changes to lose the cols/rows you've picked
        //  We clear Grid data because after the change but before the query, the data is inconsistent with the cols/rows and we don't want React to draw it like that
        if (type === "column") {
            var columns = this.state.cols.slice();
            var colLabels = this.state.colLabels.slice();

            if (value) {
                columns[index] = value;
                colLabels[index] = label;
            } else {
                columns.splice(index, 1);
                colLabels.splice(index, 1);
            }

            newState.cols = columns;
            newState.colLabels = colLabels;
        } else if (type === "row") {
            var rows = this.state.rows.slice();
            var rowLabels = this.state.rowLabels.slice();

            if (value) {
                rows[index] = value;
                rowLabels[index] = label;
            } else {
                rows.splice(index, 1);
                rowLabels.splice(index, 1);
            }

            newState.rows = rows;
            newState.rowLabels = rowLabels;
        }

        this.setState(newState, this.runSearch);
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
    onSearchChange: function (e) {
        this.setState({ query: e.target.value }, this.delayedRunSearch);
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
        var params = { q: this.state.query };
        this.addPivotClauses(params);

        // Get the count of matches from each accessible table
        jsonQuery(
            this.props.url + "/allCount",
            function (data) {
                var tableToShow = this.state.userSelectedTable;
                if (!tableToShow) tableToShow = data.content[0].tableName;

                this.setState({ allCountData: data, currentTable: tableToShow, error: null }, this.getTableBasics.bind(this, this.getGrid));
            }.bind(this),
            function (xhr, status, err) {
                this.setState({ allCountData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });

                if (status === 401) {
                    this.setState({ blockingErrorTitle: "Access Denied", blockingErrorStatus: status, blockingErrorContent: this.props.accessDeniedContent });
                } else {
                    this.setState({ blockingErrorTitle: "Service Unavailable", blockingErrorStatus: status, blockingErrorContent: this.props.serviceUnavailableContent });
                }
                console.error(xhr.url, status, err.toString());
            }.bind(this),
            params
        );
    },
    getTableBasics: function (next) {
        // Once a table is selected, find out the columns
        jsonQuery(this.props.url + "/table/" + this.state.currentTable,
            function (data) {
                this.setState(
                    {
                        currentTableAllColumns: data.content.columns,
                        error: null
                    }, next);
            }.bind(this),
            function (xhr, status, err) {
                this.setState({ gridData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
                console.error(xhr.url, status, err.toString());
            }.bind(this)
        );
    },
    getGrid: function() {
        // Once the counts query runs and table basics are loaded, get a page of results

        // Get a page of matches for the given query for the desired columns and sort order, with highlighting.
        jsonQuery(
            this.buildQueryUrl(),
            function (data) {
                var state = { gridData: data, error: null };

                // If the rows or columns were expanded by the query, use the expanded values so subsequent editing works
                // NOTE: Track the dimension for rows and columns; if only columns were passed, dimensions[0] is the column.
                if (data.content && data.content.query && data.content.query.dimensions) {
                    var dimensions = data.content.query.dimensions;
                    var dimensionIndex = 0;

                    if (this.state.rows && this.state.rows.length === 1) {
                        var lastRow = this.state.rows[0];
                        if (lastRow.indexOf(">") === lastRow.length - 1) {
                            state.rows = (dimensions[dimensionIndex] ? dimensions[dimensionIndex].groupByWhere : []);
                            state.rowLabels = [];
                        }

                        dimensionIndex++;
                    }

                    if (this.state.cols && this.state.cols.length === 1) {
                        var lastColumn = this.state.cols[0];
                        if (lastColumn.indexOf(">") === lastColumn.length - 1) {
                            state.cols = (dimensions[dimensionIndex] ? dimensions[dimensionIndex].groupByWhere : []);
                            state.colLabels = [];
                        }

                        dimensionIndex++;
                    }
                }

                this.setState(state);
            }.bind(this),
            function (xhr, status, err) {
                this.setState({ gridData: [], error: "Error: Server didn't respond to [" + xhr.url + "]. " + err });
                console.error(xhr.url, status, err.toString());
            }.bind(this)
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
            action: "aggregate",
            q: this.state.query,
            a: this.state.aggregationFunction,
            col: this.state.aggregateColumn,
        };

        // Only pass defined dimensions. Pass Rows first, so draw order is correct.
        var dimension = "d";
        if (this.state.rows && this.state.rows.length > 0) {
            addArrayParameters(parameters, dimension, this.state.rows);
            dimension = String.fromCharCode(dimension.charCodeAt(0) + 1);
        }

        if (this.state.cols && this.state.cols.length > 0) {
            addArrayParameters(parameters, dimension, this.state.cols);
            dimension = String.fromCharCode(dimension.charCodeAt(0) + 1);
        }

        var queryString = buildUrlParameters(parameters);
        return this.props.url + "/table/" + this.state.currentTable + queryString;
    },
    buildThisUrl: function (includeOpen) {
        var relevantParams = {};

        if (this.state.query) relevantParams.q = this.state.query;
        if (this.state.currentTable) relevantParams.t = this.state.currentTable;

        if (this.state.aggregationFunction !== "COUNT") {
            relevantParams.af = this.state.aggregationFunction;
            relevantParams.ac = this.state.aggregateColumn;
        }

        if (this.state.show !== "both") relevantParams.show = this.state.show;
        if (this.state.showPortionOf !== "total") relevantParams.of = this.state.showPortionOf;
        if (this.state.showPortionAs !== "bar") relevantParams.as = this.state.showPortionAs;
        
        if (this.state.rows && this.state.rows.length > 0) addArrayParameters(relevantParams, "r", this.state.rows);
        if (this.state.cols && this.state.cols.length > 0) addArrayParameters(relevantParams, "c", this.state.cols);
        if (this.state.rowLabels && this.state.rowLabels.length > 0) addArrayParameters(relevantParams, "rl", this.state.rowLabels);
        if (this.state.colLabels && this.state.colLabels.length > 0) addArrayParameters(relevantParams, "cl", this.state.colLabels);

        return window.location.protocol + '//' + window.location.host + window.location.pathname + buildUrlParameters(relevantParams);

    },
    addPivotClauses: function (set) {
        for (var i = 0; i < this.state.pivotQueries.length; ++i) {
            set["q" + (i + 1)] = this.state.pivotQueries[i].q;
            set["t" + (i + 1)] = this.state.pivotQueries[i].t;
        }
    },
    render: function () {
        if (this.state.blockingErrorTitle) return <ErrorPage title={this.state.blockingErrorTitle} status={this.state.blockingErrorStatus} message={this.state.blockingErrorContent } />;

        var headings = [];
        var gridRows = [];

        var mainContent = null;

        if (this.state.gridData && this.state.gridData.content) {
            var content = this.state.gridData.content;

            var rows = this.state.rows.slice();
            var rowLabels = this.state.rowLabels;
            rows.push("All");

            var columns = this.state.cols.slice();
            var colLabels = this.state.colLabels;
            columns.push("All");

            headings.push(<GridFunctionCell key="AC" aggregationFunction={this.state.aggregationFunction} aggregateColumn={this.state.aggregateColumn} allColumns={this.state.currentTableAllColumns} onChange={this.handleChangeAggregation} />);
            for (var columnIndex = 0; columnIndex < columns.length; ++columnIndex) {
                headings.push(<GridHeadingCell key={"HC" + columns[columnIndex]} type="column" index={columnIndex} value={columns[columnIndex]} label={colLabels[columnIndex]} onChange={this.handleQueryChange } />);
            }

            for (var rowIndex = 0; rowIndex < rows.length; ++rowIndex) {
                var cells = [];
                cells.push(<GridHeadingCell key={"HR" + rows[rowIndex]} type="row" index={rowIndex} value={rows[rowIndex]} label={rowLabels[rowIndex]} onChange={this.handleQueryChange} />);

                for (var colIndex = 0; colIndex < columns.length; ++colIndex) {
                    cells.push(<GridValueCell 
                                    key={"C" + colIndex + "R" + rowIndex} 
                                    colIndex={colIndex} 
                                    rowIndex={rowIndex} 
                                    content={content}
                                    columnCount={columns.length} rowCount={rows.length}
                                    show={this.state.show} showPortionAs={this.state.showPortionAs} showPortionOf={this.state.showPortionOf} />);
                }

                gridRows.push(<tr key={"R" + rowIndex }>{cells}</tr>);
            }

            var defaultQueries = [];
            defaultQueries.push(<option key={"SQNone"} value=""></option>);

            for (var name in this.props.gridDefaultQueries) {
                defaultQueries.push(<option key={"SQ" + name} value={name }>{name}</option>);
            }

            mainContent = (
                <div className="grid">
                    <table className={this.state.showPortionOf}>
                        <thead>
                            <tr>                                
                                {headings}
                            </tr>
                        </thead>
                        <tbody>
                            {gridRows}
                        </tbody>
                    </table>
                    <div className="options">
                        Show&nbsp;
                        <select value={this.state.show} onChange={this.handleChangeShow}>
                            <option value="number">Number</option>
                            <option value="percentage">Percentage</option>
                            <option value="both">Both</option>
                        </select>
                        &nbsp;of&nbsp;
                        <select value={this.state.showPortionOf} onChange={this.handleChangeShowPortionOf}>
                            <option value="total">Total</option>
                            <option value="row">Row</option>
                            <option value="col">Column</option>
                        </select>
                        &nbsp;as&nbsp;
                        <select value={this.state.showPortionAs} onChange={this.handleChangeShowPortionAs}>
                            <option value="bar">Bar</option>
                            <option value="pie">Pie</option>
                        </select>
                        &nbsp;&nbsp;&nbsp;&nbsp;
                        Load&nbsp;
                        <select value="" onChange={this.handleSelectDefaultQuery}>
                            {defaultQueries}
                        </select>
                    </div>
                </div>
            );
        }

        var listingUrl = "/" + buildUrlParameters({ t: this.state.currentTable, q: this.state.query });

        return (
            <div className={"viewport " + configuration.theme} onKeyDown={this.handleKeyDown}>
                <SearchHeader name={configuration.toolName}
                              feedbackEmailAddresses={configuration.feedbackEmailAddresses}
                              query={this.state.query}
                              allColumns={this.state.currentTableAllColumns}
                              onSearchChange={this.onSearchChange} />

                <div className="middle">
                    <div className="mode theme-background-dark">
                        <a href={listingUrl}><i className="icon-details" title="Listing"></i></a>
                        <a className="selected"><i className="icon-view-all-albums" title="Grid"></i></a>
                    </div>

                    <div className="center">
                        <QueryStats error={this.state.error}
                                    allCountData={this.state.allCountData}
                                    selectedData={this.state.gridData}
                                    currentTable={this.state.currentTable}
                                    onSelectedTableChange={this.onSelectedTableChange} />

                        <div className="scrollable">
                            {mainContent}
                        </div>
                    </div>
                </div>
            </div>
        );
    }
});

if (document.getElementById("gridContainer")) {
    var params = getQueryStringParameters();
    ReactDOM.render(
        <GridMain 
            url={configuration.url} 
            gridDefaultQueries={configuration.gridDefaultQueries} 
            accessDeniedContent={configuration.accessDeniedContent}
            serviceUnavailableContent={configuration.serviceUnavailableContent}
            params={params}  />,
        document.getElementById("gridContainer")
    );

    document.title = configuration.toolName;
}
