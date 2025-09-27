#nullable disable


namespace NonverbalBehaviorGenerator
{
    // one item in saliency map
    internal sealed class SaliencyItem
    {
        public string objectName;
        public float recency;
        public float primacy;
        public float priority;

        public SaliencyItem(string objName, float rece, float prim)
        {
            objectName = objName;
            recency = rece;
            primacy = prim;
        }
    }
}
