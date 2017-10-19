import "./AddColumnList.scss";
import EventedComponent from "./EventedComponent";

// AddColumnList is the list of unselected columns which shows up in the listing when the '+' is clicked.
export default class extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = { filteredColumns: [], selectedIndex: 0 };
    }
    componentDidMount() {
        super.componentDidMount();
        this.componentDidUpdate({}, {});
    }
    componentDidUpdate(prevProps, prevState) {
        const diffProps = Object.diff(prevProps, this.props);
        const diffState = Object.diff(prevState, this.state);

        if (diffProps.hasAny("allColumns", "currentColumns") || diffState.hasAny("filter")) {
            const filter = (this.state.filter || "").toLowerCase();
            const filteredColumns = this.props.allColumns
                .map(col => col.name)
                .filter(name => (!filter || name.toLowerCase().includes(filter)) && !this.props.currentColumns.includes(name));
            this.setState({ filteredColumns, selectedIndex: 0 });
        }
    }
    handleKeyDown(e) {
        if (e.key === "Escape") {
            this.setState({ filter: undefined });
            this.props.onAddColumn(null);
            e.stopPropagation();
        }
        if (e.key === "Enter" || e.key === "Tab") {
            var currentColumn = this.state.filteredColumns[this.state.selectedIndex];
            if(currentColumn) this.props.onAddColumn(currentColumn);
            e.stopPropagation();
        }
        if (e.key === "ArrowUp") {
            this.setState({ selectedIndex: (this.state.selectedIndex <= 0 ? 0 : this.state.selectedIndex - 1) });
            e.stopPropagation();
        }
        if (e.key === "ArrowDown") {
            this.setState({ selectedIndex: (this.state.selectedIndex >= this.state.filteredColumns.length ? this.state.filteredColumns.length - 1 : this.state.selectedIndex + 1) });
            e.stopPropagation();
        }
    }
    render() {
        return <div className="add-list" onClick={this.props.onClick} onKeyDown={this.handleKeyDown.bind(this)} >
            <input type="text" autoFocus placeholder="Filter Columns" value={this.state.filter} onChange={e => this.setState({ filter: e.target.value })} onKeyDown={this.handleKeyDown.bind(this)} />
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
