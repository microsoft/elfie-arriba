import "./AddColumnList.scss";
import EventedComponent from "./EventedComponent";

// AddColumnList is the list of unselected columns which shows up in the listing when the '+' is clicked.
export default class extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = { filter: null, filteredColumns: this.filterColumns(null), selectedIndex: 0 };
    }
    filterColumns(filter) {
        if (!filter) filter = "";
        filter = filter.toLowerCase();

        var filteredColumns = [];
        for (var i = 0; i < this.props.allColumns.length; ++i) {
            var column = this.props.allColumns[i];

            // Filter columns already added and those not starting with the filter
            if (this.props.currentColumns.indexOf(column.name) !== -1) continue;
            if (filter && column.name.toLowerCase().indexOf(filter) !== 0) continue;

            filteredColumns.push(column.name);
        }

        return filteredColumns;
    }
    handleKeyDown(e) {
        if (e.keyCode === 27) {
            // ESC - Close AddColumnList
            this.setState({ filter: null, filteredColumns: this.filterColumns(null), selectedIndex: 0 });
            this.props.onAddColumn(null);
            e.stopPropagation();
        } else if (e.keyCode === 13 || e.keyCode === 9) {
            // ENTER/TAB - commit highlighted match
            var currentColumn = this.state.filteredColumns[this.state.selectedIndex];
            if(currentColumn) this.props.onAddColumn(currentColumn);
            e.stopPropagation();
        } else if (e.keyCode === 38) {
            // Up Arrow - select previous
            this.setState({ selectedIndex: (this.state.selectedIndex <= 0 ? 0 : this.state.selectedIndex - 1) });
            e.stopPropagation();
        } else if (e.keyCode === 40) {
            // Down Arrow - select next
            this.setState({ selectedIndex: (this.state.selectedIndex >= this.state.filteredColumns.length ? this.state.filteredColumns.length - 1 : this.state.selectedIndex + 1) });
            e.stopPropagation();
        }
    }
    handleFilterChanged(e) {
        var newFilter = e.target.value;
        var newFilteredColumns = this.filterColumns(newFilter);
        this.setState({ filter: newFilter, filteredColumns: newFilteredColumns, selectedIndex: 0 });
    }
    render() {
        // Write an add column list (shown only once the '+' is clicked)
        if (!this.props.showing) return null;

        return <div className="add-list" onKeyDown={this.handleKeyDown.bind(this)} >
            <input type="text" autoFocus placeholder="Filter Columns" value={this.state.filter} onChange={this.handleFilterChanged.bind(this)} onKeyDown={this.handleKeyDown.bind(this)} />
            <div className="addColumnsList">
                {this.state.filteredColumns.map((name, i) => <div
                    key={"add_" + name}
                    onClick={e => {
                        this.props.onAddColumn(name);
                        e.stopPropagation();
                    }}
                    className={(i === this.state.selectedIndex) && "add-list-selected"}>
                    {name}
                </div>)}
            </div>
        </div>;
    }
}
