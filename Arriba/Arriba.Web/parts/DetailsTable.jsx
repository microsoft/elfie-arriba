
// Write a simple table with name and value for each property in a data set
export default React.createClass({
    onAddClause: function (e) {
        var name = e.target.getAttribute("data");
        this.props.onAddClause(name, stripHighlight(this.props.data[name]));
    },
    render: function () {
        var details = [];
        var emptyColumns = [];

        for (var name in this.props.data) {
            var commands = [];
            var rawValue = stripHighlight(this.props.data[name]);

            // Skip empty columns
            if(!rawValue || rawValue == 0 || rawValue.indexOf("0001-01-01T") === 0) {
                emptyColumns.push(name);
                continue;
            }

            // Skip excluded columns
            if(this.props.excludeColumns && this.props.excludeColumns.includes(name)) continue;

            // Add 'Pivot to Grid' URL
            var commandCell = null;
            if(this.props.showCommands !== "false") {
                var gridUrl = "Grid.html" + buildUrlParameters({ q: this.props.query, t: this.props.table, R1: name + ">" });
                commands.push(<a href={gridUrl} className="icon-view-all-albums icon-column-heading" title={"Grid By " + name} />);

                var showAddClause = (this.props.onAddClause);
                if (showAddClause && rawValue.length < 100) commands.push(<span className="icon-zoom icon-column-heading" onClick={this.onAddClause} title="Add Clause to Query" data={name} />);

                if (commands) {
                    var width =  (22 * commands.length) + "px";
                    commandCell = <td style={{ minWidth: width, width: width }}>{commands}</td>;
                }
            }

            details.push(
                <tr key={name}>
                    {commandCell}
                    <td>{name}</td>
                    <td><div className="detailsValue" dangerouslySetInnerHTML={highlight(this.props.data[name])} title={name + ':' + rawValue} /></td>
                </tr>
            );
        }

        return (
            <table className="legacyTable detailsTable">
                <tbody>
                    {details}
                </tbody>
            </table>
        );
    }
});
