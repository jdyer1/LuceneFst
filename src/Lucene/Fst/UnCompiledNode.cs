using System;
using System.Diagnostics;
using Lucene.Core;

namespace Lucene.Fst
{
    public class UnCompiledNode<T> : Node
    {
        private readonly Builder<T> owner;
        public int numArcs;
        public Arc<T>[] arcs;
        public T output;
        public bool isFinal;
        public long inputCount;
        public readonly int depth;

        public UnCompiledNode(Builder<T> owner, int depth)
        {
            this.owner = owner;
            arcs = new Arc<T>[1];
            arcs[0] = new Arc<T>();
            output = owner.NO_OUTPUT;
            this.depth = depth;
        }
        public bool isCompiled() => false;

        public void clear()
        {
            numArcs = 0;
            isFinal = false;
            output = owner.NO_OUTPUT;
            inputCount = 0;

            // We don't clear the depth here because it never changes 
            // for nodes on the frontier (even when reused).
        }
        public T getLastOutput(int labelToMatch)
        {
            Debug.Assert(numArcs > 0);
            Debug.Assert(arcs[numArcs - 1].label == labelToMatch);
            return arcs[numArcs - 1].output;
        }

        public void addArc(int label, Node target)
        {
            Debug.Assert(label >= 0);
#if DEBUG
            if (numArcs > 0)
            {
                Debug.Assert(label > arcs[numArcs - 1].label,
                "arc[numArcs-1].label=" + arcs[numArcs - 1].label + " new label=" + label + " numArcs=" + numArcs);
            }
#endif

            if (numArcs == arcs.Length)
            {
                Arc<T>[] newArcs = ArrayUtil.grow(arcs, numArcs + 1);
                for (int arcIdx = numArcs; arcIdx < newArcs.Length; arcIdx++)
                {
                    newArcs[arcIdx] = new Arc<T>();
                }
                arcs = newArcs;
            }
            Arc<T> arc = arcs[numArcs++];
            arc.label = label;
            arc.target = target;
            arc.output = arc.nextFinalOutput = owner.NO_OUTPUT;
            arc.isFinal = false;
        }

        public void replaceLast(int labelToMatch, Node target, T nextFinalOutput, bool isFinal)
        {
            Debug.Assert(numArcs > 0);
            Arc<T> arc = arcs[numArcs - 1];
            Debug.Assert(arc.label == labelToMatch, "arc.label=" + arc.label + " vs " + labelToMatch);
            arc.target = target;
            arc.nextFinalOutput = nextFinalOutput;
            arc.isFinal = isFinal;
        }
        public void deleteLast(int label, Node target)
        {
            Debug.Assert(numArcs > 0);
            Debug.Assert(label == arcs[numArcs - 1].label);
            Debug.Assert(target == arcs[numArcs - 1].target);
            numArcs--;
        }
        public void setLastOutput(int labelToMatch, T newOutput)
        {
            Debug.Assert(numArcs > 0);
            Arc<T> arc = arcs[numArcs - 1];
            Debug.Assert(arc.label == labelToMatch);
            arc.output = newOutput;
        }

        // pushes an output prefix forward onto all arcs
        public void prependOutput(T outputPrefix)
        {
            // TODO: Debug.Assert(owner.validOutput(outputPrefix));
            for (int arcIdx = 0; arcIdx < numArcs; arcIdx++)
            {
                arcs[arcIdx].output = owner.fst.outputs.add(outputPrefix, arcs[arcIdx].output);
            }

            if (isFinal)
            {
                T output1 = owner.fst.outputs.add(outputPrefix, output);
                this.output = output1;
            }
        }

    }
}