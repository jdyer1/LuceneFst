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
        private IntsRef lastInput = new IntsRef();
        public readonly FST<T> fst;
        public readonly T NO_OUTPUT;
        private UnCompiledNode<T>[] frontier;

        public Builder(INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            this.fst = new FST<T>(inputType, outputs, bytesPageBits);
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
                //TODO: Debug.Assert(validOutput(lastOutput));

                T commonOutputPrefix;
                T wordSuffix;

                if (!lastOutput.Equals(NO_OUTPUT))
                {
                    commonOutputPrefix = fst.outputs.common(output, lastOutput);
                    //TODO: Debug.Assert(validOutput(commonOutputPrefix));
                    wordSuffix = fst.outputs.subtract(lastOutput, commonOutputPrefix);
                    //TODO: Debug.Assert(validOutput(wordSuffix));
                    parentNode.setLastOutput(input.ints[input.offset + idx - 1], commonOutputPrefix);
                    node.prependOutput(wordSuffix);
                }
                else
                {
                    commonOutputPrefix = wordSuffix = NO_OUTPUT;
                }

                output = fst.outputs.subtract(output, commonOutputPrefix);
                //TODO: Debug.Assert(validOutput(output));
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

        private void freezeTail(int prefixLenPlus1)
        {
            //TODO
        }

    }
