using System;
using System.Diagnostics;
using Lucene.Core;

namespace Lucene.Fst
{
    public class FST<T>
    {
        private static readonly int BIT_FINAL_ARC = 1 << 0;
        private static readonly int BIT_LAST_ARC = 1 << 1;
        private static readonly int BIT_TARGET_NEXT = 1 << 2;
        private static readonly int BIT_STOP_NODE = 1 << 3;

        /** This flag is set if the arc has an output. */
        public static readonly int BIT_ARC_HAS_OUTPUT = 1 << 4;

        private static readonly int BIT_ARC_HAS_FINAL_OUTPUT = 1 << 5;

        private static readonly int FIXED_LENGTH_ARC_SHALLOW_DEPTH = 3;

        private static readonly int FIXED_LENGTH_ARC_SHALLOW_NUM_ARCS = 5;

        private static readonly int FIXED_LENGTH_ARC_DEEP_NUM_ARCS = 10;
        private static readonly long FINAL_END_NODE = -1;

        private static readonly long NON_FINAL_END_NODE = 0;

        /** If arc has this label then that arc is final/accepted */
        public static readonly int END_LABEL = -1;
        private readonly INPUT_TYPE inputType;
        public readonly Outputs<T> outputs;
        private readonly int bytesPageBits;
        public readonly BytesStore bytes;
        public T emptyOutput;
        private long startNode = -1;
        public FST(INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            this.inputType = inputType;
            this.outputs = outputs;
            this.bytesPageBits = bytesPageBits;
            this.bytes = new BytesStore(bytesPageBits);
            // pad: ensure no node gets address 0 which is reserved to mean
            // the stop state w/ no arcs
            bytes.writeByte((byte)0);
        }

        public void setEmptyOutput(T v)
        {
            if (emptyOutput != null)
            {
                emptyOutput = outputs.merge(emptyOutput, v);
            }
            else
            {
                emptyOutput = v;
            }
        }


        // serializes new node by appending its bytes to the end
        // of the current byte[]
        public long addNode(Builder<T> builder, UnCompiledNode<T> nodeIn)
        {
            T NO_OUTPUT = outputs.getNoOutput();

            if (nodeIn.numArcs == 0)
            {
                if (nodeIn.isFinal)
                {
                    return FINAL_END_NODE;
                }
                else
                {
                    return NON_FINAL_END_NODE;
                }
            }
            long startAddress = builder.bytes.getPosition();

            bool doFixedLengthArcs = shouldExpandNodeWithFixedLengthArcs(builder, nodeIn);
            if (doFixedLengthArcs)
            {
                if (builder.numBytesPerArc.Length < nodeIn.numArcs)
                {
                    builder.numBytesPerArc = new int[ArrayUtil.oversize(nodeIn.numArcs, 4)];
                    builder.numLabelBytesPerArc = new int[builder.numBytesPerArc.Length];
                }
            }

            builder.arcCount += nodeIn.numArcs;
            int lastArc = nodeIn.numArcs - 1;
            long lastArcStart = builder.bytes.getPosition();
            int maxBytesPerArc = 0;
            int maxBytesPerArcWithoutLabel = 0;
            for (int arcIdx = 0; arcIdx < nodeIn.numArcs; arcIdx++)
            {
                Arc<T> arc = nodeIn.arcs[arcIdx];
                CompiledNode target = (CompiledNode)arc.target;
                int flags = 0;

                if (arcIdx == lastArc)
                {
                    flags += BIT_LAST_ARC;
                }

                if (builder.lastFrozenNode == target.node && !doFixedLengthArcs)
                {
                    // TODO: for better perf (but more RAM used) we
                    // could avoid this except when arc is "near" the
                    // last arc:
                    flags += BIT_TARGET_NEXT;
                }

                if (arc.isFinal)
                {
                    flags += BIT_FINAL_ARC;
                    if (!NO_OUTPUT.Equals(arc.nextFinalOutput))
                    {
                        flags += BIT_ARC_HAS_FINAL_OUTPUT;
                    }
                }
                else
                {
                    Debug.Assert(NO_OUTPUT.Equals(arc.nextFinalOutput));
                }

                bool targetHasArcs = target.node > 0;

                if (!targetHasArcs)
                {
                    flags += BIT_STOP_NODE;
                }

                if (!NO_OUTPUT.Equals(arc.output))
                {
                    flags += BIT_ARC_HAS_OUTPUT;
                }

                builder.bytes.writeByte((byte)flags);
                long labelStart = builder.bytes.getPosition();
                writeLabel(builder.bytes, arc.label);
                int numLabelBytes = (int)(builder.bytes.getPosition() - labelStart);

                if (!NO_OUTPUT.Equals(arc.output))
                {
                    throw new NotImplementedException();
                    //TODO: outputs.write(arc.output, builder.bytes);
                }

                if (!NO_OUTPUT.Equals(arc.nextFinalOutput))
                {
                    throw new NotImplementedException();
                    //TODO: outputs.writeFinalOutput(arc.nextFinalOutput, builder.bytes);
                }

                if (targetHasArcs && (flags & BIT_TARGET_NEXT) == 0)
                {
                    Debug.Assert(target.node > 0);
                    builder.bytes.writeVLong(target.node);
                }

                // just write the arcs "like normal" on first pass, but record how many bytes each one took
                // and max byte size:
                if (doFixedLengthArcs)
                {
                    int numArcBytes = (int)(builder.bytes.getPosition() - lastArcStart);
                    builder.numBytesPerArc[arcIdx] = numArcBytes;
                    builder.numLabelBytesPerArc[arcIdx] = numLabelBytes;
                    lastArcStart = builder.bytes.getPosition();
                    maxBytesPerArc = Math.Max(maxBytesPerArc, numArcBytes);
                    maxBytesPerArcWithoutLabel = Math.Max(maxBytesPerArcWithoutLabel, numArcBytes - numLabelBytes);
                }
            }

            if (doFixedLengthArcs)
            {
                Debug.Assert(maxBytesPerArc > 0);

                // 2nd pass just "expands" all arcs to take up a fixed byte size
                int labelRange = nodeIn.arcs[nodeIn.numArcs - 1].label - nodeIn.arcs[0].label + 1;
                Debug.Assert(labelRange > 0);
                if (shouldExpandNodeWithDirectAddressing(builder, nodeIn, maxBytesPerArc, maxBytesPerArcWithoutLabel, labelRange))
                {
                    //writeNodeForDirectAddressing(builder, nodeIn, startAddress, maxBytesPerArcWithoutLabel, labelRange);
                    //builder.directAddressingNodeCount++;
                    throw new NotImplementedException();
                }
                else
                {
                    writeNodeForBinarySearch(builder, nodeIn, startAddress, maxBytesPerArc);
                    builder.binarySearchNodeCount++;
                }
            }

            long thisNodeAddress = builder.bytes.getPosition() - 1;
            builder.bytes.reverse(startAddress, thisNodeAddress);
            builder.nodeCount++;
            return thisNodeAddress;
        }
        private bool shouldExpandNodeWithDirectAddressing(Builder<T> builder, UnCompiledNode<T> nodeIn,
                                                             int numBytesPerArc, int maxBytesPerArcWithoutLabel, int labelRange)
        {
            //TODO
            return false;
        }
        private void writeNodeForBinarySearch(Builder<T> builder, UnCompiledNode<T> nodeIn, long startAddress, int maxBytesPerArc)
        {
            //TODO
            throw new NotImplementedException();
        }

        private bool shouldExpandNodeWithFixedLengthArcs(Builder<T> builder, UnCompiledNode<T> node)
        {
            return builder.allowFixedLengthArcs &&
                ((node.depth <= FIXED_LENGTH_ARC_SHALLOW_DEPTH && node.numArcs >= FIXED_LENGTH_ARC_SHALLOW_NUM_ARCS) ||
                    node.numArcs >= FIXED_LENGTH_ARC_DEEP_NUM_ARCS);
        }

        private void writeLabel(BytesStore bOut, int v)
        {
            Debug.Assert(v >= 0, "v=" + v);
            if (inputType == INPUT_TYPE.BYTE1)
            {
                Debug.Assert(v <= 255, "v=" + v);
                bOut.writeByte((byte)v);
            }
            else if (inputType == INPUT_TYPE.BYTE2)
            {
                Debug.Assert(v <= 65535, "v=" + v);
                bOut.writeShort((short)v);
            }
            else
            {
                bOut.writeVInt(v);
            }
        }

        public void finish(long newStartNode)
        {
            Debug.Assert(newStartNode <= bytes.getPosition());
            if (startNode != -1)
            {
                throw new InvalidOperationException("already finished");
            }
            if (newStartNode == FINAL_END_NODE && emptyOutput != null)
            {
                newStartNode = 0;
            }
            startNode = newStartNode;
            bytes.finish();
        }
    }
    public enum INPUT_TYPE { BYTE1, BYTE2, BYTE4 }
}