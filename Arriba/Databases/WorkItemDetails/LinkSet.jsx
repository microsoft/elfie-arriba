import "./LinkSet.scss";

// Render a collection of links indented under a heading. Nothing is rendered if the collection is empty
export default React.createClass({
    render: function () {
        if(!this.props.data || this.props.data.length === 0) return null;
        
        var set = JSON.parse(this.props.data);
        var collection = [];
        for(var i = 0; i < set.length; ++i) {
            var link = set[i];

            if(link.uri) {
                // Attachment
                collection.push(<a target="_blank" href={link.uri} dangerouslySetInnerHTML={highlight(link.name)}></a>);
            } else {
                // Related Links
                var encodedId =  encodeURIComponent(link.id);
                collection.push(<a href={"?t=" + this.props.table + "&q=ID%3D" + encodedId + "&open=" + encodedId} dangerouslySetInnerHTML={highlight(link.id)}></a>);
            }
        }

        return (
            <div className="linkset">
                <div className="title">{this.props.label}</div>
                <div className="indent">
                    {collection}
                </div>
            </div>
        );        
    }
});