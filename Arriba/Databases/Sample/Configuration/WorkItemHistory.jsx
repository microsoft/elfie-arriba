import "./WorkItemHistory.scss";

// Render Work Item history from a JSON object
export default React.createClass({
    render: function () {
        if(!this.props.data || this.props.data.length === 0) return null;
        
        var history = JSON.parse(this.props.data);
        var collection = [];
        for(var i = 0; i < history.length; ++i) {
            var entry = history[i];

            // Only include entries with a comment
            if(!entry.comment) continue;

            // Compute the relative time of the comment
            var dateTime = moment(entry.when);

            collection.push(
                <div className="history">
                    <div><span className="who">{entry.who}</span> <span className="when">{dateTime.fromNow()}</span></div>
                    <div className="comment" dangerouslySetInnerHTML={highlight(entry.comment)}></div>
                </div>
            );
        }

        // Don't return the box if there's no history
        if(collection.length === 0) return null;

        return (
            <div class="box">
                {collection}
            </div>
        );        
    }
});