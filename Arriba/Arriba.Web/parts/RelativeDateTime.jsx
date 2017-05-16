// RelativeDateTime writes a DateTime in terms of now (ex: 3 days ago)
export default React.createClass({
    render: function () {
        var label = null;
        if (this.props.label) label = <span>{this.props.label + " "}</span>;

        if (!this.props.value || this.props.value.toString().indexOf("0001-01-01") === 0) {
            return <div>{label} Unknown</div>
        }

        var dateTime = moment(this.props.value);
        var howLongSince = moment.duration(moment().diff(dateTime));

        var content = <span>{dateTime.fromNow()}</span>;
        if(this.props.bold) content = <b>{content}</b>;

        return <div title={this.props.columnName + ':' + this.props.value}>{label}{content}</div>;
    }
});