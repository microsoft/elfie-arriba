// Writes out a field with label and highlighting, but only if the value is set
export default React.createClass({
    urlReplace: function (urlFormat, marker, id) {
        var idForUrl = encodeURIComponent(id);
        return urlFormat.replace(new RegExp(marker), idForUrl);
    },
    render: function () {
        if (!this.props.value || this.props.value === "N/A" || this.props.value === "Unknown") return null;
        
        var content = <span dangerouslySetInnerHTML={highlight(this.props.value)} title={this.props.columnName + ':' + stripHighlight(this.props.value)} />;
        if (this.props.bold) content = <b>{content}</b>;

        var link = null;
        if(this.props.url) content = <a target="_blank" href={this.urlReplace(this.urlReplace(this.props.url, "{ID}", this.props.id), "{Value}", this.props.value)} title={this.props.title}>{content}</a>;

        var label = null;
        if (this.props.label) label = <span>{this.props.label + " "}</span>;

        if (this.props.inline === "true") {
            return <span>{label} {content}{link}</span>
        } else {
            return <div>{label} {content}{link}</div>
        }
    }
});
