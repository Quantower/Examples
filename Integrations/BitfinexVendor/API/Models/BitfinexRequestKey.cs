namespace BitfinexVendor.API
{
    struct BitfinexRequestKey
    {
        public BitfinexChannelType Channel { get; set; }

        public string Pair { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is BitfinexRequestKey key))
                return false;

            if (this.Channel != key.Channel)
                return false;

            if (this.Pair != key.Pair)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;

                hash = (hash * 16777619) ^ this.Channel.GetHashCode();
                hash = (hash * 16777619) ^ (this.Pair ?? string.Empty).GetHashCode();

                return hash;
            }
        }

        public override string ToString() => $"{this.Pair} : {this.Channel}";
    }
}
