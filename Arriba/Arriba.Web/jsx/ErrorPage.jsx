// An error page to show in place of all content when access is denied or the site is down.
var ErrorPage = React.createClass({
    render: function () {
        return (
            <div className="errorPage">
                <h1>{this.props.title}</h1>
                <article>
                    <p>
                        {this.props.message}
                    </p>
                </article>
            </div>
        );
    }
});
