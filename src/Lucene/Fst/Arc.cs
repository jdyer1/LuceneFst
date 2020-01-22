namespace Lucene.Fst
{
    public class Arc<T>
    {
        // really an "unsigned" byte
        public int label; 
        public Node target;
        public bool isFinal;
        public T output;
        public T nextFinalOutput;
    }
}