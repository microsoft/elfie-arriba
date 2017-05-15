// Render a collection of links indented under a heading. Nothing is rendered if the collection is empty
export default React.createClass({
    render: function () {
        if(!this.props.data || this.props.data.length === 0) return null;
        
        var collection = [];
        for(var i = 0; i < this.props.data.length; ++i) {
            var link = this.props.data[i];

            if(link.uri) {
                // Attachment
                collection.push(<a target="_blank" href={link.uri}>{link.name}</a>);
            } else {
                // Related Links
                collection.push(<a href={"?q=ID=" + link.id + "&open=" + link.id}>{link.id}</a>);
            }
        }

        return (
            <div>
                <b>{this.props.label}</b>
                <div class="indent">
                    {collection}
                </div>
                <br />
            </div>
        );        
    }
});