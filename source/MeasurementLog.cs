using System.Diagnostics;
using System.IO;
using System.Text;
using SysCol = System.Collections.Generic;

namespace ChaosUtil.Debug
{
    /// <summary>
    ///     Provides methods to create log files about specific actions' runtime.
    ///     The log files consist of a single XML node for every recorded action.
    ///     Actions may be nested within each other, resulting in nested nodes.
    ///     The resulting document is not a valid XML document as it lacks a root node.
    ///     If one were to add a root node it would be valid XML though.
    /// </summary>
    public static class MeasurementLog
    {
        /// <summary> Describes an attribute added to a measurement node for adding additional information about the action. </summary>
        public struct CustomAttribute
        {
            /// <summary>
            ///     The name of the attribute.
            ///     Must be a valid XML attribute name.
            /// </summary>
            public readonly string name;

            /// <summary>
            ///     The value of the attribute.
            ///     Must be a valid XML attribute value.
            /// </summary>
            public readonly string value;

            /// <summary> Create an instance of <see cref="CustomAttribute"/>. </summary>
            /// <param name="name">
            ///     The name of the attribute.
            ///     Must be a valid XML attribute name.
            /// </param>
            /// <param name="value">
            ///     The value of the attribute.
            ///     Must be a valid XML attribute value.
            /// </param>
            public CustomAttribute(string name, string value)
            {
                this.name = name;
                this.value = value;
            }
        }

        /// <summary> The directory to save measurement log files to. Ends with a backslash. </summary>
        public static readonly string LOG_DIRECTORY = $"{Constants.LOG_DIRECTORY}Measurements\\";

        const int SLEEP = 1000;
        const long MAX_FILE_SIZE = 10000000;
        const uint MAX_LOG_DEPTH = 666;

        static int fileCounter;
        static readonly SysCol.Stack<Stopwatch> freeClocks = new SysCol.Stack<Stopwatch>();
        static readonly SysCol.Stack<Stopwatch> busyClocks = new SysCol.Stack<Stopwatch>();

        static StreamWriter wr;

        static readonly StringBuilder currentNode = new StringBuilder();
        static readonly SysCol.Queue<string> finishedNodes = new SysCol.Queue<string>();

        static readonly SysCol.Stack<int> durationPos = new SysCol.Stack<int>();
        static readonly SysCol.Stack<int> startTagEnds = new SysCol.Stack<int>();

        static volatile bool alive = true;

        static int depth => busyClocks.Count;

        static System.Threading.Thread worker;
        static System.Threading.ManualResetEvent cancelWorker;

        static volatile bool initialized = false;
        static volatile object initLock = new object();
        static void Init()
        {
            lock (initLock)
                if (!initialized)
                {
                    initialized = true;

                    Directory.CreateDirectory(LOG_DIRECTORY);
                    foreach (string file in Directory.EnumerateFiles(LOG_DIRECTORY))
                        File.Delete(file);

                    wr = new StreamWriter(File.Create($"{LOG_DIRECTORY}Measure {fileCounter++}.xml"));
                    cancelWorker = new System.Threading.ManualResetEvent(false);
                    worker = new System.Threading.Thread(FlushRegularly);
                    worker.Start();
                }
        }

        static void FlushRegularly()
        {
            while (alive)
            {
                cancelWorker.WaitOne(SLEEP);
                if (alive) Flush();
            }
        }

        /// <summary> Start the measurement for a specific action. </summary>
        /// <param name="action">
        ///     The name of the action used as the value of the resulting XML-Node's '<paramref name="action"/>' attribute.
        ///     Must be a valid XML attribute value.
        /// </param>
        /// <param name="attrs"> Attributes added to the resulting XML-Node to add additional information about the action. </param>
        [Conditional("Measure")]
        public static void StartMeasure(string action, params CustomAttribute[] attrs)
        {
            Init();
            lock (currentNode)
            {
                AsserMaxDepth();

                currentNode.Append($"{new string('\t', depth)}<Measure {nameof(action)}=\"{action}\" time=\"");
                durationPos.Push(currentNode.Length);
                currentNode.Append('"');

                foreach (CustomAttribute attr in attrs)
                {
                    currentNode.Append(' ');
                    currentNode.Append(attr.name);
                    currentNode.Append("=\"");
                    currentNode.Append(attr.value);
                    currentNode.Append('"');
                }

                startTagEnds.Push(currentNode.Length);
                currentNode.AppendLine(">");
                PopClock().Restart();
            }
        }

        /// <summary> Ends the most recently started still running measure. </summary>
        /// <param name="appendAttribute">
        ///     Attributes added to the resulting XML-Node to add additional information about the action.
        ///     These will be appended to the attributes provided in the corresponding
        ///     <see cref="StartMeasure(string, CustomAttribute[])"/> call.
        /// </param>
        [Conditional("Measure")]
        public static void EndMeasure(System.Func<double, SysCol.IEnumerable<CustomAttribute>> appendAttribute = null)
        {
            Init();
            lock (currentNode)
            {
                AssertNonZeroDepth();

                Stopwatch clock = busyClocks.Pop();
                clock.Stop();

                int lastCloseTagPos = startTagEnds.Pop();
                if (currentNode.Length == lastCloseTagPos + 3)
                    currentNode.Insert(lastCloseTagPos, '/');
                else
                    currentNode.Append(new string('\t', depth) + "</Measure>\n");

                double tm = (double)clock.ElapsedTicks / Stopwatch.Frequency;
                int pos = durationPos.Pop();
                currentNode.Insert(pos, GetTimeString(tm));

                if (appendAttribute != null)
                {
                    int i = pos;
                    while (i < currentNode.Length)
                        if (currentNode[i] == '>')
                            break;
                        else if (currentNode[i] == '/' && currentNode[i + 1] == '>')
                            break;
                        else
                            i++;

                    foreach (CustomAttribute attr in appendAttribute(tm))
                    {
                        StringBuilder attrBldr = new StringBuilder();
                        attrBldr.Append(' ');
                        attrBldr.Append(attr.name);
                        attrBldr.Append("=\"");
                        attrBldr.Append(attr.value);
                        attrBldr.Append('"');
                        currentNode.Insert(i, attrBldr);
                    }
                }

                freeClocks.Push(clock);
                FinishNode();
            }
        }

        static string GetTimeString(double tm)
        {
            const string TIME_FORMAT = "#.###";

            if (tm >= 1)
                return tm.ToString(TIME_FORMAT) + "s";
            if (tm >= 1e-3)
                return (1e3f * tm).ToString(TIME_FORMAT) + "ms";
            else if (tm >= 1e-6)
                return (1e6f * tm).ToString(TIME_FORMAT) + "µs";
            else if (tm >= 1e-9)
                return (1e9f * tm).ToString(TIME_FORMAT) + "ns";
            else
                return "less than 1ns";
        }

        /// <summary> Add a single node to the log. </summary>
        /// <param name="name">
        ///     The name of the action that was measured.
        ///     Used as the resulting node's name.
        ///     Must be a valid XML node name.
        /// </param>
        /// <param name="duration"> The duration of the action. </param>
        [Conditional("Measure")]
        public static void AddMeasurement(string name, double duration)
        {
            Init();
            lock (currentNode)
            {
                currentNode.AppendLine(new string('\t', depth) + $"<{name} time=\"{GetTimeString(duration)}\"/>");
                FinishNode();
            }
        }

        static void FinishNode()
        {
            lock (currentNode)
            {
                lock (finishedNodes)
                {
                    if (depth == 0)
                    {
                        finishedNodes.Enqueue(currentNode.ToString());
                        currentNode.Clear();
                    }
                }
            }
        }

        static void Flush()
        {
            lock (finishedNodes)
                while (finishedNodes.Count > 0)
                {
                    string nextChunk = finishedNodes.Dequeue();
                    if (wr.BaseStream.Length + nextChunk.Length > MAX_FILE_SIZE)
                    {
                        wr.Flush();
                        wr.Close();
                        wr = new StreamWriter(File.Create($"{LOG_DIRECTORY}Measure {fileCounter++}.xml"));
                    }
                    wr.Write(nextChunk);
                }
        }

        /// <summary>
        ///     Writes all finished measurements to the log file.
        ///     After this is called no more measurements are written to any log files.
        ///     Measurements that were started but no yet completed will be discarded.
        /// </summary>
        public static void FinalizeLog()
        {
            lock (finishedNodes)
                if (alive)
                {
                    alive = false;
                    lock (initLock)
                        if (initialized)
                        {
                            cancelWorker.Set();
                            worker.Join();
                            Flush();
                            wr.Flush();
                            wr.Close();
                        }
                }
        }

        /// <summary>
        ///     Checks whether there are currently active measurements and throws an exception if so.
        ///     Use this at points in your program where you are certain that there should be no
        ///     active measurements in order to detect mismatched <see cref="StartMeasure(string, CustomAttribute[])"/>
        ///     and <see cref="EndMeasure(System.Func{double, SysCol.IEnumerable{CustomAttribute}})"/> calls.
        /// </summary>
        [Conditional("Measure")]
        public static void AssertZeroDepth()
        {
            if (depth != 0)
                throw new System.Exception("Log depth is not zero!");
        }

        static void AssertNonZeroDepth()
        {
            if (depth <= 0)
                throw new System.Exception("No active measurement!");
        }

        static void AsserMaxDepth()
        {
            if (depth > MAX_LOG_DEPTH)
                throw new System.Exception("Exceeding maximum measurement log depth!");
        }

        static Stopwatch PopClock()
        {
            if (freeClocks.Count == 0)
            {
                Stopwatch watch = new Stopwatch();
                busyClocks.Push(watch);
                return watch;
            }
            else
            {
                Stopwatch freeClock = freeClocks.Pop();
                busyClocks.Push(freeClock);
                return freeClock;
            }
        }
    }
}
