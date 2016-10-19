namespace Arriba.Model
{
    public class AddOrUpdateOptions
    {
        public bool AddMissingRows { get; set; }
        public bool AddMissingColumns { get; set; }

        public AddOrUpdateOptions()
        {
            this.AddMissingRows = true;
            this.AddMissingColumns = false;
        }
    }
}
