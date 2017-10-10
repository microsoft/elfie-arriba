import "./WorkItemHistory.scss";

import createDOMPurify  from "DOMPurify";
const DOMPurify = createDOMPurify(window);

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

            var linkToItem = document.location.protocol + "//" + document.location.host + document.location.pathname + "?t=" + encodeURIComponent(this.props.table) + "&q=ID%3D" + encodeURIComponent(this.props.itemId) + "&open=" + encodeURIComponent(this.props.itemId);
            var mailTo = "mailto:" + encodeURIComponent(stripHighlight(entry.who)) + "?subject=" + encodeURIComponent(this.props.table + "." + this.props.itemId) + "&body=" + encodeURIComponent(linkToItem);

            collection.push(
                <div className="history">
                    <div><a className="who" href={mailTo} dangerouslySetInnerHTML={highlight(entry.who)} /> <span className="when">{dateTime.fromNow()}</span></div>
                    <div className="comment" dangerouslySetInnerHTML={highlightHtml(DOMPurify.sanitize(entry.comment))}></div>
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
