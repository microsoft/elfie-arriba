// InfiniteScroll provides infinite scrolling support.
/* Usage:
    <InfiniteScroll page={this.state.page} hasMoreData={this.state.hasMoreData} loadMore={this.getResultsPage}>
        {content}
    </InfiniteScroll>

    Page identifies the integer page of items loaded so far, starting at zero.
    When scrolling is required, loadMore will be called for page+1.
    Page updating on the InfiniteScroll class tells it that it can call loadMore again (the previous loadMore is complete).
    hasMoreData is set to false when there are no more items to load and loadMore should no longer be called.
*/
var InfiniteScroll = React.createClass({
    handleScroll: function (e) {
        var element = e.target;
        if (e.target === this.refs.scrollContainer) {
            var pixelsFromBottom = (element.scrollHeight - element.clientHeight - element.scrollTop);

            if (pixelsFromBottom < 200) {
                // Remove this listener to avoid multiple loadMore calls
                // "page" changing in the caller will cause this to re-render and be re-attached
                e.target.removeEventListener("onScroll", this.handleScroll);

                // Load the next page of items
                if (this.props.hasMoreData) {
                    this.props.loadMore(this.props.page + 1);
                }
            }
        }
    },
    render: function () {
        return (
            <div ref="scrollContainer" className="scrollable" onScroll={this.handleScroll}>
                {this.props.children}
            </div>
        );
    }
});
