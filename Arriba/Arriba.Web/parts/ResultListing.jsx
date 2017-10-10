import "./ResultListing.scss";

import EventedComponent from "./EventedComponent";
﻿import AddColumnList from "./AddColumnList";
import Delete from "./Delete"

import createDOMPurify  from "DOMPurify";
const DOMPurify = createDOMPurify(window);

// ResultListing shows a table of items matching a query, with sortable columns
export default class extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = { selectedIndex: -1, addColumnsShowing: false };
        this.events = { "click": e => this.setState({ addColumnsShowing: false }) };
    }
    handleResort(columnNameClicked, e) {
        // If a column heading was clicked, re-sort the table
        var sortOrder = "asc";

        // If this column is already the sort column, toggle ascending/descending
        if (this.props.sortColumn === columnNameClicked) {
            sortOrder = (this.props.sortOrder === "asc" ? "desc" : "asc");
        }

        // Store the sort column and order and raise the "onResort" event
        this.props.onResort(columnNameClicked, sortOrder);

        e.stopPropagation();
    }
    handleSelect(e) {
        var rowElement = e;
        this.setState({ selectedIndex: rowElement.props.itemIndex, addColumnsShowing: false });
        this.props.onSelectionChanged(rowElement.props.itemId);
    }
    onAddColumn(name) {
        if (name) {
            var columns = this.props.data.query.columns;
            columns.push(name);

            this.setState({ addColumnsShowing: false }, this.props.onSetColumns(columns));
        } else {
            this.setState({ addColumnsShowing: false });
        }
    }
    selectByRelativeIndex(i) {
        // Figure out the current row count
        var count = 0;
        if (this.props.data) count = this.props.data.values.rows.length;

        // See what index the caller wants selected
        var newSelectedIndex = this.state.selectedIndex + i;

        // If nothing was selected, let down open the first item
        if (!this.props.selectedId) newSelectedIndex = -1 + i;

        // If out-of-bounds, do nothing
        if (newSelectedIndex < 0 || newSelectedIndex >= count) return;

        // Otherwise, trigger a selection change
        var idColumn = this.props.allBasics && this.props.allBasics[this.props.data.query.tableName].idColumn;
        var idColumnIndex = this.props.data.query.columns.indexOf(idColumn);
        var row = this.props.data.values.rows[newSelectedIndex];
        this.setState({ selectedIndex: newSelectedIndex });
        this.props.onSelectionChanged(stripHighlight(row[idColumnIndex]));
    }
    render() {
        var content = this.props.data;

        if (!this.props.allBasics || !content) return null;

        if (!content.details.succeeded) return <div className="body-error">{content.details.errors}</div>

        const table = this.props.allBasics[this.props.data.query.tableName];
        if (!table) return null;

        var idColumn = table.idColumn;
        var idColumnIndex = content.query.columns.indexOf(idColumn);

        // Write a row for each item
        var index = 0;
        var selectFunction = this.handleSelect.bind(this);
        var selectedId = this.props.selectedId;

        return <table className="resultListing" tabIndex="2">
            <thead>
                <tr>
                    {content.values.columns.map((column, i) => {
                        var sort = this.props.sortColumn === column.name
                            ? this.props.sortOrder === "asc" ? " ↑" : " ↓"
                            : "";

                        // Extra element div.th-inner because display:flex cannot be applied to td as td is already display:table.
                        return <td key={"heading_" + column.name} onClick={this.handleResort.bind(this, column.name)}>
                            <div className="th-inner">
                                <span className="th-title">{column.name}{sort}</span>
                                {column.name !== idColumn && <a
                                    href={"Grid.html" + buildUrlParameters({ q: content.query.where, t: content.query.tableName, R1: column.name + ">" })}
                                    className="icon-button columnCommand"
                                    title={"Grid By " + column.name }>
                                    <img src="/icons/grid-mini.svg"/>
                                </a>}
                                {column.name !== idColumn && <Delete key={"remove_" + column.name}
                                    title="Remove Column"
                                    className="columnCommand"
                                    onClick={e => {
                                        this.props.onSetColumns(this.props.data.query.columns.filter(name => name !== column.name));
                                        e.stopPropagation();
                                    }} />}
                                {i == content.values.columns.length - 1 && <img
                                    src="/icons/add.svg"
                                    className="icon-button add-column-button"
                                    title="Add Column"
                                    onClick={e => {
                                        e.stopPropagation();
                                        this.setState({ addColumnsShowing: !this.state.addColumnsShowing })}
                                    }/>}
                            </div>
                        </td>;
                    })}
                </tr>
            </thead>
            <tbody>
                {content.values.rows.map(function (row) {
                    var id = stripHighlight(row[idColumnIndex]);
                    return <ResultListingItem
                        key={id}
                        itemId={id}
                        itemIndex={index++}
                        data={row}
                        columns={content.values.columns}
                        onSelectionChanged={selectFunction}
                        selected={selectedId === id } />;
                })}
            </tbody>
            {this.state.addColumnsShowing && <AddColumnList
                onAddColumn={this.onAddColumn.bind(this)}
                allColumns={table.columns}
                currentColumns={content.query.columns}
                onClick={e => e.stopPropagation() /* Prevent clicks originating inside from dismissing the list */} />}
        </table>;
    }
}

class ResultListingItem extends React.Component {
    render() {
        return <tr onClick={() => this.props.onSelectionChanged(this)}
            className={(this.props.selected ? "selected" : "")}>
            {this.props.data.map((item, i) => {
                if (item.length > 200) { // Cap cell contents length for browser layout perf.
                    item = item.substr(0, 200) + "…";
                }
                const type = this.props.columns[i].type;
                if (type === "Html") { // Make case insensitive.
                    item = DOMPurify.sanitize(item);
                }
                return <td key={`${this.props.itemId}_${i}`} title={stripHighlight(item)} dangerouslySetInnerHTML={highlight(item)} />
            })}
        </tr>;
    }
}
