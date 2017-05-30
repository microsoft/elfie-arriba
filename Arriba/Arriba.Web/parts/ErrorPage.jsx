// An error page to show in place of all content when access is denied or the site is down.
export default React.createClass({
    render: function () {
        return (
            <div className="errorPage">
                <h1>
                    <span className="errorTitle">Access Denied</span>
                    <span className="errorStatus">{this.props.status == 0 ? "" : this.props.status}</span>
                </h1>
                <article>
                    <p>{configuration.accessDeniedContent}</p>
                </article>
            </div>
        );
    }
});
