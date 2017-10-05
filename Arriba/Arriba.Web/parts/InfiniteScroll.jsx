// InfiniteScroll provides infinite scrolling support.
/* Usage:
    <InfiniteScroll hasMoreData loadMore>
        {content}
    </InfiniteScroll>

    Page identifies the integer page of items loaded so far, starting at zero.
    When scrolling is required, loadMore will be called for page+1.
    Page updating on the InfiniteScroll class tells it that it can call loadMore again (the previous loadMore is complete).
    hasMoreData is set to false when there are no more items to load and loadMore should no longer be called.
*/
export default React.createClass({
    handleScroll: function (e) {
        var element = e.target;
        if (e.target === this.refs.scrollContainer) {
            // Need to re-find the cells each event just in case the columns changed.
            // Selecting td/th instead of thead for IE/Edge compat.
            var cells = [].slice.apply(this.refs.scrollContainer.querySelectorAll("thead > tr > *"));

            // Position relative works equally well.
            cells.forEach(cell => cell.style.transform = "translate(0," + element.scrollTop + "px)");

            var pixelsFromBottom = (element.scrollHeight - element.clientHeight - element.scrollTop);

            if (pixelsFromBottom < 200) {
                // Remove this listener to avoid multiple loadMore calls
                // "page" changing in the caller will cause this to re-render and be re-attached
                e.target.removeEventListener("onScroll", this.handleScroll);

                // Load the next page of items
                if (this.props.hasMoreData) {
                    this.props.loadMore();
                }
            }
        }
    },
    render: function () {
        return (
            // .offsetParent for ResultListing > AddColumnList.
            <div ref="scrollContainer" className="scrollable infiniteScroll offsetParent" onScroll={this.handleScroll}>
                {this.props.children}
            </div>
        );
    }
});
