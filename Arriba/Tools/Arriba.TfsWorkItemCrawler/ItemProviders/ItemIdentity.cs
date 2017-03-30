using System;

namespace Arriba.TfsWorkItemCrawler.ItemProviders
{
    public class ItemIdentity
    {
        public int ID { get; set; }
        public DateTime ChangedDate { get; set; }

        public ItemIdentity(int id, DateTime changedDate)
        {
            this.ID = id;
            this.ChangedDate = changedDate;
        }

        public override bool Equals(object o)
        {
            if (!(o is ItemIdentity)) return false;
            ItemIdentity other = (ItemIdentity)o;
            return this.ID.Equals(other.ID) && this.ChangedDate.Equals(other.ChangedDate);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode() ^ this.ChangedDate.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("{0:n0} | {1:s}", this.ID, this.ChangedDate);
        }
    }

    //public class ItemIdentityGeneric
    //{
    //    public object ID { get; set; }
    //    public IComparable ChangedDate { get; set; }
    //}
}
