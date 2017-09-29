import AddColumnList from "./AddColumnList";
import "./ResultListing.scss";
import Delete from "./Delete"

import createDOMPurify  from "DOMPurify";
const DOMPurify = createDOMPurify(window);

// ResultListing shows a table of items matching a query, with sortable columns
export default React.createClass({
    getInitialState: function () {
        return { selectedIndex: -1, addColumnsShowing: false };
    },
    handleResort: function (columnNameClicked, e) {
        // If a column heading was clicked, re-sort the table
        var sortOrder = "asc";

        // If this column is already the sort column, toggle ascending/descending
        if (this.props.sortColumn === columnNameClicked) {
            sortOrder = (this.props.sortOrder === "asc" ? "desc" : "asc");
        }

        // Store the sort column and order and raise the "onResort" event
        this.props.onResort(columnNameClicked, sortOrder);

        e.stopPropagation();
    },
    handleSelect: function (e) {
        var rowElement = e;
        this.setState({ selectedIndex: rowElement.props.itemIndex, addColumnsShowing: false });
        this.props.onSelectionChanged(rowElement.props.itemId);
    },
    handleAdd: function (e) {
        if (e.target === this.refs.addButton) {
            this.setState({ addColumnsShowing: !this.state.addColumnsShowing });
        }
        e.stopPropagation();
    },
    onAddColumn: function (name) {
        if (name) {
            var columns = this.props.data.query.columns;
            columns.push(name);

            this.setState({ addColumnsShowing: false }, this.props.onSetColumns(columns));
        } else {
            this.setState({ addColumnsShowing: false });
        }
    },
    selectByRelativeIndex: function (i) {
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
    },
    render: function () {
        var content = this.props.data;

        if (!this.props.allBasics || !content) return null;

        if (!content.details.succeeded) return <div className="body-error">{content.details.errors}</div>

        const table = this.props.allBasics[this.props.data.query.tableName];
        if (!table) return null;

        var idColumn = table.idColumn;
        var idColumnIndex = content.query.columns.indexOf(idColumn);

        // Write a row for each item
        var index = 0;
        var selectFunction = this.handleSelect;
        var selectedId = this.props.selectedId;

        return <table className="resultTable" tabIndex="2">
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
                                    className="icon-button"
                                    title={"Grid By " + column.name }>
                                    <img src="/icons/grid.svg"/>
                                </a>}
                                {column.name !== idColumn && <Delete key={"remove_" + column.name}
                                    title="Remove Column"
                                    onClick={e => {
                                        this.props.onSetColumns(this.props.data.query.columns.filter(name => name !== column.name));
                                        e.stopPropagation();
                                    }} />}
                                {i == content.values.columns.length - 1 && <img
                                    src="/icons/add.svg"
                                    className="icon-button add-column-button"
                                    ref={"addButton"}
                                    title="Add Column"
                                    onClick={this.handleAdd}/>}
                            </div>
                        </td>;
                    })}
                </tr>
            </thead>
            <tbody>
                {content.values.rows.map(function (row) {
                    var id = stripHighlight(row[idColumnIndex]);
                    return <ResultListingItem key={id} itemId={id} itemIndex={index++} data={row} columns={content.values.columns} onSelectionChanged={selectFunction} selected={selectedId === id } />;
                })}
            </tbody>
            <AddColumnList
                showing={this.state.addColumnsShowing}
                onAddColumn={this.onAddColumn}
                allColumns={table.columns}
                currentColumns={content.query.columns} />
        </table>;
    }
});

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
