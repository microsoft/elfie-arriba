// Writes out a field with label and highlighting, but only if the value is set
export default React.createClass({
    render: function () {
        if (!this.props.value || this.props.value === "N/A" || this.props.value === "Unknown") return null;

        var link = null;
        if(this.props.url) link = <DetailReportLink url={this.props.url} id={this.props.id} title={this.props["url-title"]} />;

        var content = <span dangerouslySetInnerHTML={highlight(this.props.value)} title={this.props.columnName + ':' + stripHighlight(this.props.value)} />;
        if (this.props.bold) content = <b>{content}</b>;

        var label = null;
        if (this.props.label) label = <span>{this.props.label + ": "}</span>;

        if (this.props.inline === "true") {
            return <span>{label} {content}{link}</span>
        } else {
            return <div>{label} {content}{link}</div>
        }
    }
});