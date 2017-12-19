import "../Search.scss";
﻿import "./Grid.scss";
import "!script-loader!../js/utilities.js";

import EventedComponent from "./EventedComponent";
import QueryStats from "./QueryStats"

window.configuration = require("../configuration/Configuration.jsx").default;

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
export default class extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = {
            aggregationFunction: this.props.params.af || "COUNT",
            aggregateColumn: this.props.params.ac || "",
            rows: getParameterArrayForPrefix(this.props.params, "r"),
            rowLabels: getParameterArrayForPrefix(this.props.params, "rl"),
            cols: getParameterArrayForPrefix(this.props.params, "c"),
            colLabels: getParameterArrayForPrefix(this.props.params, "cl"),

            show: this.props.params.show || "both",
            showPortionOf: this.props.params.of || "total",
            showPortionAs: this.props.params.as || "bar",
        };
    }
    getClearedUserSelections() {
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
        };
    }
    componentDidMount() {
        super.componentDidMount();
        var defaultQuery = this.props.params.p || configuration.gridDefault;
        if (!this.props.query && defaultQuery) {
            this.selectDefaultQuery(defaultQuery);
        }
        this.componentDidUpdate({}, {});
    }
    componentDidUpdate(prevProps, prevState) {
        const diffProps = Object.diff(prevProps, this.props);
        const diffState = Object.diff(prevState, this.state);

        if (diffProps.hasAny("currentTable") && prevProps.currentTable) {
            this.setState(this.getClearedUserSelections());
        }

        // These require a re-render but not new getGrid: "rowLabels", "colLabels", "show", "showPortionOf", "showPortionAs".
        if (diffProps.hasAny("debouncedQuery", "currentTable") || diffState.hasAny("aggregationFunction", "aggregateColumn", "rows", "cols")) {
            this.getGrid();

            var url = this.buildThisUrl(true);
            if (url !== window.location.href) {
                history.pushState("", "", url);
            }
        }
    }

    selectDefaultQuery(name) {
        const query = Object.assign(this.getClearedUserSelections(), configuration.gridDefaultQueries[name]);
        if (query.query) this.props.queryChanged(query.query);
        if (query.userSelectedTable) this.props.userSelectedTableChanged(query.userSelectedTable);
        delete query.query;
        delete query.userSelectedTable;
        this.setState(query);
    }
    handleQueryChange(type, index, value, label) {
        this.props.userSelectedTableChanged(this.props.currentTable);

        // NOTE: When a column or row is changed, we lock the current table and clear the grid data.
        //  We lock the table because the rows/cols are cleared when the active table is changed and we don't want "top query" changes to lose the cols/rows you've picked
        //  We clear Grid data because after the change but before the query, the data is inconsistent with the cols/rows and we don't want React to draw it like that
        const newState = {};
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

        this.setState(newState);
    }

    getGrid() {
        // Once the counts query runs and table basics are loaded, get a page of results
        if (!this.props.query || !this.props.currentTable) {
            this.setState({ gridData: undefined })
            return;
        }

        // Get a page of matches for the given query for the desired columns and sort order, with highlighting.
        xhr(this.buildQueryUrl()).then(data => {
            var newState = { gridData: {content: data} };

            // If the rows or columns were expanded by the query, use the expanded values so subsequent editing works
            // NOTE: Track the dimension for rows and columns; if only columns were passed, dimensions[0] is the column.
            var dimensions = data.query && data.query.dimensions;
            if (dimensions) {
                var dimensionIndex = 0;

                var fetch = (key) => {
                    var list = this.state[key + "s"];
                    if (list && list.length) {
                        if (list.length === 1 && list[0].endsWith(">")) {
                            var dim = dimensions[dimensionIndex];
                            newState[key + "s"] = dim && dim.groupByWhere || [];
                            newState[key + "Labels"] = [];
                        }
                        dimensionIndex++;
                    }
                }

                fetch("row");
                fetch("col");
            }

            this.setState(newState);
        });
    }

    buildQueryUrl() {
        var parameters = {
            action: "aggregate",
            q: this.props.query,
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
        return "table/" + this.props.currentTable + queryString;
    }
    buildThisUrl(includeOpen) {
        var relevantParams = {};

        if (this.props.query) relevantParams.q = this.props.query;
        if (this.props.currentTable) relevantParams.t = this.props.currentTable;

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

        return location.protocol + '//' + location.host + "/Grid.html" + buildUrlParameters(relevantParams);
    }
    render() {
        var mainContent = null;

        const content = this.state.gridData && this.state.gridData.content;
        const rows = [...this.state.rows.slice(), "All"];
        const rowLabels = this.state.rowLabels;
        const columns = [...this.state.cols.slice(), "All"];
        const colLabels = this.state.colLabels;

        if (content && !content.details.succeeded) {
            mainContent = <div className="body-error">{content.details.errors}</div>
        } else if (content && content.values && content.values.rows.length >= rows.length * columns.length) {
            // If content is stale (cell count less than expected) then skip render.

            const currentTableAllColumns =
                this.props.allBasics && this.props.currentTable &&
                this.props.allBasics[this.props.currentTable] &&
                this.props.allBasics[this.props.currentTable].columns || [];

            mainContent = (
                <div className="grid">
                    <table className={"legacyTable " + this.state.showPortionOf}>
                        <thead>
                            <tr>
                                <GridFunctionCell key="AC"
                                    aggregationFunction={this.state.aggregationFunction}
                                    aggregateColumn={this.state.aggregateColumn}
                                    allColumns={currentTableAllColumns}
                                    onChange={(aggregationFunction, aggregateColumn) => {
                                        this.props.userSelectedTableChanged(this.props.currentTable);
                                        this.setState({ aggregationFunction: aggregationFunction, aggregateColumn: aggregateColumn });
                                    }} />
                                {columns.map((col, i) => <GridHeadingCell
                                    key={"HC" + col}
                                    type="column"
                                    index={i}
                                    value={col}
                                    label={colLabels[i]}
                                    onChange={this.handleQueryChange.bind(this)} />)}
                            </tr>
                        </thead>
                        <tbody>
                            {rows.map((row, rowIndex) => <tr key={"R" + rowIndex }>
                                <GridHeadingCell key={"HR" + rows[rowIndex]} type="row" index={rowIndex} value={rows[rowIndex]} label={rowLabels[rowIndex]} onChange={this.handleQueryChange} />
                                {columns.map((col, colIndex) => <GridValueCell
                                    key={"C" + colIndex + "R" + rowIndex}
                                    colIndex={colIndex}
                                    rowIndex={rowIndex}
                                    content={content}
                                    columnCount={columns.length} rowCount={rows.length}
                                    show={this.state.show} showPortionAs={this.state.showPortionAs} showPortionOf={this.state.showPortionOf} />)}
                            </tr>)}
                        </tbody>
                    </table>
                    <div className="options">
                        Show&nbsp;
                        <select value={this.state.show} onChange={e => this.setState({ show: e.target.value })}>
                            <option value="number">Number</option>
                            <option value="percentage">Percentage</option>
                            <option value="both">Both</option>
                        </select>
                        &nbsp;of&nbsp;
                        <select value={this.state.showPortionOf} onChange={e => this.setState({ showPortionOf: e.target.value })}>
                            <option value="total">Total</option>
                            <option value="row">Row</option>
                            <option value="col">Column</option>
                        </select>
                        &nbsp;as&nbsp;
                        <select value={this.state.showPortionAs} onChange={e => this.setState({ showPortionAs: e.target.value })}>
                            <option value="bar">Bar</option>
                            <option value="pie">Pie</option>
                        </select>
                        &nbsp;&nbsp;&nbsp;&nbsp;
                        Load&nbsp;
                        <select value="" onChange={e => this.selectDefaultQuery(e.target.value)}>
                            <option key={"SQNone"} value=""></option>
                            {Object.keys(configuration.gridDefaultQueries || {}).map(name => <option key={"SQ" + name} value={name}>{name}</option>)}
                        </select>
                    </div>
                </div>
            );
        }

        return <div className="center">
            <QueryStats selectedData={this.state.gridData && this.state.gridData.content} />
            <div className="scrollable">
                {mainContent}
            </div>
        </div>;
    }
}
