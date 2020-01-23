using System.Diagnostics;
using System;
using Lucene.Core;

namespace Lucene.Fst
{
    public class Builder<T>
    {
        private readonly int minSuffixCount1 = 0;
        private readonly int minSuffixCount2 = 0;
        private readonly bool doShareSuffix = false;
        private readonly bool doShareNonSingletonNodes = false;
        private readonly int shareMaxTailLength = Int32.MaxValue;
        public readonly bool allowFixedLengthArcs = true;
        private IntsRef lastInput = new IntsRef();
        public readonly FST<T> fst;
        public readonly BytesStore bytes;
        public readonly T NO_OUTPUT;
        private UnCompiledNode<T>[] frontier;

        // Used for the BIT_TARGET_NEXT optimization (whereby
        // instead of storing the address of the target node for
        // a given arc, we mark a single bit noting that the next
        // node in the byte[] is the target node):
        public long lastFrozenNode;

        public int[] numBytesPerArc = new int[4];
        public int[] numLabelBytesPerArc = new int[4];
        public long arcCount;
        public long nodeCount;
        public long binarySearchNodeCount;

        public Builder(INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            this.fst = new FST<T>(inputType, outputs, bytesPageBits);
            this.bytes = this.fst.bytes;
            this.NO_OUTPUT = outputs.getNoOutput();
            this.frontier = new UnCompiledNode<T>[10];
            for (int i = 0; i < this.frontier.Length; i++)
            {
                this.frontier[i] = new UnCompiledNode<T>(this, i);
            }
        }

        public Builder<T> add(IntsRef input, T output)
        {
            Debug.Assert(lastInput.length == 0 || input.CompareTo(lastInput) >= 0, "inputs are added out of order lastInput=" + lastInput + " vs input=" + input);
            if (input.length == 0)
            {
                // empty input: only allowed as first input.  we have
                // to special case this because the packed FST
                // format cannot represent the empty input since
                // 'finalness' is stored on the incoming arc, not on
                // the node
                frontier[0].inputCount++;
                frontier[0].isFinal = true;
                fst.setEmptyOutput(output);
                return this;
            }

            // compare shared prefix length
            int pos1 = 0;
            int pos2 = input.offset;
            int pos1Stop = Math.Min(lastInput.length, input.length);
            while (true)
            {
                frontier[pos1].inputCount++;
                if (pos1 >= pos1Stop || lastInput.ints[pos1] != input.ints[pos2])
                {
                    break;
                }
                pos1++;
                pos2++;
            }
            int prefixLenPlus1 = pos1 + 1;
            if (frontier.Length < input.length + 1)
            {
                UnCompiledNode<T>[] next = ArrayUtil.grow(frontier, input.length + 1);
                for (int idx = frontier.Length; idx < next.Length; idx++)
                {
                    next[idx] = new UnCompiledNode<T>(this, idx);
                }
                frontier = next;
            }

            // minimize/compile states from previous input's
            // orphan'd suffix
            freezeTail(prefixLenPlus1);

            // init tail states for current input
            for (int idx = prefixLenPlus1; idx <= input.length; idx++)
            {
                frontier[idx - 1].addArc(input.ints[input.offset + idx - 1],
                                       frontier[idx]);
                frontier[idx].inputCount++;
            }
            UnCompiledNode<T> lastNode = frontier[input.length];
            if (lastInput.length != input.length || prefixLenPlus1 != input.length + 1)
            {
                lastNode.isFinal = true;
                lastNode.output = NO_OUTPUT;
            }

            // push conflicting outputs forward, only as far as
            // needed
            for (int idx = 1; idx < prefixLenPlus1; idx++)
            {
                UnCompiledNode<T> node = frontier[idx];
                UnCompiledNode<T> parentNode = frontier[idx - 1];

                T lastOutput = parentNode.getLastOutput(input.ints[input.offset + idx - 1]);

                T commonOutputPrefix;
                T wordSuffix;

                if (!lastOutput.Equals(NO_OUTPUT))
                {
                    commonOutputPrefix = fst.outputs.common(output, lastOutput);
                    wordSuffix = fst.outputs.subtract(lastOutput, commonOutputPrefix);
                    parentNode.setLastOutput(input.ints[input.offset + idx - 1], commonOutputPrefix);
                    node.prependOutput(wordSuffix);
                }
                else
                {
                    commonOutputPrefix = wordSuffix = NO_OUTPUT;
                }

                output = fst.outputs.subtract(output, commonOutputPrefix);
            }
            if (lastInput.length == input.length && prefixLenPlus1 == 1 + input.length)
            {
                // same input more than 1 time in a row, mapping to
                // multiple outputs
                lastNode.output = fst.outputs.merge(lastNode.output, output);
            }
            else
            {
                // this new arc is private to this new input; set its
                // arc output to the leftover output:
                frontier[prefixLenPlus1 - 1].setLastOutput(input.ints[input.offset + prefixLenPlus1 - 1], output);
            }

            // save last input
            lastInput = (IntsRef)input.Clone();

            return this;
        }

        private CompiledNode compileNode(UnCompiledNode<T> nodeIn, int tailLength)
        {
            long node;
            long bytesPosStart = bytes.getPosition();
            //TODO: deduphash
            node = fst.addNode(this, nodeIn);
            Debug.Assert(node != -2);

            long bytesPosEnd = bytes.getPosition();
            if (bytesPosEnd != bytesPosStart)
            {
                // The FST added a new node:
                Debug.Assert(bytesPosEnd > bytesPosStart);
                lastFrozenNode = node;
            }

            nodeIn.clear();

            CompiledNode fn = new CompiledNode();
            fn.node = node;
            return fn;
        }
        private void freezeTail(int prefixLenPlus1)
        {
            int downTo = Math.Max(1, prefixLenPlus1);
            for (int idx = lastInput.length; idx >= downTo; idx--)
            {

                bool doPrune = false;
                bool doCompile = false;

                UnCompiledNode<T> node = frontier[idx];
                UnCompiledNode<T> parent = frontier[idx - 1];

                if (node.inputCount < minSuffixCount1)
                {
                    doPrune = true;
                    doCompile = true;
                }
                else if (idx > prefixLenPlus1)
                {
                    // prune if parent's inputCount is less than suffixMinCount2
                    if (parent.inputCount < minSuffixCount2 || (minSuffixCount2 == 1 && parent.inputCount == 1 && idx > 1))
                    {
                        // my parent, about to be compiled, doesn't make the cut, so
                        // I'm definitely pruned 

                        // if minSuffixCount2 is 1, we keep only up
                        // until the 'distinguished edge', ie we keep only the
                        // 'divergent' part of the FST. if my parent, about to be
                        // compiled, has inputCount 1 then we are already past the
                        // distinguished edge.  NOTE: this only works if
                        // the FST outputs are not "compressible" (simple
                        // ords ARE compressible).
                        doPrune = true;
                    }
                    else
                    {
                        // my parent, about to be compiled, does make the cut, so
                        // I'm definitely not pruned 
                        doPrune = false;
                    }
                    doCompile = true;
                }
                else
                {
                    // if pruning is disabled (count is 0) we can always
                    // compile current node
                    doCompile = minSuffixCount2 == 0;
                }

                if (node.inputCount < minSuffixCount2 || (minSuffixCount2 == 1 && node.inputCount == 1 && idx > 1))
                {
                    // drop all arcs
                    for (int arcIdx = 0; arcIdx < node.numArcs; arcIdx++)
                    {
                        UnCompiledNode<T> target = (UnCompiledNode<T>)node.arcs[arcIdx].target;
                        target.clear();
                    }
                    node.numArcs = 0;
                }

                if (doPrune)
                {
                    // this node doesn't make it -- deref it
                    node.clear();
                    parent.deleteLast(lastInput.ints[idx - 1], node);
                }
                else
                {

                    if (minSuffixCount2 != 0)
                    {
                        //TODO: minSuffixCount2 is always 0 for now.
                        //compileAllTargets(node, lastInput.length()-idx);
                    }
                    T nextFinalOutput = node.output;

                    // We "fake" the node as being final if it has no
                    // outgoing arcs; in theory we could leave it
                    // as non-final (the FST can represent this), but
                    // FSTEnum, Util, etc., have trouble w/ non-final
                    // dead-end states:
                    bool isFinal = node.isFinal || node.numArcs == 0;

                    if (doCompile)
                    {
                        // this node makes it and we now compile it.  first,
                        // compile any targets that were previously
                        // undecided:
                        parent.replaceLast(lastInput.ints[idx - 1],
                                           compileNode(node, 1 + lastInput.length - idx),
                                           nextFinalOutput,
                                           isFinal);
                    }
                    else
                    {
                        // replaceLast just to install
                        // nextFinalOutput/isFinal onto the arc
                        parent.replaceLast(lastInput.ints[idx - 1],
                                           node,
                                           nextFinalOutput,
                                           isFinal);
                        // this node will stay in play for now, since we are
                        // undecided on whether to prune it.  later, it
                        // will be either compiled or pruned, so we must
                        // allocate a new node:
                        frontier[idx] = new UnCompiledNode<T>(this, idx);
                    }
                }
            }
        }

    }
}
