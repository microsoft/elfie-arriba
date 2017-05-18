import DetailsField from "../../parts/DetailsField";

// Render a mail to link with a URL back to the item
export default React.createClass({
    render: function () {
        var linkToItem = document.location.protocol + "//" + document.location.host + document.location.pathname + "?t=" + encodeURIComponent(this.props.table) + "&q=ID%3D" + encodeURIComponent(this.props.itemId) + "&open=" + encodeURIComponent(this.props.itemId);
        var mailTo = "mailto:" + encodeURIComponent(stripHighlight(this.props.value)) + "?subject=" + encodeURIComponent(this.props.table + "." + this.props.itemId) + "&body=" + encodeURIComponent(linkToItem);
        return <DetailsField value={this.props.value} columnName={this.props.columnName} label={this.props.columnName} bold="true" url={mailTo} />;   
    }
});