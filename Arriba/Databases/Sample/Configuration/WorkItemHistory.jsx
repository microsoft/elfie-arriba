// Render Work Item history from a JSON object
export default React.createClass({
    render: function () {
        if(!this.props.data || this.props.data.length === 0) return null;
        
        var collection = [];
        for(var i = 0; i < this.props.data.length; ++i) {
            var entry = this.props.data[i];

            var dateTime = moment(entry.when);
            var howLongSince = moment.duration(moment().diff(dateTime));

            collection.push(
                <div>
                    <div><b>{entry.who}</b> {howLongSince}</div>
                    <div dangerouslySetInnerHTML={highlight(entry.comment)}></div>
                </div>
            );
        }

        return (
            <div class="box">
                {collection}
            </div>
        );        
    }
});