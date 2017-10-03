import "./Start.scss";

export default class extends React.Component {
    constructor(props) {
        super(props);
    }
    componentDidMount() {
        window.addEventListener("storage", this);
    }
    componentWillUnmount() {
        window.removeEventListener("storage", this);
    }
    handleEvent (e) {
        // Assumed to be type="storage" as we only subscribed for that.
        if (["hideIntro", "favorites", "recents"].includes(e.key)) setTimeout(() => this.forceUpdate());
    }
    del (e, type, fav) {
        e.stopPropagation();
        localStorage.updateJson(type, favs => favs.remove(fav));
    }
    render() {
        var itemGroup = (title, key, items, icon) => <div className="item-group" style={{ display: items.length ? '' : 'none' }}>
            <div className="h1">{title}</div>
            {items.map(item =>
                <div className="item" onClick={() => this.props.queryChanged(item)}>
                    <span>{icon}</span>
                    <span>{item}</span>
                    <span onClick={e => this.del(e, key, item)}>Delete</span>
                </div>
            )}
        </div>;

        var tables = Object.map(this.props.allBasics || {}, (name, data) => data);
        var itemCount = tables.reduce((sum, table) => sum + table.rowCount, 0);
        var lastUpdateDate = Math.min(...tables.map(table => new Date(table.lastWriteTimeUtc))); // Will be a Date() if found, 0 if empty array.
        var lastUpdated = lastUpdateDate ? moment(lastUpdateDate).fromNow().replace(/minutes?/, "min").replace("a few seconds ago", "just now") : "...";

        var hideIntro = localStorage.getJson("hideIntro");

        var favs = localStorage.getJson("favorites") || [];
        var recents = localStorage.getJson("recents") || [];
        var [r, rf, f] = recents.venn(favs);

        return <div className="start-wrapper">
            <div className="start" ref="start">
                {itemCount
                    ? <h1>Instantly search <b>{itemCount.toLocaleString()}</b> items as of <b>{lastUpdated}</b></h1>
                    : <h1>Loading...</h1>}
                {configuration.startContent && [
                    <div className="intro" style={{ display: hideIntro ? "none" : "" }}>
                        <div className="welcome">{configuration.startContent.overview}</div>
                        {configuration.startContent.examples && Object.map(configuration.startContent.examples, (query, remark) =>
                            <div className="example">
                                <div className="query clickable" onClick={() => this.props.queryChanged(query)}>{query}</div>{remark}
                            </div>
                        )}
                    </div>,
                    <div className="divider">
                        <div onClick={() => localStorage.updateJson("hideIntro", hi => !hi)}
                            className="more">{hideIntro ? "▼ Show" : "▲ Hide"}</div>
                    </div>
                ]}
                <div className="items" style={{ display: favs.length || recents.length ? '' : 'none' }}>
                    {itemGroup("Favorite", "favorites", [...rf, ...f], "☆")}
                    {itemGroup("Recent", "recents", r, "○")}
                </div>
            </div>
        </div>;
    }
}
