#region license

// AoC2023 - AoC2023 - Day20.cs
// Copyright (C) 2023 Nicholas
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System.Runtime.CompilerServices;
using QuikGraph;
using QuikGraph.Graphviz;

namespace AoC2023;

public class Day20 : Adventer {
    public enum Pulse {
        Low = 0,
        High = 1,
    }

    public abstract class Module(string id) : IEquatable<Module> {
        public bool Equals(Module? other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return ReferenceEquals(Id, other.Id);
        }

        public override bool Equals(object? obj) {
            return ReferenceEquals(this, obj) || obj is Module other && Equals(other);
        }

        public override int GetHashCode() {
            return RuntimeHelpers.GetHashCode(Id);
        }

        public static bool operator ==(Module? left, Module? right) {
            return Equals(left, right);
        }

        public static bool operator !=(Module? left, Module? right) {
            return !Equals(left, right);
        }

        public string Id { get; } = string.Intern(id);
        public abstract Pulse? HandleInput(Module sender, Pulse input);

        public abstract string Prefix { get; }

        public override string ToString() {
            return $"{Prefix}{Id}";
        }
    };

    public class Broadcast() : Module("broadcaster") {
        public override Pulse? HandleInput(Module _, Pulse input) => input;
        public override string Prefix => "";
    }

    public class FlipFlop(string id) : Module(id) {
        public bool Powered { get; set; }

        public override Pulse? HandleInput(Module _, Pulse input) {
            if (input == Pulse.High) {
                return null;
            }

            if (Powered) {
                Powered = false;
                return Pulse.Low;
            }

            Powered = true;
            return Pulse.High;
        }

        public override string Prefix => "%";
    }

    public class Conjunction(string id, IEnumerable<string> sources) : Module(id) {

        private Dictionary<string, Pulse> Inputs { get; } = sources.ToDictionary(s => s, _ => Pulse.Low, EqualityComparer<string>.Create(ReferenceEquals, RuntimeHelpers.GetHashCode));

        private void ObserveInput(Module sender, Pulse input) {
            Inputs[sender.Id] = input;
        }

        public override Pulse? HandleInput(Module sender, Pulse input) {
            ObserveInput(sender, input);

            foreach (var pulse in Inputs.Values) {
                if (pulse == Pulse.Low) {
                    return Pulse.High;
                }
            }

            return Pulse.Low;
        }

        public override string Prefix => "&";
    }
    
    public class Dummy(string id) : Module(id) {
        public override Pulse? HandleInput(Module sender, Pulse input) {
            return null;
        }

        public override string Prefix => "";
    }

    public readonly record struct Message(Module Sender, Pulse Datum, Module Recipient) {
        public override string ToString() {
            return $"{Sender} -{Datum}-> {Recipient}";
        }

    }

    public class Machine {

        private readonly ulong[] pulseCounters = { 0ul, 0ul };
        private readonly Dictionary<string, Module> modules = new();
        private readonly Dictionary<Module, List<Module>> moduleGraph = new(); 
        private readonly Module broadcaster;
        private readonly Queue<Message> messageQueue = new();

        public event Action<Message>? ObserveMessage;

        private void ObservePulse(Pulse pulse) {
            pulseCounters[(int) pulse]++;
        }

        public void PushButton() {
            messageQueue.Enqueue(new Message(broadcaster, Pulse.Low, broadcaster));
            Quiesce();
        }

        private bool Quiescent => messageQueue.Count == 0;

        private void Quiesce() {
            while (!Quiescent) {
                var msg = messageQueue.Dequeue();
                ObserveMessage?.Invoke(msg);
                ObservePulse(msg.Datum);
                var output = msg.Recipient.HandleInput(msg.Sender, msg.Datum);
                if (output == null) {
                    continue;
                }

                foreach (var destination in moduleGraph[msg.Recipient]) {
                    messageQueue.Enqueue(new Message(msg.Recipient, output.Value, destination));
                }
            }
        }

        public IEnumerable<(Pulse, int)> PulseCounters => pulseCounters.Select((c, i) => ((Pulse) i, (int) c));

        public Machine(IBidirectionalGraph<string, Edge<string>> blueprint, IDictionary<string, char> idToPrefix) {
            broadcaster = new Broadcast();
            foreach (var vert in blueprint.Vertices) {
                var vertex = string.Intern(vert);
                if (!idToPrefix.TryGetValue(vertex, out var prefix)) {
                    prefix = default(char);
                }
                var module = prefix switch {
                    '%' => new FlipFlop(vertex),
                    '&' => new Conjunction(vertex, blueprint.InEdges(vertex).Select(e => e.Source)),
                    _ when vertex == "broadcaster" => broadcaster,
                    _ => new Dummy(vertex),
                };

                modules[vertex] = module;
            }

            foreach (var vertex in blueprint.Vertices) {
                moduleGraph[modules[vertex]] = blueprint.OutEdges(vertex).Select(e => modules[e.Target]).ToList();
            }
            // flipFlops.Sort(((flop, flipFlop) => string.Compare(flop.Id, flipFlop.Id, StringComparison.Ordinal)));
        }
    }

    public class Problem {
        private readonly BidirectionalGraph<string, Edge<string>> blueprint = new(false);
        private readonly Dictionary<string, char> idToPrefix = new();

        public Problem(IEnumerable<string> lines) {
            foreach (var line in lines) {
                var span = line.AsSpan();
                var prefix = span[0];
                var arrow = span.IndexOf("->");
                var id = prefix switch {
                    '%' => span[1..arrow].Trim().ToString(),
                    '&' => span[1..arrow].Trim().ToString(),
                    _   => span[..arrow].Trim().ToString(),
                };
                id = string.Intern(id);
                var destStart = arrow + 2;
                var current = span[destStart..].Trim();
                var destEnd = current.IndexOf(',');
                if (destEnd == -1) {
                    destEnd = current.Length;
                }
                while (current.Length > 0) {
                    var dest = current[..destEnd].Trim().ToString();
                    blueprint.AddVerticesAndEdge(new Edge<string>(id, dest));
                    destEnd = current.IndexOf(',');
                    if (destEnd == -1) {
                        destEnd = current.Length-1;
                    }
                    current = current[(destEnd + 1)..].Trim();
                };

                idToPrefix[id] = prefix;
            }
        }

        public Machine BuildMachine() {
            return new Machine(blueprint, idToPrefix);
        }

    }

    public Day20() {
        Bag["test"] = """
                      broadcaster -> a, b, c
                      %a -> b
                      %b -> c
                      %c -> inv
                      &inv -> a
                      """;
    }

    private Problem problem;
    protected override void InternalOnLoad() {
        problem = new Problem(Input.Lines);
    }

    protected override object InternalPart1() {
        var machine = problem.BuildMachine();
        for (var i = 0; i < 1000; i++) {
            machine.PushButton();
        }
        return machine.PulseCounters.Product(c => (long)c.Item2);
    }

    protected override object InternalPart2() {
        var machine = problem.BuildMachine();
        var firstObservedHigh = new Dictionary<string, long>();
        var pushes = new[] { 0L };
        machine.ObserveMessage += msg => {
            if (msg is { Datum: Pulse.High, Recipient.Id: "jz" }) {
                firstObservedHigh.TryAdd(msg.Sender.Id, pushes[0]);
            }
        };
        while (firstObservedHigh.Count < 4) {
            pushes[0]++;
            machine.PushButton();
        }

        return firstObservedHigh.Values.Aggregate(MathUtils.Lcm);
    }
}
