
// Class for rendering json data as a table
export default React.createClass({
    render: function () {
        let table = [];
        let header = null;
        const records = JSON.parse(this.props.json);

        records.forEach((record, index) => {
            if (!header) {
                header = (
                    <tr>
                        {
                            Object.keys(record).map(key => (
                                <th>{key}</th>
                            ))
                        }
                    </tr>
                );
            }

            table.push(
                <tr>
                    {
                        Object.keys(record).map(key => (
                            <td>{record[key]}</td>
                        ))
                    }
                </tr>
            );
        });

        return (
            <table className="legacyTable detailsTable">
                <thead>
                    {header}
                </thead>
                <tbody>
                    {table}
                </tbody>
            </table>
        );
    }
});