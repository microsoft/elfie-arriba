// Writes out a field with label and highlighting, but only if the value is set
export default React.createClass({
    render: function () {
        if (!this.props.value || this.props.value === "N/A" || this.props.value === "Unknown") return null;
        
        var content = <span dangerouslySetInnerHTML={highlight(this.props.value)} title={this.props.columnName + ':' + stripHighlight(this.props.value)} />;
        if (this.props.bold) content = <b>{content}</b>;

        if(this.props.url) {
            var fullUrl = this.props.url.replace(/{ID}/g, encodeURIComponent(this.props.id)).replace(/{Value}/g, encodeURIComponent(this.props.value));
            var target = (fullUrl.indexOf("mailto:") === 0 ? "_self" : "_blank");
            content = <a target={target} title={this.props.title} href={fullUrl}>{content}</a>;
        }

        var label = this.props.label && <span>{this.props.label + " "}</span>

        if (this.props.inline === "true") {
            return <span>{label} {content}</span>
        } else {
            return <div>{label} {content}</div>
        }
    }
});
