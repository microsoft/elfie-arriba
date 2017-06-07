import "./SearchBox.scss";
import EventedComponent from "./EventedComponent";
import Suggestions from "./Suggestions";

export default class SearchBox extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = {};      
        this.events = {
            "storage": e => {
                if (!["favorites"].includes(e.key)) return;
                setTimeout(() => this.forceUpdate()); // Just to update the star.
            },
            "click": e => { // Ideally use input.blur, but that fires before suggestions.item.onClick.
                if (e.target === this.refs.input) return;
                this.refs.suggestions.clear();
            },
        };
    }
    componentDidMount() {
        super.componentDidMount();
        this.refs.input.focus();
    }
    onInput(query) {
        // IE focus/blur spurriously triggers onInput(), this works around that.
        if (this.props.query !== query) this.props.queryChanged(query);
    }
    toggleFavorite() {
        if (!this.props.parsedQuery) return;
        localStorage.updateJson("favorites", favs => (favs || []).toggle(this.props.parsedQuery));
    }
    render() {
        var star = (localStorage.getJson("favorites") || []).includes(this.props.parsedQuery) ? "icon-solid-star" : "icon-outlined-star"
        return <div className="searchBox">
            <div className={ "loading " + (this.props.loading ? "loading-active" : "") }></div>
            <input ref="input" type="text"
                spellCheck="false"
                placeholder="Search for..." 
                tabIndex="1"
                value={this.props.query}
                onClick={e => this.refs.suggestions.fetch()}
                onInput={e => this.onInput(e.target.value)}
                onKeyDown={e => this.refs.suggestions.onKeyDown(e)} />
            <div className="rail">
                {this.state.completed}
                <span style={{ position: "relative" }} >
                    <div className="railContents">
                        <Suggestions
                            ref="suggestions"
                            query={this.props.query}
                            queryChanged={q => this.props.queryChanged(q)}
                            completedChanged={c => this.setState({ completed: c })}
                            selectedChanged={s => this.setState({ selected: s && s.category === "ColumnName" && s || undefined }) }
                            refocus={() => this.refs.input.focus()} />

                        <Suggestions
                            ref="peek"
                            marginTop={this.state.selected && this.state.selected.offsetTop || 0}
                            cache={true}
                            query={this.state.selected && this.state.completed + this.state.selected.completeAs + ' = ' || undefined}
                            queryChanged={q => this.props.queryChanged(q)}
                            sel={-1}
                            completedChanged={c => {}}
                            refocus={() => this.refs.input.focus()} />
                    </div>
                </span>
            </div>
            <i className={"searchIcon clickable " + star} onClick={e => this.toggleFavorite(e)}></i>
            <i className="searchIcon icon-find"></i>
        </div>
    }
}
